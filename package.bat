@echo off


echo This will build and package Bibledit


echo Setting environment
setlocal
SET PATH=%PATH%;C:\Program Files\Git\cmd
rem Visual Studio 2015.
rem SET PATH=%PATH%;C:\Program Files (x86)\Microsoft Visual Studio 14.0\VC
rem Visual Studio 2019.
rem SET PATH=%PATH%;C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build
call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvarsall.bat" x86
SET PATH=%PATH%;C:\Program Files (x86)\Inno Setup 5


echo Cloning repository
rmdir /S /Q C:\bibledit-windows
if %errorlevel% neq 0 (
pause
exit /b %errorlevel%
)
cd C:\
git clone --depth 1 https://github.com/bibledit/windows.git bibledit-windows
if %errorlevel% neq 0 (
pause
exit /b %errorlevel%
)
rmdir /S /Q C:\bibledit-windows\.git


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
xcopy C:\bibledit-windows\CefSharp_x32\* C:\bibledit-windows-packager /E /I /Y
if %errorlevel% neq 0 (
pause
exit /b %errorlevel%
)


echo Building the core library component
cd C:\bibledit-windows
msbuild server.sln /property:Configuration=Release /property:Platform=x86
if %errorlevel% neq 0 (
pause
exit /b %errorlevel%
)


echo Staging core library for packager
copy /Y C:\bibledit-windows\Release\server.exe C:\bibledit-windows-packager
if %errorlevel% neq 0 (
pause
exit /b %errorlevel%
)


echo Building
cd C:\bibledit-windows
# For some reason the bibledit.exe built in Release mode does not start with the WebKit library.
# msbuild gui.sln /property:Configuration=Release /property:Platform=x86
# The bibledit.exe built in Debug mode works with the WebKit library.
msbuild gui.sln /property:Configuration=Debug /property:Platform=x86
if %errorlevel% neq 0 (
pause
exit /b %errorlevel%
)


echo Staging Windows executables for packager
copy /Y C:\bibledit-windows\gui\bibledit.exe C:\bibledit-windows-packager
if %errorlevel% neq 0 (
pause
exit /b %errorlevel%
)
copy /Y C:\bibledit-windows\vc\*.exe C:\bibledit-windows-packager
if %errorlevel% neq 0 (
pause
exit /b %errorlevel%
)


echo Creating the Bibledit setup.exe
cd C:\bibledit-windows-packager
ISCC package.iss
if %errorlevel% neq 0 (
pause
exit /b %errorlevel%
)


rem echo Signing the setup.exe
rem cd C:\bibledit-windows-packager\Output
rem "C:\Program Files (x86)\Microsoft SDKs\Windows\v7.1A\Bin\signtool" sign /f "C:\Users\Teus Benschop\Desktop\TeunisBenschop.pfx" /p "" /tr http://tsa.startssl.com/rfc3161 bibledit-*.exe
rem if %errorlevel% neq 0 (
rem pause
rem exit /b %errorlevel%
rem )


IF "%1" == "" (
pause
) 

