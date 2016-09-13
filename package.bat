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


echo Building
cd C:\bibledit-windows
msbuild /property:Configuration=Release /property:Platform=x86


pause
