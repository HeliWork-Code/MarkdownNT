@echo off
setlocal
set REPO=%~dp0..
cd /d "%REPO%"

git config --global --add safe.directory "%REPO%"

echo Reusing the known fix commit from the feature branch...
git switch feature/markdownnt-fix
if errorlevel 1 goto :error

git cherry-pick 543dab6
if errorlevel 1 goto :error

echo.
echo Fix commit applied to the fresh branch.
goto :end

:error
echo ERROR: cherry-pick failed. Resolve the conflicts in the repo, then run:
	echo git add .
	echo git cherry-pick --continue
	exit /b 1

:end
endlocal
