echo This will copy all Bibledit files into a staging directory


echo Clearing staging directory
rmdir /S /Q C:\bibledit-windows
mkdir C:\bibledit-windows
if %errorlevel% neq 0 ( pause; exit /b %errorlevel% )


echo Copying Bibledit supporting data into staging directory
xcopy server\* C:\bibledit-windows /E /I /Y /Q /EXCLUDE:exclude.txt
if %errorlevel% neq 0 ( pause; exit /b %errorlevel% )


echo Getting processor architecture
set arch=%processor_architecture%
echo %arch%
if %arch%==AMD64 (
    goto archready
)
if %arch%==ARM64 (
    goto archready
)
echo Unsupported architecture %arch%
exit /b 1
:archready


echo Copying kernel binary into staging directory

if %arch%==AMD64 (
    echo Copy x64
    copy x64\Release\server.exe C:\bibledit-windows /Y
    if %errorlevel% neq 0 ( pause; exit /b %errorlevel% )
)

if %arch%==ARM64 (
    echo Copy arm64
    copy ARM64\Release\server.exe C:\bibledit-windows /Y
    if %errorlevel% neq 0 ( pause; exit /b %errorlevel% )
)


echo Copying GUI binaries into staging directory

if %arch%==AMD64 (
    echo Copy x64
    xcopy gui\bibledit\bin\Release\* C:\bibledit-windows /E /I /Y /Q
    if %errorlevel% neq 0 ( pause; exit /b %errorlevel% )
)

if %arch%==ARM64 (
    echo Copy arm64
    xcopy gui\bibledit\bin\ARM64\Release\* C:\bibledit-windows /E /I /Y /Q
    if %errorlevel% neq 0 ( pause; exit /b %errorlevel% )
)



echo Copying Microsoft WebView2 binaries

if %arch%==AMD64 (
    echo Copy x64
)

if %arch%==ARM64 (
    echo Copy arm64
    pushd packages
    pushd M*
    xcopy runtimes\win-arm64\native\* C:\bibledit-windows /E /I /Y /Q
    if %errorlevel% neq 0 ( pause; exit /b %errorlevel% )
    popd
    popd
)


echo Cleaning unwanted files from staging directory
if %arch%==AMD64 (
    rmdir /S /Q C:\bibledit-windows\Debug
    rmdir /S /Q C:\bibledit-windows\Release
    rmdir /S /Q C:\bibledit-windows\x64
)


echo If the script gets here, all went well
pause
