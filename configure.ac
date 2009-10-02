AC_INIT([meshwork], 0.1, [meshwork@lists.filefind.net])
AC_PREREQ(2.53)
AC_CANONICAL_SYSTEM
AM_INIT_AUTOMAKE([1.9 tar-ustar])
AM_MAINTAINER_MODE

AC_PROG_INTLTOOL([0.25])

AC_PROG_INSTALL

dnl pkg-config
AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
	AC_MSG_ERROR([You need to install pkg-config])
fi

dnl C# compiler
AC_PATH_PROG(CSC,  csc,  no)
AC_PATH_PROG(MCS,  gmcs, no)
AC_PATH_PROG(MONO, mono, no)

CS="C#"
if test "x$CSC" = "xno" -a "x$MCS" = "xno"  ; then
	dnl AC_MSG_ERROR([You need to install a C# compiler])
	AC_MSG_ERROR([No $CS compiler found])
fi

if test "x$MCS" = "xno" ; then
	MCS=$CSC
fi

if test "x$MONO" = "xno"; then
	AC_MSG_ERROR([No mono runtime found])
fi

dnl library dependencies
MONO_REQUIRED_VERSION=1.2.6
GTKSHARP_REQUIRED_VERSION=2.8.2
MONO_CAIRO_REQUIRED_VERSION=1.1.13

PKG_CHECK_MODULES(MONO, mono >= $MONO_REQUIRED_VERSION)

PKG_CHECK_MODULES(GTKSHARP, gtk-sharp-2.0 >= $GTKSHARP_REQUIRED_VERSION)
AC_SUBST(GTK_SHARP_LIBS)

PKG_CHECK_MODULES(GLADE_SHARP, glade-sharp-2.0 >= $GTKSHARP_REQUIRED_VERSION)
AC_SUBST(GLADE_SHARP_LIBS)

PKG_CHECK_MODULES(MONO_CAIRO, mono-cairo >= $MONO_CAIRO_REQUIRED_VERSION)
AC_SUBST(MONO_CAIRO_LIBS)

dnl Intl
ALL_LINGUAS=""
GETTEXT_PACKAGE=meshwork
AC_SUBST(GETTEXT_PACKAGE)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE,"$GETTEXT_PACKAGE", [Gettext package])
AM_GLIB_GNU_GETTEXT

# CSC_FLAGS="-langversion:ISO-1"
CSC_FLAGS=""

MESHWORK_DIR='$(prefix)/lib/meshwork'

AC_SUBST(MCS)
AC_SUBST(MESHWORK_DIR)
AC_SUBST(CSC_FLAGS)

AC_OUTPUT([
meshwork.pc
meshwork-gtk.pc
Makefile
src/Makefile
src/FileFind.Meshwork/Makefile
src/FileFind.Meshwork.GtkClient/Makefile
contrib/Makefile
resources/Makefile
po/Makefile.in
plugins/Makefile
plugins/Debug/Makefile
])

echo "
meshwork-$VERSION

    Install Prefix:    ${prefix}
    Mono C# Compiler:  ${MCS}
    Mono Runtime:      ${MONO}
"
