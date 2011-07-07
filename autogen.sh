#!/bin/bash -e

# Usage:
#     compare_versions MIN_VERSION ACTUAL_VERSION
# returns true if ACTUAL_VERSION >= MIN_VERSION
# NOTE: Thie method was taken from gnome-autogen.sh
compare_versions() {
    ch_min_version=$1
    ch_actual_version=$2
    ch_status=0
    IFS="${IFS=         }"; ch_save_IFS="$IFS"; IFS="."
    set $ch_actual_version
    for ch_min in $ch_min_version; do
        ch_cur=`echo $1 | sed 's/[^0-9].*$//'`; shift # remove letter suffixes
        if [ -z "$ch_min" ]; then break; fi
        if [ -z "$ch_cur" ]; then ch_status=1; break; fi
        if [ $ch_cur -gt $ch_min ]; then break; fi
        if [ $ch_cur -lt $ch_min ]; then ch_status=1; break; fi
    done
    IFS="$ch_save_IFS"
    return $ch_status
}

# checking for automake 1.9+
am_version=`automake --version | cut -f 4 -d ' ' | head -n 1`
if ! compare_versions 1.9 $am_version; then
	echo "**Error**: automake 1.9+ required.";
	exit 1;
fi

# checking for aclocal 1.9+
al_version=`aclocal --version | cut -f 4 -d ' ' | head -n 1`
if ! compare_versions 1.9 $al_version; then
	echo "**Error**: aclocal 1.9+ required.";
	exit 1;
fi

echo "Running glib-gettextize ..."
glib-gettextize --force --copy ||
	{ echo "**Error**: glib-gettextize failed."; exit 1; }

echo "Running intltoolize ..."
intltoolize --force --copy --automake ||
	{ echo "**Error**: intltoolize failed."; exit 1; }

echo "Running aclocal $ACLOCAL_FLAGS ..."
aclocal $ACLOCAL_GLAGS || {
  echo
  echo "**Error**: aclocal failed. This may mean that you have not"
  echo "installed all of the packages you need, or you may need to"
  echo "set ACLOCAL_FLAGS to include \"-I \$prefix/share/aclocal\""
  echo "for the prefix where you installed the packages whose"
  echo "macros were not found"
  exit 1
}

echo "Running automake --gnu $am_opt ..."
automake --add-missing --gnu $am_opt ||
	{ echo "**Error**: automake failed."; exit 1; }

echo "running autoconf ..."
WANT_AUTOCONF=2.5 autoconf || {
  echo "**Error**: autoconf failed."; exit 1; }

conf_flags="--enable-maintainer-mode --enable-compile-warnings"

if test x$NOCONFIGURE = x; then
  echo Running $srcdir/configure $conf_flags "$@" ...
  ./configure $conf_flags "$@" \
  && echo Now type \`make\' to compile $PKG_NAME || exit 1
else
  echo Skipping configure process.
fi

