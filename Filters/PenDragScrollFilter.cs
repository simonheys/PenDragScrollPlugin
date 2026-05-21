using System.Numerics;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.DependencyInjection;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using PenDragScroll.Native;
using PenDragScroll.State;
using OtdTimer = OpenTabletDriver.Plugin.Timers.ITimer;

namespace PenDragScroll.Filters;

[PluginName("Pen Drag Scroll")]
[SupportedPlatform(PluginPlatform.Linux | PluginPlatform.MacOS | PluginPlatform.Windows)]
public sealed class PenDragScrollFilter : IPositionedPipelineElement<IDeviceReport>, IDisposable
{
    private readonly IMouseWheel _wheel = MouseWheelFactory.Create();
    private readonly object _lock = new();
    private bool _scrolling;
    private Vector2 _anchorPosition;
    private Vector2 _currentPosition;
    private float _verticalAccumulator;
    private float _horizontalAccumulator;
    private bool _suppressTipUntilRelease;
    private OtdTimer? _timer;

    public event Action<IDeviceReport>? Emit;

    [TabletReference]
    public TabletReference? Tablet { get; set; }

    public PipelinePosition Position => PipelinePosition.PostTransform;

    [Resolved]
    public OtdTimer? Timer
    {
        get => _timer;
        set
        {
            if (_timer != null)
                _timer.Elapsed -= ScrollFromAnchorOffset;

            _timer = value;

            if (_timer != null)
            {
                _timer.Interval = ScrollIntervalMs;
                _timer.Elapsed += ScrollFromAnchorOffset;
            }
        }
    }

    [Property("Anchor Cursor"), DefaultPropertyValue(true),
     ToolTip("Pen Drag Scroll:\n\nWhen enabled, the cursor is held in place while the drag-scroll modifier is active.")]
    public bool AnchorCursor { get; set; } = true;

    [Property("Vertical Units Per Pixel"), DefaultPropertyValue(0.12f), Unit("u/px"),
     ToolTip("Pen Drag Scroll:\n\nHow much scroll to emit per timer tick for each pixel held away from the start position.")]
    public float VerticalUnitsPerPixel { get; set; } = 0.12f;

    [Property("Horizontal Units Per Pixel"), DefaultPropertyValue(0f), Unit("u/px"),
     ToolTip("Pen Drag Scroll:\n\nHow much horizontal scroll to emit per timer tick for each pixel held away from the start position.\nSet to 0 to disable horizontal drag-scrolling.")]
    public float HorizontalUnitsPerPixel { get; set; } = 0f;

    [Property("Dead Zone Pixels"), DefaultPropertyValue(24f), Unit("px"),
     ToolTip("Pen Drag Scroll:\n\nHow far the pen must move from the start position before scrolling starts.")]
    public float DeadZonePixels { get; set; } = 24f;

    [Property("Pressure Threshold"), DefaultPropertyValue(0u),
     ToolTip("Pen Drag Scroll:\n\nMinimum pressure required before drag-scroll starts.\nThe default requires any pen-tip contact.")]
    public uint PressureThreshold { get; set; }

    [Property("Pen Button Index"), DefaultPropertyValue(0),
     ToolTip("Pen Drag Scroll:\n\nPhysical pen button index used for drag-scroll.\nThe default is the first side button.")]
    public int PenButtonIndex { get; set; }

    [Property("Suppress Tip While Scrolling"), DefaultPropertyValue(true),
     ToolTip("Pen Drag Scroll:\n\nWhen enabled, pen-tip clicks are suppressed while drag-scrolling and until the tip is lifted to prevent text selection or drawing.")]
    public bool SuppressTipWhileScrolling { get; set; } = true;

    [Property("Scroll Interval"), DefaultPropertyValue(16f), Unit("ms"),
     ToolTip("Pen Drag Scroll:\n\nHow often scrolling is emitted while the pen button and tip are held.")]
    public float ScrollIntervalMs
    {
        get => _scrollIntervalMs;
        set
        {
            _scrollIntervalMs = Math.Max(1f, value);
            if (Timer != null)
                Timer.Interval = _scrollIntervalMs;
        }
    }

    [Property("Invert Vertical"), DefaultPropertyValue(false),
     ToolTip("Pen Drag Scroll:\n\nInvert vertical drag-scroll direction.")]
    public bool InvertVertical { get; set; }

