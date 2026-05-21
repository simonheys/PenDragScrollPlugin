# Pen Drag Scroll Plugin for OpenTabletDriver

> [!WARNING]
> This project was partially vibecoded because I needed this functionality quickly for myself, but it is fully functional and I'm sharing it publicly in case others find it useful.

A small OpenTabletDriver plugin for **Linux, macOS, and Windows** that turns a pen button into a **hold-to-scroll modifier**.

While the modifier is held and the pen tip is touching the tablet, distance from the initial touch point is converted into continuous scrolling instead of normal cursor movement.

## Features
- Hold a pen button to enable drag-to-scroll
- Require pen-tip contact before scrolling starts
- Vertical drag scrolling by default
- Optional horizontal scrolling support
- Optional cursor anchoring while scrolling
- Works with OpenTabletDriver `0.6.6.2`

## How it works
This plugin has two parts:

- `PenDragScroll.Bindings.PenDragScrollToggleBinding`
  - assign this to a pen button
  - while held, drag-scroll mode is active

- `PenDragScroll.Filters.PenDragScrollFilter`
  - add this as an OTD filter
  - converts pen movement into high-resolution wheel events

## Current setup used on this machine
The active config was set up like this:
- pen button 1 -> `Pen Drag Scroll Toggle`
- pen button 2 -> normal `Button 1`
- anchored cursor while scrolling
- vertical scrolling enabled
- horizontal scrolling disabled

## Files
- `Bindings/PenDragScrollToggleBinding.cs` - button modifier binding
- `Filters/PenDragScrollFilter.cs` - movement-to-scroll filter
- `State/DragScrollStateStore.cs` - shared on/off state
- `Native/Linux/...` - Linux evdev/uinput wheel emitter
- `Native/MacOS/...` - macOS CoreGraphics wheel emitter
- `PenDragScroll.csproj` - project file
- `install.sh` - local build/install helper
- `metadata.json` - OTD plugin metadata

## Requirements
- Linux, macOS, or Windows
- OpenTabletDriver `0.6.6.2`
- .NET 8 SDK
- on Linux: permissions for virtual input / uinput as required by your setup

## Build
Example using the local SDK path already used during setup:

```bash
/tmp/dotnet/sdk/dotnet build -c Release
```

Or if `dotnet` is on your PATH:

```bash
dotnet build -c Release
```

## Install
### Automatic
```bash
bash ./install.sh
```

### Manual
Build first, then copy:

```bash
mkdir -p ~/.config/OpenTabletDriver/Plugins/Pen\ Drag\ Scroll/
cp bin/Release/net8.0/PenDragScroll.dll ~/.config/OpenTabletDriver/Plugins/Pen\ Drag\ Scroll/
cp metadata.json ~/.config/OpenTabletDriver/Plugins/Pen\ Drag\ Scroll/
```

Then restart OpenTabletDriver.

## OTD config example
### Filter
```json
{
  "Path": "PenDragScroll.Filters.PenDragScrollFilter",
  "Settings": [
    { "Property": "AnchorCursor", "Value": true },
    { "Property": "VerticalUnitsPerPixel", "Value": 0.12 },
    { "Property": "HorizontalUnitsPerPixel", "Value": 0.0 },
    { "Property": "DeadZonePixels", "Value": 24.0 },
    { "Property": "PressureThreshold", "Value": 0 },
    { "Property": "PenButtonIndex", "Value": 0 },
    { "Property": "SuppressTipWhileScrolling", "Value": true },
    { "Property": "ScrollIntervalMs", "Value": 16.0 },
    { "Property": "InvertVertical", "Value": false },
    { "Property": "InvertHorizontal", "Value": false }
  ],
  "Enable": true
}
```

### Pen button binding
```json
{
  "Path": "PenDragScroll.Bindings.PenDragScrollToggleBinding",
  "Settings": [],
  "Enable": true
}
```

## Tuning
### Scroll speed
Adjust:
- `VerticalUnitsPerPixel`
- `HorizontalUnitsPerPixel`
- `DeadZonePixels`
- `ScrollIntervalMs`
- `PenButtonIndex`

Notes:
- `120` units is about one traditional wheel tick on Linux/Windows
- `0.12` is a moderate default for Wacom-like continuous rate scrolling on macOS
- Larger `DeadZonePixels` values require a longer drag before scrolling starts

### Direction
If scrolling feels reversed:
- set `InvertVertical` to `true`
- set `InvertHorizontal` to `true`

### Cursor behavior
- `AnchorCursor = true`: keep cursor fixed while scrolling
- `AnchorCursor = false`: allow cursor to move while also scrolling
- `SuppressTipWhileScrolling = true`: prevent tip-click actions such as text selection while scrolling, until the pen tip is lifted

## Platform notes
- **Linux**: uses evdev/uinput high-resolution wheel events
- **macOS**: uses CoreGraphics pixel scroll events
- **Windows**: uses `SendInput` wheel events

## Known limitations
- behavior may vary slightly depending on compositor/app support
- this is a practical workaround for global drag-scroll, not native tablet-stack support in every environment

## License
This project is licensed under the MIT License. See [`LICENSE`](./LICENSE).
