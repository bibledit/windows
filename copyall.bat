@echo off


echo This will copy all Bibledit files into a staging directory


echo Clearing staging directory
rmdir /S /Q C:\bibledit-windows
mkdir C:\bibledit-windows
if %errorlevel% neq 0 ( pause; exit /b %errorlevel% )


echo Copying Bibledit supporting data into staging directory
xcopy server\* C:\bibledit-windows /E /I /Y /Q
if %errorlevel% neq 0 ( pause; exit /b %errorlevel% )


echo Copying kernel binary into staging directory
copy Debug\server.exe C:\bibledit-windows /Y
if %errorlevel% neq 0 ( pause; exit /b %errorlevel% )


echo Copying GUI binaries into staging directory
xcopy gui\bibledit\bin\Release\* C:\bibledit-windows /E /I /Y /Q
if %errorlevel% neq 0 ( pause; exit /b %errorlevel% )


echo Cleaning unwanted files from staging directory
del C:\bibledit-windows\server.*
rmdir /S /Q C:\bibledit-windows\Debug


echo If the script gets here, all went well
pause
