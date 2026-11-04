@echo off
setlocal
set REPO=%~dp0..
cd /d "%REPO%"

git config --global --add safe.directory "%REPO%"

echo [1/3] Fetching remote history...
git fetch origin --tags

if errorlevel 1 goto :error

echo [2/3] Backing up current branch...
git branch backup-before-reset 2>nul

if errorlevel 1 goto :error

echo [3/3] Resetting local main to origin/main...
git switch main 2>nul || git switch -c main
git pull --ff-only origin main
git reset --hard origin/main

echo.
echo Done. Local main is now aligned with origin/main.
goto :end

:error
echo ERROR: the reset script failed.
exit /b 1

:end
endlocal
