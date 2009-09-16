SUBDIRS = contrib po resources src plugins
bin_SCRIPTS = meshwork

desktopdir = $(datadir)/applications
desktop_DATA = meshwork.desktop

# for application icon
pixmapdir = $(datadir)/pixmaps
pixmap_DATA = 

# for linking with pkg-config at some point
pkgconfig_in_files = meshwork.pc.in meshwork-gtk.pc.in

pkgconfigdir=$(prefix)/lib/pkgconfig
pkgconfig_DATA=$(pkgconfig_in_files:.pc.in=.pc)

run:
	$(MONO) --debug $(top_builddir)/build/FileFind.Meshwork.GtkClient.exe

REWRITE =                                   \
	sed -e "s|\@prefix\@|$(prefix)|g"       \
		-e "s|\@pkglibdir\@|$(pkglibdir)|g" \
		-e "s|\@bindir\@|$(bindir)|g"       \
		-e "s|\@MONO\@|$(MONO)|g"

$(bin_SCRIPTS): $(srcdir)/meshwork.in Makefile
	$(REWRITE) $(srcdir)/meshwork.in > $@.tmp
	mv $@.tmp $@

CLEANFILES = \
	intltool-merge   \
	intltool-extract \
	intltool-update  \
	meshwork         \
	meshwork.pc      \
	meshwork-gtk.pc

EXTRA_DIST = \
	meshwork.in         \
	meshwork.desktop    \
	meshwork.pc.in      \
	intltool-extract.in \
	intltool-merge.in   \
	intltool-update.in

DISCLEANFILES = \
	configure    \
	config.guess \
	config.sub   \
	Makefile.in  \
	install-sh   \
	aclocal.m4   \
	INSTALL      \
	COPYING      \
	missing
