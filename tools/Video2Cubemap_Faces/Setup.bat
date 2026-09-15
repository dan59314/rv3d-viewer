@echo off
setlocal
cd /d "%~dp0"
where python >nul 2>nul
if errorlevel 1 goto :missing
python -m venv ".venv"
if errorlevel 1 goto :failed
".venv\Scripts\python.exe" -m pip install -r requirements.txt
if errorlevel 1 goto :failed
echo Setup complete. Run Video2Skybox.bat.
pause
exit /b 0
:missing
echo Python 3.10 or newer is required. Install Python and enable Add Python to PATH.
:failed
echo Setup failed. Check the error above and your network connection.
pause
exit /b 1