    [Property("Invert Horizontal"), DefaultPropertyValue(false),
     ToolTip("Pen Drag Scroll:\n\nInvert horizontal drag-scroll direction.")]
    public bool InvertHorizontal { get; set; }

    public void Consume(IDeviceReport value)
    {
        if (value is ITabletReport tabletReport)
            ProcessTabletReport(tabletReport);

        Emit?.Invoke(value);
    }

    private void ProcessTabletReport(ITabletReport report)
    {
        bool active = IsDragScrollActive(report);
        bool touching = report.Pressure > PressureThreshold;
        bool startTimer = false;
        bool stopTimer = false;

        lock (_lock)
        {
            if (!touching)
            {
                stopTimer = StopScrolling();
                _suppressTipUntilRelease = false;
            }
            else if (!active)
            {
                stopTimer = StopScrolling();

                // Avoid a synthetic tip press after releasing the scroll button,
                // but let hover position updates resume immediately.
                if (_suppressTipUntilRelease && SuppressTipWhileScrolling)
                    SuppressTip(report);
            }
            else
            {
                if (!_scrolling)
                {
                    _scrolling = true;
                    _anchorPosition = report.Position;
                    _verticalAccumulator = 0;
                    _horizontalAccumulator = 0;
                    startTimer = true;
                }

                _currentPosition = report.Position;

                if (AnchorCursor)
                    report.Position = _anchorPosition;

                if (SuppressTipWhileScrolling)
                {
                    _suppressTipUntilRelease = true;
                    SuppressTip(report);
                }
            }
        }

        if (stopTimer)
            Timer?.Stop();

        if (startTimer)
            Timer?.Start();
    }

    private void ScrollFromAnchorOffset()
    {
        int verticalWhole;
        int horizontalWhole;

        lock (_lock)
        {
            if (!_scrolling)
                return;

            var offset = _currentPosition - _anchorPosition;

            // Default mapping: holding the pen below/right of the anchor scrolls down/right.
            float verticalDelta = -ApplyDeadZone(offset.Y, DeadZonePixels) * VerticalUnitsPerPixel;
            float horizontalDelta = -ApplyDeadZone(offset.X, DeadZonePixels) * HorizontalUnitsPerPixel;

            if (InvertVertical)
                verticalDelta *= -1;

            if (InvertHorizontal)
                horizontalDelta *= -1;

            _verticalAccumulator += verticalDelta;
            _horizontalAccumulator += horizontalDelta;

            verticalWhole = (int)MathF.Truncate(_verticalAccumulator);
            if (verticalWhole != 0)
                _verticalAccumulator -= verticalWhole;

            horizontalWhole = (int)MathF.Truncate(_horizontalAccumulator);
            if (horizontalWhole != 0)
                _horizontalAccumulator -= horizontalWhole;
        }

        if (verticalWhole != 0)
            _wheel.ScrollVertically(verticalWhole);

        if (horizontalWhole != 0)
            _wheel.ScrollHorizontally(horizontalWhole);

        if (verticalWhole != 0 || horizontalWhole != 0)
            _wheel.Flush();
    }

    private void Reset()
    {
        bool stopTimer;

        lock (_lock)
        {
            stopTimer = StopScrolling();
            _suppressTipUntilRelease = false;
        }

        if (stopTimer)
            Timer?.Stop();
    }

    private bool StopScrolling()
    {
        var wasScrolling = _scrolling;

        _scrolling = false;
        _verticalAccumulator = 0;
        _horizontalAccumulator = 0;

        return wasScrolling;
    }

    private void SuppressTip(ITabletReport report)
    {
        report.Pressure = 0;
    }

    private bool IsDragScrollActive(ITabletReport report)
    {
        if (PenButtonIndex >= 0 && PenButtonIndex < report.PenButtons.Length)
        {
            bool active = report.PenButtons[PenButtonIndex];
            DragScrollStateStore.SetActive(Tablet, active);
            return active;
        }

        return DragScrollStateStore.IsActive(Tablet);
    }

    private static float ApplyDeadZone(float value, float deadZone)
    {
        var absValue = MathF.Abs(value);
        if (absValue <= deadZone)
            return 0;

        return MathF.Sign(value) * (absValue - deadZone);
    }

    private float _scrollIntervalMs = 16f;

    public void Dispose()
    {
        Timer?.Stop();
        if (Timer != null)
            Timer.Elapsed -= ScrollFromAnchorOffset;
        _wheel.Dispose();
    }
}
