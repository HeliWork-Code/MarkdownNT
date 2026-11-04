@echo off
setlocal
set REPO=%~dp0..
cd /d "%REPO%"

git config --global --add safe.directory "%REPO%"

git fetch origin --tags

echo Recreating feature branch from origin/main...
git switch main
if errorlevel 1 goto :error

git pull --ff-only origin main
if errorlevel 1 goto :error

git branch backup-current-feature 2>nul

git switch -C feature/markdownnt-fix
if errorlevel 1 goto :error

echo.
echo Feature branch recreated from origin/main.
goto :end

:error
echo ERROR: creating the feature branch failed.
exit /b 1

:end
endlocal
