#!/usr/bin/env bash
set -euo pipefail
export COPYFILE_DISABLE=1

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
DOTNET_BIN="${DOTNET:-/opt/homebrew/opt/dotnet@8/bin/dotnet}"
OTD_APP="${OTD_APP:-/Users/simon/Applications/OpenTabletDriver.app}"
SETTINGS_JSON="${OTD_SETTINGS_JSON:-${HOME}/Library/Application Support/OpenTabletDriver/settings.json}"
SIGN_IDENTITY="${INSTALLER_SIGN_IDENTITY:-}"
NOTARY_PROFILE="${NOTARY_PROFILE:-}"

PLUGIN_VERSION="$(node -e "const fs=require('fs'); const m=JSON.parse(fs.readFileSync('${ROOT_DIR}/metadata.json','utf8')); process.stdout.write(m.PluginVersion)")"
IDENTIFIER="com.simonheys.opentabletdriver.full"
DIST_DIR="${ROOT_DIR}/dist/macos"
WORK_DIR="${ROOT_DIR}/obj/macos-full-pkg"
ROOT_STAGING="${WORK_DIR}/root"
SUPPORT_STAGING="${ROOT_STAGING}/Library/Application Support/PenDragScrollFullInstaller"
PLUGIN_STAGING="${SUPPORT_STAGING}/Pen Drag Scroll"
SCRIPTS_DIR="${WORK_DIR}/scripts"
SCRIPT_PAYLOAD="${SCRIPTS_DIR}/payload.tar.gz"
COMPONENT_PKG="${DIST_DIR}/PenDragScrollFull-component.pkg"
PRODUCT_PKG="${DIST_DIR}/PenDragScrollFull-${PLUGIN_VERSION}-macos.pkg"

if [ ! -d "${OTD_APP}" ]; then
  echo "OpenTabletDriver app not found: ${OTD_APP}" >&2
  exit 1
fi

if [ ! -f "${SETTINGS_JSON}" ]; then
  echo "OpenTabletDriver settings not found: ${SETTINGS_JSON}" >&2
  exit 1
fi

"${DOTNET_BIN}" build "${ROOT_DIR}/PenDragScroll.csproj" -c Release

/bin/rm -rf "${WORK_DIR}" "${PRODUCT_PKG}" "${COMPONENT_PKG}"
/bin/mkdir -p "${ROOT_STAGING}/Applications" "${PLUGIN_STAGING}" "${SCRIPTS_DIR}" "${DIST_DIR}"

/usr/bin/ditto --norsrc --noextattr "${OTD_APP}" "${ROOT_STAGING}/Applications/OpenTabletDriver.app"
/usr/bin/ditto --norsrc --noextattr "${ROOT_DIR}/bin/Release/net8.0/PenDragScroll.dll" "${PLUGIN_STAGING}/PenDragScroll.dll"
/usr/bin/ditto --norsrc --noextattr "${ROOT_DIR}/metadata.json" "${PLUGIN_STAGING}/metadata.json"
/usr/bin/ditto --norsrc --noextattr "${SETTINGS_JSON}" "${SUPPORT_STAGING}/settings.json"
/usr/bin/ditto --norsrc --noextattr "${ROOT_DIR}/packaging/macos-full/templates/net.opentabletdriver.login.plist" "${SUPPORT_STAGING}/net.opentabletdriver.login.plist"
/usr/bin/ditto --norsrc --noextattr "${ROOT_DIR}/packaging/macos-full/scripts/postinstall" "${SCRIPTS_DIR}/postinstall"
/bin/chmod 755 "${SCRIPTS_DIR}/postinstall"

/usr/bin/xattr -cr "${ROOT_STAGING}" || true
/usr/bin/find "${ROOT_STAGING}" -exec /usr/bin/xattr -d com.apple.provenance {} + 2>/dev/null || true
COPYFILE_DISABLE=1 /usr/bin/tar -czf "${SCRIPT_PAYLOAD}" -C "${ROOT_STAGING}" .

/usr/bin/pkgbuild \
  --nopayload \
  --scripts "${SCRIPTS_DIR}" \
  --identifier "${IDENTIFIER}" \
  --version "${PLUGIN_VERSION}" \
  "${COMPONENT_PKG}"

if [ -n "${SIGN_IDENTITY}" ]; then
  /usr/bin/productbuild \
    --package "${COMPONENT_PKG}" \
    --sign "${SIGN_IDENTITY}" \
    "${PRODUCT_PKG}"
else
  /usr/bin/productbuild \
    --package "${COMPONENT_PKG}" \
    "${PRODUCT_PKG}"
fi

/bin/rm -f "${COMPONENT_PKG}"

if [ -n "${NOTARY_PROFILE}" ]; then
  /usr/bin/xcrun notarytool submit "${PRODUCT_PKG}" \
    --keychain-profile "${NOTARY_PROFILE}" \
    --wait
  /usr/bin/xcrun stapler staple "${PRODUCT_PKG}"
  /usr/bin/xcrun stapler validate "${PRODUCT_PKG}"
fi

echo "${PRODUCT_PKG}"
