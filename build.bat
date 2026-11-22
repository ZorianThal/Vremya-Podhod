@echo off
cd /d "%~dp0"
title Building VremyaPodhod (C# 2.0 WinForms)...

echo ========================================================
echo   Vremya-Podhod: Shockwave & Approach Time Calculator
echo   Building standalone application...
echo ========================================================
echo.

if not exist "%~dp0Program.cs" (
    echo [ERROR] Program.cs was not found!
    echo If running inside a ZIP archive, please right click the zip,
    echo select 'Extract All...', and run build.bat from the extracted folder.
    echo.
    pause
    exit /b 1
)

set CSC=
if exist "%windir%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" set CSC="%windir%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if "%CSC%"=="" if exist "%windir%\Microsoft.NET\Framework\v4.0.30319\csc.exe" set CSC="%windir%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if "%CSC%"=="" if exist "%windir%\Microsoft.NET\Framework64\v2.0.50727\csc.exe" set CSC="%windir%\Microsoft.NET\Framework64\v2.0.50727\csc.exe"
if "%CSC%"=="" if exist "%windir%\Microsoft.NET\Framework\v2.0.50727\csc.exe" set CSC="%windir%\Microsoft.NET\Framework\v2.0.50727\csc.exe"

if "%CSC%"=="" (
    echo [ERROR] .NET Framework compiler (csc.exe) was not found!
    echo Please ensure Microsoft .NET Framework 2.0, 3.5, 4.0 or higher is installed.
    pause
    exit /b 1
)

echo Using compiler: %CSC%
echo Compiling Program.cs...
%CSC% /nologo /target:winexe /out:VremyaPodhod.exe /r:System.dll,System.Drawing.dll,System.Windows.Forms.dll "%~dp0Program.cs"

if %ERRORLEVEL% equ 0 (
    echo.
    echo [SUCCESS] Build completed! Starting VremyaPodhod.exe...
    start "" "%~dp0VremyaPodhod.exe"
) else (
    echo.
    echo [FAILED] Compilation failed with error code %ERRORLEVEL%.
    pause
)
