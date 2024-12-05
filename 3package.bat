@echo off


echo This will package Bibledit


echo Setting environment
setlocal
SET PATH=%PATH%;C:\Program Files (x86)\Inno Setup 6
if %errorlevel% neq 0 ( pause; exit /b %errorlevel% )


echo Copying the setup script
copy package.iss C:\bibledit-windows /Y
if %errorlevel% neq 0 ( pause; exit /b %errorlevel% )


echo Copying the Visual C redistributables
xcopy vc\*.exe C:\bibledit-windows /E /I /Y /Q
if %errorlevel% neq 0 ( pause; exit /b %errorlevel% )


echo Creating the Bibledit setup.exe
cd C:\bibledit-windows
if %errorlevel% neq 0 ( pause; exit /b %errorlevel% )
ISCC package.iss
if %errorlevel% neq 0 ( pause; exit /b %errorlevel% )


pause

