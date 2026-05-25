#!/usr/bin/env bash
set -euo pipefail
export COPYFILE_DISABLE=1

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
DOTNET_BIN="${DOTNET:-/opt/homebrew/opt/dotnet@8/bin/dotnet}"
SIGN_IDENTITY="${INSTALLER_SIGN_IDENTITY:-}"

VERSION="$(node -e "const fs=require('fs'); const m=JSON.parse(fs.readFileSync('${ROOT_DIR}/metadata.json','utf8')); process.stdout.write(m.PluginVersion)")"
IDENTIFIER="com.simonheys.opentabletdriver.pendragscroll"
DIST_DIR="${ROOT_DIR}/dist/macos"
WORK_DIR="${ROOT_DIR}/obj/macos-pkg"
SCRIPTS_DIR="${WORK_DIR}/scripts"
COMPONENT_PKG="${DIST_DIR}/PenDragScrollPlugin-component.pkg"
PRODUCT_PKG="${DIST_DIR}/PenDragScrollPlugin-${VERSION}-macos.pkg"

"${DOTNET_BIN}" build "${ROOT_DIR}/PenDragScroll.csproj" -c Release

/bin/rm -rf "${WORK_DIR}" "${DIST_DIR}"
/bin/mkdir -p "${SCRIPTS_DIR}" "${DIST_DIR}"

DLL_B64="$(/usr/bin/base64 -i "${ROOT_DIR}/bin/Release/net8.0/PenDragScroll.dll" | /usr/bin/tr -d '\n')"
METADATA_B64="$(/usr/bin/base64 -i "${ROOT_DIR}/metadata.json" | /usr/bin/tr -d '\n')"

/usr/bin/sed \
  -e "s#__PENDRAGSCROLL_DLL_BASE64__#${DLL_B64}#g" \
  -e "s#__PENDRAGSCROLL_METADATA_BASE64__#${METADATA_B64}#g" \
  "${ROOT_DIR}/packaging/macos/scripts/postinstall" > "${SCRIPTS_DIR}/postinstall"
/bin/chmod 755 "${SCRIPTS_DIR}/postinstall"

/usr/bin/pkgbuild \
  --nopayload \
  --scripts "${SCRIPTS_DIR}" \
  --identifier "${IDENTIFIER}" \
  --version "${VERSION}" \
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

echo "${PRODUCT_PKG}"
