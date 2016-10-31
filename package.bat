@echo off


echo This will build and package Bibledit


echo Setting environment
setlocal
SET PATH=%PATH%;C:\Program Files\Git\cmd
SET PATH=%PATH%;C:\Program Files (x86)\Microsoft Visual Studio 14.0\VC
call vcvarsall.bat x86
SET PATH=%PATH%;C:\Program Files (x86)\Inno Setup 5


echo Cloning repository
rmdir /S /Q C:\bibledit-windows
if %errorlevel% neq 0 (
pause
exit /b %errorlevel%
)
cd C:\
git clone --depth 1 https://github.com/bibledit/bibledit-windows.git
if %errorlevel% neq 0 (
pause
exit /b %errorlevel%
)


echo Staging files for packager
cd C:\
rmdir /S /Q C:\bibledit-windows-packager
mkdir C:\bibledit-windows-packager
xcopy C:\bibledit-windows\server\* C:\bibledit-windows-packager /E /I /Y
if %errorlevel% neq 0 (
pause
exit /b %errorlevel%
)
del C:\bibledit-windows-packager\server.*
copy /Y C:\bibledit-windows\package.iss C:\bibledit-windows-packager
if %errorlevel% neq 0 (
pause
exit /b %errorlevel%
)


echo Building
cd C:\bibledit-windows
msbuild /property:Configuration=Release /property:Platform=x86
if %errorlevel% neq 0 (
pause
exit /b %errorlevel%
)


echo Staging binaries for packager
copy /Y C:\bibledit-windows\Release\server.exe C:\bibledit-windows-packager
if %errorlevel% neq 0 (
pause
exit /b %errorlevel%
)
copy /Y C:\bibledit-windows\gui\bibledit\bin\Release\bibledit.exe C:\bibledit-windows-packager
if %errorlevel% neq 0 (
pause
exit /b %errorlevel%
)
copy /Y C:\bibledit-windows\vc\*.exe C:\bibledit-windows-packager
if %errorlevel% neq 0 (
pause
exit /b %errorlevel%
)


echo Creating the Bibledit setup .exe
cd C:\bibledit-windows-packager
ISCC package.iss
if %errorlevel% neq 0 (
pause
exit /b %errorlevel%
)


IF "%1" == "" (
pause
) 

