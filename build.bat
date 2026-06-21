@echo off
setlocal

REM Locate the .NET Framework C# compiler (prefer 64-bit, fall back to 32-bit).
set "CSC_PATH=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC_PATH%" set "CSC_PATH=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"

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

echo Compiling WinDebloater.cs ...
"%CSC_PATH%" /nologo /optimize+ /out:"UltimateTweakTool.exe" /target:winexe /platform:anycpu /win32manifest:"app.manifest" WinDebloater.cs AssemblyInfo.cs
if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Compilation failed!
    pause
    exit /b %errorlevel%
)

echo.
echo [OK] Build completed successfully! UltimateTweakTool.exe is ready.
echo ===================================================
pause
