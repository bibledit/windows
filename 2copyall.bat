@echo off


echo This will copy all Bibledit files into a staging directory


echo Clearing staging directory
rmdir /S /Q C:\bibledit-windows
mkdir C:\bibledit-windows
if %errorlevel% neq 0 ( pause; exit /b %errorlevel% )


echo Copying Bibledit supporting data into staging directory
xcopy server\* C:\bibledit-windows /E /I /Y /Q /EXCLUDE:exclude.txt
if %errorlevel% neq 0 ( pause; exit /b %errorlevel% )


echo Copying kernel binary into staging directory
if exists "x64\Release\server.exe" (
    echo Copy x64
    copy x64\Release\server.exe C:\bibledit-windows /Y
    if %errorlevel% neq 0 ( pause; exit /b %errorlevel% )
)
else if exists "ARM64\Release\server.exe" (
    echo Copy arm64
    copy ARM64\Release\server.exe C:\bibledit-windows /Y
    if %errorlevel% neq 0 ( pause; exit /b %errorlevel% )
)
else (
     echo Cannot find server.exe
     exit /b 1
)


echo Copying GUI binaries into staging directory
xcopy gui\bibledit\bin\Release\* C:\bibledit-windows /E /I /Y /Q
if %errorlevel% neq 0 ( pause; exit /b %errorlevel% )


echo Cleaning unwanted files from staging directory
rmdir /S /Q C:\bibledit-windows\Debug
rmdir /S /Q C:\bibledit-windows\x64


echo If the script gets here, all went well
pause
