#!/bin/bash
if [ -z "$1" ]; then
  echo enter location of app
  exit
fi
APP_LOC=$1
echo "Signing Game ${APP_LOC}"

echo "Copying provisioning profile..."
cp ./adventure.provisionprofile ${APP_LOC}/Contents/
cp ./info.plist ${APP_LOC}/Contents/
echo "Starting Signing..."

codesign -f --deep -s "Developer ID Application: Robert Antonucci" --entitlements adventure.entitlements ${APP_LOC}/Contents/Frameworks/Mono/MonoEmbedRuntime/osx/libmono.0.dylib
codesign -f --deep -s "Developer ID Application: Robert Antonucci" --entitlements adventure.entitlements ${APP_LOC}/Contents/Frameworks/Mono/MonoEmbedRuntime/osx/libMonoPosixHelper.dylib
codesign -f --deep -s "Developer ID Application: Robert Antonucci" --entitlements adventure.entitlements ${APP_LOC}/

echo "Done Signing..."

echo "Packaging game..."
productbuild --component "${APP_LOC}" "/Applications" --sign "Developer ID Installer: Robert Antonucci" adventure.pkg
