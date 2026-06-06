@echo off
set CSC_PATH=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

echo ===================================================
echo     Compiling UltimateTweakTool.exe (GUI Application)
echo ===================================================
echo.

if not exist "%CSC_PATH%" (
    echo [ERROR] .NET Framework 4.0 Compiler (csc.exe) not found!
    echo Please verify that .NET Framework 4.0 or newer is installed.
    pause
    exit /b 1
)

echo Compiling WinDebloater.cs...
"%CSC_PATH%" /nologo /out:"UltimateTweakTool.exe" /target:winexe /platform:anycpu WinDebloater.cs
if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Compilation failed!
    pause
    exit /b %errorlevel%
)

echo.
echo [✓] Build Completed successfully! UltimateTweakTool.exe is ready.
echo ===================================================
pause
