@echo off
setlocal DisableDelayedExpansion
chcp 65001 >nul
set "ROOT=%~dp0"
set "PYTHON=python"
if exist "%ROOT%.venv\Scripts\python.exe" set "PYTHON=%ROOT%.venv\Scripts\python.exe"
set "VIDEO=%~1"
if not defined VIDEO set /p "VIDEO=MP4 path (or drag a video onto this BAT): "
set "VIDEO=%VIDEO:"=%"
if not defined VIDEO goto :invalid
if not exist "%VIDEO%" goto :invalid
echo.
echo Normal rotating video: enter the approximate TOTAL sweep in degrees.
echo Use 360 only for a complete full turn. Unknown coverage defaults to 180.
echo Missing directions remain black and have coverage masks. No AI filling.
set "ANGLE=180"
set /p "ANGLE=Horizontal coverage [180]: "
echo.
"%PYTHON%" "%ROOT%video_to_skybox.py" "%VIDEO%" --coverage "%ANGLE%" --output-root "%ROOT%output"
set "RESULT=%ERRORLEVEL%"
echo.
if not "%RESULT%"=="0" echo Failed. See the message above. Run Setup.bat if dependencies are missing.
pause
exit /b %RESULT%
:invalid
echo Video file not found.
pause
exit /b 2
