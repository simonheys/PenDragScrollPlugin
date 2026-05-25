# macOS installer

Build an unsigned installer:

```sh
packaging/macos/build-pkg.sh
```

Build a Developer ID signed installer:

```sh
INSTALLER_SIGN_IDENTITY="Developer ID Installer: Studio Heys Limited (4YADBGMM88)" \
  packaging/macos/build-pkg.sh
```

Build, notarize, and staple:

```sh
INSTALLER_SIGN_IDENTITY="Developer ID Installer: Studio Heys Limited (4YADBGMM88)" \
NOTARY_PROFILE="studio-heys-notary" \
  packaging/macos/build-pkg.sh
```

Create the notarytool profile once with either an App Store Connect API key or an app-specific password. For example:

```sh
xcrun notarytool store-credentials "studio-heys-notary" \
  --apple-id "APPLE_ID_EMAIL" \
  --team-id "4YADBGMM88" \
  --password "APP_SPECIFIC_PASSWORD"
```

The installer writes `PenDragScroll.dll` and `metadata.json` to the logged-in user's OpenTabletDriver plugin directory:

```text
~/Library/Application Support/OpenTabletDriver/Plugins/Pen Drag Scroll/
```
