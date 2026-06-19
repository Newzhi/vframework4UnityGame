@echo off
setlocal
REM Create sample TestExcels/Hero.xlsx

chcp 65001 >nul 2>&1
cd /d "%~dp0"

where dotnet >nul 2>&1
if errorlevel 1 (
    echo [ERROR] dotnet SDK not found. Install .NET 8 SDK and retry.
    pause
    exit /b 1
)

echo ========================================
echo   ExcelTool Create Sample
echo ========================================
echo.

dotnet run --project "ExcelTool\ExcelTool.csproj" -c Debug -- --create-sample
set "EXIT_CODE=%ERRORLEVEL%"

if not "%EXIT_CODE%"=="0" (
    echo.
    echo [FAILED] Create sample failed, exit code: %EXIT_CODE%
    pause
    exit /b %EXIT_CODE%
)

echo.
echo [OK] Sample created.
pause
exit /b 0
