#!/bin/bash


# This script runs on Linux or macOS.
# It takes the source code of the Bibledit library,
# and puts it in the Windows project.
# Visual Studio can then build Bibledit for Windows.


# Exit script on error.
set -e


# Work in the directory with Windows source, separate from Bibledit library source.
WINDOWS_DIR=$(dirname "$0" | realpath)
cd "$WINDOWS_DIR"


echo Remove all existing code.
# Purpose: Remove code still in the Windows port but no longer in the core library.
pushd server
find . -name "*.h" -delete
find . -name "*.c" -delete
find . -name "*.cpp" -delete
popd


echo Pulling in the relevant Bibledit library source code.
rsync --archive --exclude ".idea" --exclude "build" --exclude "cmake-build-debug" ../cloud/* server


echo Change directory to the core library.
cd server


echo Prepare the sample Bible.
rm -rf build
cmake -B build
cmake --build build --target generate -j 4
./build/generate . samplebible
rm -f logbook/1*


# Configure the Bibledit source.
cmake -B build -DHAVE_WINDOWS=ON -DBUILD_UNITTESTS=OFF


# Remove some Linux related definitions as they don't work on Windows.
sed -i.bak '/HAVE_LIBPROC/d' config.h
sed -i.bak '/HAVE_EXECINFO/d' config.h
sed -i.bak '/HAVE_ICU/d' config.h
sed -i.bak '/HAVE_PUGIXML/d' config.h
sed -i.bak '/HAVE_UTF8PROC/d' config.h


# Windows now uses mbedTLS 3.x.
# Remove mbedTLS 2.x.
rm -rf mbedtls2


# Disable threading in mbedTLS on Windows.
sed -i.bak '/#define MBEDTLS_THREADING_C/d' mbedtls/mbedtls_config.h
sed -i.bak '/#define MBEDTLS_THREADING_PTHREAD/d' mbedtls/mbedtls_config.h


echo Remove files no longer needed.
rm -rf unittests
rm -rf sources
rm -rf build
rm AUTHORS
rm ChangeLog
rm CMakeLists.txt
rm dev
rm DEVELOP
rm -rf gtk
rm INSTALL
rm NEWS
rm valgrind
rm config.h.in
find . -name ".dirstamp" -delete
find . -name "*.bak" -delete
rm bibledit


# Clean stuff out.
cd "$WINDOWS_DIR"
find . -name .DS_Store -delete


# Update Inno Setup script.
cd "$WINDOWS_DIR"
VERSION=$(grep -w "VERSION" server/config.h | sed -n -e 's/^.* VERSION //p' | tr -d '"')
echo Version "$VERSION"
sed -i.bak "s/AppVersion=.*/AppVersion=$VERSION/" package.iss
sed -i.bak "s/OutputBaseFilename=.*/OutputBaseFilename=bibledit-$VERSION/" package.iss
rm package.iss.bak


echo Ready
