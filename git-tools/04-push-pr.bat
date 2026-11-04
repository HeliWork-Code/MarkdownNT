@echo off
setlocal
set REPO=%~dp0..
cd /d "%REPO%"

git config --global --add safe.directory "%REPO%"

echo [1/3] Checking status...
git status
if errorlevel 1 goto :error

echo [2/3] Pushing branch...
git push -u origin feature/markdownnt-fix
if errorlevel 1 goto :error

echo [3/3] Creating pull request...
gh pr create --repo HeliWork-Code/MarkdownNT --base main --head feature/markdownnt-fix --fill
if errorlevel 1 goto :error

echo.
echo Done. PR creation was attempted.
goto :end

:error
echo ERROR: push or PR failed. Check git status and branch history.
exit /b 1

:end
endlocal
