#!/bin/bash
# ساخت APK/AAB از خط فرمان — بدون باز کردن ادیتور
# استفاده:
#   ./tools/build_android.sh apk      → Build/Android/FogWalker.apk (Release)
#   ./tools/build_android.sh apk-dev  → APK توسعه
#   ./tools/build_android.sh aab      → Build/Android/FogWalker.aab (انتشار)

UNITY_EXE="${UNITY_EXE:-/opt/unity/Editor/Unity}"   # مسیر Unity خود را اینجا یا با متغیر محیطی بدهید
PROJECT_PATH="$(cd "$(dirname "$0")/.." && pwd)"
MODE="${1:-apk}"

case "$MODE" in
  apk)     METHOD="FogWalker.EditorTools.BuildScript.BuildAndroidApk" ;;
  apk-dev) METHOD="FogWalker.EditorTools.BuildScript.BuildAndroidApkMenu" ;;
  aab)     METHOD="FogWalker.EditorTools.BuildScript.BuildAndroidAab" ;;
  *) echo "حالت نامعتبر: $MODE (apk | apk-dev | aab)"; exit 1 ;;
esac

echo ">> Unity:  $UNITY_EXE"
echo ">> پروژه: $PROJECT_PATH"
echo ">> متد:   $METHOD"

"$UNITY_EXE" -batchmode -quit -nographics \
  -projectPath "$PROJECT_PATH" \
  -buildTarget Android \
  -executeMethod "$METHOD" \
  -logFile "$PROJECT_PATH/Logs/build.log"

EXIT_CODE=$?
echo ">> خروجی: $PROJECT_PATH/Build/Android/"
exit $EXIT_CODE
