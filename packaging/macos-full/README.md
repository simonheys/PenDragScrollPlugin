# Full macOS installer

This installer is intended for Simon's internal machines with the same architecture as the source Mac.

It bundles:

- `/Applications/OpenTabletDriver.app`
- Pen Drag Scroll plugin
- the current OpenTabletDriver `settings.json`
- a user LaunchAgent that opens OpenTabletDriver at login

The package is built as a scripts-only installer with an embedded tarball. That avoids macOS extended-attribute metadata being written into the package payload.

Build and sign:

```sh
INSTALLER_SIGN_IDENTITY="Developer ID Installer: Studio Heys Limited (4YADBGMM88)" \
  packaging/macos-full/build-full-pkg.sh
```

Optional overrides:

```sh
OTD_APP="/Users/simon/Applications/OpenTabletDriver.app"
OTD_SETTINGS_JSON="$HOME/Library/Application Support/OpenTabletDriver/settings.json"
```

The target Mac does not need Xcode. It will still need to approve macOS permissions for OpenTabletDriver in System Settings.

Without notarization, Gatekeeper may reject the package in Finder even though it has a valid Developer ID Installer signature. For internal installation, use:

```sh
sudo installer -pkg PenDragScrollFull-0.1.1-macos.pkg -target /
```

If the target Mac is Apple Silicon and Rosetta is not installed yet, macOS will prompt for Rosetta when OpenTabletDriver first launches.
