@echo off


cd %~p0
if %errorlevel% neq 0 ( pause; exit /b )
echo | set /p="Working directory is "
cd


echo No need to restore dependencies of the C++ server solution
echo Restoring dependencies of the GUI solution
dotnet restore gui.sln
if %errorlevel% neq 0 ( pause; exit /b )


echo Build the server solution
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe"^
 server.sln /p:RestorePackages=false /p:Configuration=Release /p:Platform=x64
if %errorlevel% neq 0 ( pause; exit /b )


echo Build the GUI solution
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe"^
 gui.sln /p:RestorePackages=false /p:Configuration=Release /p:Platform=x64
if %errorlevel% neq 0 ( pause; exit /b )


set staging_dir=%USERPROFILE%\Desktop\BibleditStaging


echo Creating the staging directory at %staging_dir%
if EXIST %staging_dir% (
    rmdir /S /Q %staging_dir%
    if %errorlevel% neq 0 ( pause; exit /b )
)
mkdir %staging_dir%
if %errorlevel% neq 0 ( pause; exit /b )


echo Copying Bibledit supporting data into staging directory
xcopy server\* %staging_dir% /E /I /Y /Q /EXCLUDE:exclude.txt
if %errorlevel% neq 0 ( pause; exit /b )


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
    copy x64\Release\server.exe %staging_dir% /Y
    if %errorlevel% neq 0 ( pause; exit /b )
)

if %arch%==ARM64 (
    echo Copy arm64
    copy ARM64\Release\server.exe %staging_dir% /Y
    if %errorlevel% neq 0 ( pause; exit /b )
)


echo Copying GUI binaries into staging directory

if %arch%==AMD64 (
    echo Copy x64
    xcopy gui\bibledit\bin\Release\* %staging_dir% /E /I /Y /Q
    if %errorlevel% neq 0 ( pause; exit /b )
)

if %arch%==ARM64 (
    echo Copy arm64
    xcopy gui\bibledit\bin\ARM64\Release\* %staging_dir% /E /I /Y /Q
    if %errorlevel% neq 0 ( pause; exit /b )
)


echo Copying Microsoft WebView2 binaries

if %arch%==AMD64 (
    echo Copy x64
)

if %arch%==ARM64 (
    echo Copy arm64
    pushd packages
    if %errorlevel% neq 0 ( pause; exit /b )
    pushd M*
    if %errorlevel% neq 0 ( pause; exit /b )
    xcopy runtimes\win-arm64\native\* %staging_dir% /E /I /Y /Q
    if %errorlevel% neq 0 ( pause; exit /b )
    popd
    popd
)


echo Cleaning unwanted files from staging directory
if %arch%==AMD64 (
	if EXIST %staging_dir%\Debug (
		rmdir /S /Q %staging_dir%\Debug
		if %errorlevel% neq 0 ( pause; exit /b )
	)
	if EXIST %staging_dir%\Release (
		rmdir /S /Q %staging_dir%\Release
		if %errorlevel% neq 0 ( pause; exit /b )
	)
	if EXIST %staging_dir%\x64 (
		rmdir /S /Q %staging_dir%\x64
		if %errorlevel% neq 0 ( pause; exit /b )
	)
)


echo Setting environment for packager
setlocal
SET PATH=%PATH%;C:\Program Files (x86)\Inno Setup 6
if %errorlevel% neq 0 ( pause; exit /b )


echo Copying the setup script
copy package.iss %staging_dir% /Y
if %errorlevel% neq 0 ( pause; exit /b )


echo Copying the Visual C redistributables
xcopy vc\*.exe %staging_dir% /E /I /Y /Q
if %errorlevel% neq 0 ( pause; exit /b )


echo Creating the Bibledit setup.exe
cd %staging_dir%
if %errorlevel% neq 0 ( pause; exit /b )
ISCC package.iss
if %errorlevel% neq 0 ( pause; exit /b )


echo The builder is ready
pause
