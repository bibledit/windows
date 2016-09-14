@echo off


echo This will build and package Bibledit


echo Setting environment
setlocal
SET PATH=%PATH%;C:\Program Files\Git\cmd
SET PATH=%PATH%;C:\Program Files (x86)\Microsoft Visual Studio 14.0\VC
call vcvarsall.bat x86


echo Cloning repository
rmdir /S /Q C:\bibledit-windows
cd C:\
git clone --depth 1 https://github.com/bibledit/bibledit-windows.git


echo Staging files for packager
cd C:\
mkdir bibledit-windows-packager
xcopy bibledit-windows\server\* bibledit-windows-packager /E /I /Y
del bibledit-windows-packager\server.*


echo Building
cd C:\bibledit-windows
rem msbuild /property:Configuration=Release /property:Platform=x86


pause
