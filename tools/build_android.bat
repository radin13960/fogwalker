@echo off
REM ساخت APK/AAB از خط فرمان ویندوز
REM استفاده: tools\build_android.bat apk | apk-dev | aab

setlocal
if "%UNITY_EXE%"=="" set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.0.32f1\Editor\Unity.exe"
set "PROJECT_PATH=%~dp0.."
set "MODE=%~1"
if "%MODE%"=="" set "MODE=apk"

if /i "%MODE%"=="apk" set "METHOD=FogWalker.EditorTools.BuildScript.BuildAndroidApk"
if /i "%MODE%"=="apk-dev" set "METHOD=FogWalker.EditorTools.BuildScript.BuildAndroidApkMenu"
if /i "%MODE%"=="aab" set "METHOD=FogWalker.EditorTools.BuildScript.BuildAndroidAab"
if not defined METHOD ( echo حالت نامعتبر & exit /b 1 )

if not exist "%PROJECT_PATH%\Logs" mkdir "%PROJECT_PATH%\Logs"

echo ^>^> Unity:  %UNITY_EXE%
echo ^>^> پروژه: %PROJECT_PATH%
echo ^>^> متد:   %METHOD%

"%UNITY_EXE%" -batchmode -quit -nographics -projectPath "%PROJECT_PATH%" -buildTarget Android -executeMethod %METHOD% -logFile "%PROJECT_PATH%\Logs\build.log"

echo ^>^> خروجی: %PROJECT_PATH%\Build\Android\
endlocal
