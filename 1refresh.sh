#!/bin/bash


# This script runs on Linux or macOS.
# It takes the source code of the Bibledit library,
# and puts it in the Windows project.
# Visual Studio can then build Bibledit for Windows.


# Work in the directory with Windows source, separate from Bibledit library source.
WINDOWSDIR=`dirname $0 | realpath`


echo Build the core library once more
# The purpose of this is to make sure
# that the autoconf and automake systems
# are in a consistent state.
# There were cases that an inconsistent system
# led to this script failing.
cd $WINDOWSDIR
if [ $? != 0 ]; then exit; fi
cd ../cloud
if [ $? != 0 ]; then exit; fi
make --jobs=4
if [ $? != 0 ]; then exit; fi


echo Remove all existing code.
# The point of doing this is to remove code
# that still is in the Windows port
# but no longer in the core library.
cd $WINDOWSDIR
if [ $? != 0 ]; then exit; fi
cd server
if [ $? != 0 ]; then exit; fi
find . -name "*.h" -delete
find . -name "*.c" -delete
find . -name "*.cpp" -delete


cd $WINDOWSDIR
if [ $? != 0 ]; then exit; fi
echo Pulling in the relevant Bibledit library source code.
rsync --archive --exclude '*.o' --exclude '*.a' --exclude '.dirstamp' --exclude 'server' --exclude 'unittest' --exclude '.DS_Store' --exclude 'autom4te.cache' --exclude 'bibledit' --exclude '*~' --exclude 'dev' --exclude 'generate' --exclude 'valgrind' --exclude 'AUTHORS' --exclude 'NEWS' --exclude 'README' --exclude 'ChangeLog' --exclude 'reconfigure' --exclude 'xcode' --exclude 'DEVELOP' --exclude '*.Po' ../cloud/* server
if [ $? != 0 ]; then exit; fi


echo Change directory to the core library.
cd server
if [ $? != 0 ]; then exit; fi


echo Prepare the sample Bible.
./configure
if [ $? != 0 ]; then exit; fi
make --jobs=4
if [ $? != 0 ]; then exit; fi
./generate . samplebible
if [ $? != 0 ]; then exit; fi
rm logbook/1*
make distclean
if [ $? != 0 ]; then exit; fi


echo Remove code that is no longer needed.
rm -rf unittests
rm -rf sources
rm -rf cloud.xcodeproj


# Configure the Bibledit source.
./configure --enable-windows
if [ $? != 0 ]; then exit; fi


# echo Set the network port it listens on.
# echo 9876 > config/network-port


# Remove some Linux related definitions as they don't work on Windows.
sed -i.bak '/HAVE_LIBPROC/d' config.h
if [ $? != 0 ]; then exit; fi
sed -i.bak '/HAVE_EXECINFO/d' config.h
if [ $? != 0 ]; then exit; fi
sed -i.bak '/HAVE_ICU/d' config.h
if [ $? != 0 ]; then exit; fi
sed -i.bak '/HAVE_PUGIXML/d' config.h
if [ $? != 0 ]; then exit; fi
sed -i.bak '/HAVE_UTF8PROC/d' config.h
if [ $? != 0 ]; then exit; fi


# Since Windows did have a few build problem on mbedTLS 3.x,
# use mbedTLS 2.x just now.
rm -rf mbedtls
if [ $? != 0 ]; then exit; fi
mv mbedtls2windows mbedtls
if [ $? != 0 ]; then exit; fi

# Disable threading in mbedTLS on Windows.
# sed -i.bak '/#define MBEDTLS_THREADING_C/d' mbedtls/mbedtls_config.h
sed -i.bak '/#define MBEDTLS_THREADING_C/d' mbedtls/config.h
if [ $? != 0 ]; then exit; fi
# sed -i.bak '/#define MBEDTLS_THREADING_PTHREAD/d' mbedtls/mbedtls_config.h
sed -i.bak '/#define MBEDTLS_THREADING_PTHREAD/d' mbedtls/config.h
if [ $? != 0 ]; then exit; fi


# Remove files and folders no longer needed after configure.
find . -name ".deps" -type d -prune -exec rm -rf '{}' '+'
rm aclocal.m4
rm compile
rm config.guess
rm *.bak
rm mbedtls/*.bak
rm config.h.in
rm config.log
rm config.status
rm config.sub
rm configure
rm configure.ac
rm depcomp
rm INSTALL
rm install-sh
rm Makefile*
rm missing
rm stamp-h1
rmdir unittests
rmdir sources
rm -rf autom4te.cache


# Clean stuff out.
cd $WINDOWSDIR
if [ $? != 0 ]; then exit; fi
find . -name .DS_Store -delete


# Update Inno Setup script.
cd $WINDOWSDIR
if [ $? != 0 ]; then exit; fi
VERSION=`sed -n -e 's/^.* PACKAGE_VERSION //p' server/config.h | tr -d '"'`
echo Version $VERSION
sed -i.bak "s/AppVersion=.*/AppVersion=$VERSION/" package.iss
if [ $? != 0 ]; then exit; fi
sed -i.bak "s/OutputBaseFilename=.*/OutputBaseFilename=bibledit-$VERSION/" package.iss
if [ $? != 0 ]; then exit; fi
rm package.iss.bak


echo Ready
