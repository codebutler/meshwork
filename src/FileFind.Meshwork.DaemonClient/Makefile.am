BINDIR = $(top_builddir)/build

REFERENCES = \
	-r:System.ServiceProcess \
	-r:Mono.Posix		\
	-r:$(top_builddir)/build/FileFind.Meshwork.dll

ASSEMBLY = $(BINDIR)/FileFind.Meshwork.DaemonClient.exe
FILES = MeshworkDaemon.cs Settings.cs AvatarManager.cs SettingsCreator.cs

all: $(ASSEMBLY)

$(ASSEMBLY): $(FILES)
	mkdir -p $(BINDIR)
	$(MCS) $(CSC_FLAGS) -debug -out:$@ $(REFERENCES) $(FILES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES)

meshwork_daemondir = $(MESHWORK_DAEMON_DIR)
meshwork_daemon_DATA  = $(ASSEMBLY) $(ASSEMBLY).mdb
