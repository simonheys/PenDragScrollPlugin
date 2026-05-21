using System.Runtime.InteropServices;

namespace PenDragScroll.Native.MacOS;

internal sealed class MacOSMouseWheel : IMouseWheel
{
    private const string CoreGraphics = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    private int? _verticalDelta;
    private int? _horizontalDelta;

    public void ScrollVertically(int amount)
    {
        _verticalDelta = (_verticalDelta ?? 0) - amount;
    }

    public void ScrollHorizontally(int amount)
    {
        _horizontalDelta = (_horizontalDelta ?? 0) - amount;
    }

    public void Flush()
    {
        if (!_verticalDelta.HasValue && !_horizontalDelta.HasValue)
            return;

        var scrollEvent = CGEventCreateScrollWheelEvent2(
            nint.Zero,
            CGScrollEventUnit.Pixel,
            2,
            _verticalDelta ?? 0,
            _horizontalDelta ?? 0,
            0);

        CGEventPost(CGEventTapLocation.HidEventTap, scrollEvent);
        CFRelease(scrollEvent);

        _verticalDelta = null;
        _horizontalDelta = null;
    }

    public void Dispose() {}

    [DllImport(CoreFoundation)]
    private static extern void CFRelease(nint handle);

    [DllImport(CoreGraphics)]
    private static extern nint CGEventCreateScrollWheelEvent2(
        nint source,
        CGScrollEventUnit units,
        uint wheelCount,
        int wheel1,
        int wheel2,
        int wheel3);

    [DllImport(CoreGraphics, EntryPoint = "CGEventPost")]
    private static extern void CGEventPost(CGEventTapLocation tap, nint eventRef);

    private enum CGScrollEventUnit
    {
        Pixel = 0,
        Line = 1
    }

    private enum CGEventTapLocation
    {
        HidEventTap = 0,
        SessionEventTap = 1,
        AnnotatedSessionEventTap = 2
    }
}
