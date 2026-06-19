@echo off
setlocal
REM ExcelTool export: TestExcels -> MetaConfigs + AssetBundle/ConfigTables
REM Double-click or run from cmd in this folder.

chcp 65001 >nul 2>&1
cd /d "%~dp0"

where dotnet >nul 2>&1
if errorlevel 1 (
    echo [ERROR] dotnet SDK not found. Install .NET 8 SDK and retry.
    echo https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo ========================================
echo   ExcelTool Export
echo ========================================
echo.

dotnet run --project "ExcelTool\ExcelTool.csproj" -c Debug -- %*
set "EXIT_CODE=%ERRORLEVEL%"

if not "%EXIT_CODE%"=="0" (
    echo.
    echo [FAILED] Export failed, exit code: %EXIT_CODE%
    pause
    exit /b %EXIT_CODE%
)

echo.
echo [OK] Export finished.
pause
exit /b 0
