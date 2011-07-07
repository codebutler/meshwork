BINDIR = $(top_builddir)/build

REFERENCES = $(MESHWORK_LIBS) -r:System.ServiceProcess -r:Mono.Posix

ASSEMBLY = $(BINDIR)/meshwork-daemon.exe
FILES = MeshworkDaemon.cs Settings.cs AvatarManager.cs SettingsCreator.cs

all: $(ASSEMBLY)

$(ASSEMBLY): $(FILES)
	mkdir -p $(BINDIR)
	$(MCS) $(MCS_FLAGS) -out:$@ $(REFERENCES) $(FILES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES)

meshwork_daemondir = $(MESHWORK_DAEMON_DIR)
meshwork_daemon_DATA  = $(ASSEMBLY) $(ASSEMBLY).mdb
