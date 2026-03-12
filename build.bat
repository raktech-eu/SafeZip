@echo off
setlocal

:: ── Locate csc.exe (tries Framework64 v4 first, then v4 32-bit) ──────────────
set CSC=
for %%V in (v4.0.30319 v3.5) do (
    if exist "%WINDIR%\Microsoft.NET\Framework64\%%V\csc.exe" (
        set "CSC=%WINDIR%\Microsoft.NET\Framework64\%%V\csc.exe"
        goto :found
    )
    if exist "%WINDIR%\Microsoft.NET\Framework\%%V\csc.exe" (
        set "CSC=%WINDIR%\Microsoft.NET\Framework\%%V\csc.exe"
        goto :found
    )
)

echo ERROR: csc.exe not found. Install .NET Framework 4.x.
pause & exit /b 1

:found
echo Using: %CSC%

:: ── Check required files ──────────────────────────────────────────────────────
if not exist "SafeZip.cs"   ( echo ERROR: SafeZip.cs not found.   & pause & exit /b 1 )
if not exist "SafeZip.ico"  ( echo ERROR: SafeZip.ico not found.  & pause & exit /b 1 )
if not exist "README.md"    ( echo ERROR: README.md not found.    & pause & exit /b 1 )

:: ── Compile ───────────────────────────────────────────────────────────────────
"%CSC%" ^
    /target:winexe ^
    /optimize+ ^
    /out:SafeZip.exe ^
    /win32icon:SafeZip.ico ^
    /res:SafeZip.ico ^
    /res:README.md ^
    /reference:System.dll ^
    /reference:System.Drawing.dll ^
    /reference:System.Windows.Forms.dll ^
    SafeZip.cs

if %ERRORLEVEL% neq 0 (
    echo.
    echo BUILD FAILED.
    pause & exit /b %ERRORLEVEL%
)

echo.
echo BUILD OK  -^>  SafeZip.exe
pause
endlocal
