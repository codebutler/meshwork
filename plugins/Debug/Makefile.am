MCS_FLAGS = -t:library -debug

DLL = $(top_builddir)/build/plugins/DebugPlugin.dll

REFERENCES = \
	-r:Mono.Posix        \
	-pkg:gtk-sharp-2.0   \
	-pkg:glade-sharp-2.0 \
	-r:$(top_builddir)/build/FileFind.Meshwork.dll \
	-r:$(top_builddir)/build/FileFind.Meshwork.GtkClient.exe

RESOURCES = \
	-resource:debugplugin.glade    \
	-resource:DebugPluginMenus.xml 

FILES = \
	DebugPlugin.cs      \
	DebugActionGroup.cs \
	DebugWindow.cs

all: $(DLL)

$(DLL): $(FILES)
	mkdir -p $(top_builddir)/build/plugins
	$(MCS) $(MCS_FLAGS) $(REFERENCES) $(RESOURCES) -t:library -out:$@ $(FILES)

clean:
	rm -f $(DLL) $(DLL).mdb
