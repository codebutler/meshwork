BINDIR       = $(top_builddir)/build/
ASSEMBLY_DLL = FileFind.Meshwork.GtkClient.exe
ASSEMBLY     = $(BINDIR)/$(ASSEMBLY_DLL)

GENERATED_FILES_IN=Defines.cs.in
FILES=$(GENERATED_FILES_IN:.in=)

FILES +=                                       \
	AvatarManager.cs                       \
	GtkHelper.cs                           \
	DrawingUtilities.cs                    \
	AssemblyInfo.cs                        \
	DndUtils.cs                            \
	Gui.cs                                 \
	Menus/ConnectionMenu.cs                \
	Menus/TransfersMenu.cs                 \
	Menus/MemoMenu.cs                      \
	Menus/MapMenu.cs                       \
	Menus/UserMenu.cs                      \
	Menus/ChatMenu.cs                      \
	Widgets/GladeWidgetExtract.cs          \
	Widgets/SearchEntry.cs                 \
	Widgets/MainSidebar.cs                 \
	Widgets/HoverImageButton.cs            \
	Widgets/FilterWidget.cs                \
	Widgets/FolderDialog.cs                \
	Widgets/ExpanderBar.cs                 \
	Widgets/FileSelector.cs                \
	Widgets/NavigationBar.cs               \
	Widgets/Header.cs                      \
	Widgets/ZoomableCairoArea.cs           \
	Widgets/ZoomableNetworkMap.cs          \
	Widgets/NetworkGroupedTreeStore.cs     \
	Widgets/FadingAlignment.cs             \
	Widgets/FileSearchEntry.cs             \
	Runtime.cs                             \
	BuiltinActionGroup.cs                  \
	GtkMeshworkOptions.cs                  \
	RunOnMainThread.cs                     \
	Settings.cs                            \
	DispatchService.cs                     \
	SyncContext.cs                         \
	GuiSyncContext.cs                      \
	TrayIcon.cs                            \
	Windows/StartupProblemsDialog.cs       \
	Windows/GladeWindow.cs                 \
	Windows/FileTransferWindow.cs          \
	Windows/InviteToChatDialog.cs          \
	Windows/MainWindow.cs                  \
	Windows/ChatRoomInvitationDialog.cs    \
	Windows/winJoinChat.cs                 \
	Windows/winEditTrustedNode.cs          \
	Windows/PreferencesDialog.cs           \
	Windows/EditNetworkDialog.cs           \
	Windows/winBadOptions.cs               \
	Windows/winAddTrustedNode.cs           \
	Windows/EditMemoDialog.cs              \
	Windows/winAcceptKey.cs                \
	Windows/winDownloadPublicKey.cs        \
	Windows/AboutDialog.cs                 \
	Windows/winViewMemo.cs                 \
	Windows/winSplash.cs                   \
	Windows/UserInfoDialog.cs              \
	Windows/GenerateKeyDialog.cs           \
	Windows/ConnectDialog.cs               \
	Windows/AddNetworkDialog.cs            \
	Windows/GladeDialog.cs                 \
	Windows/ChatRoomPasswordDialog.cs      \
	Windows/FilePropertiesWindow.cs        \
	Pages/IPage.cs                         \
	Pages/NetworkOverviewPage.cs           \
	Pages/NetworkOverviewPage.UserList.cs  \
	Pages/SearchResultsPage.cs             \
	Pages/UserBrowserPage.cs               \
	Pages/ChatsPage.cs                     \
	Pages/ChatSubpageBase.cs               \
	Pages/PrivateChatSubpage.cs            \
	Pages/ChatRoomSubpage.cs               \
	Pages/MemosPage.cs                     \
	Pages/ConnectionsPage.cs               \
	Pages/StatusLogPage.cs                 \
	Pages/TransfersPage.cs                 \
	Pages/NewSearchPage.cs                 \
	SidebarItems/SeparatorItem.cs          \
	SidebarItems/WhatsPopularSearchItem.cs \
	SidebarItems/UserBrowserItem.cs        \
	SidebarItems/WhatsNewSearchItem.cs     \
	SidebarItems/NetworkOverviewItem.cs    \
	SidebarItems/FileSearchItem.cs         \
	SidebarItems/TransfersItem.cs          \
	SidebarItems/ChatsItem.cs              \
	SidebarItems/MemosItem.cs              \
	SidebarItems/ConnectionsItem.cs        \
	SidebarItems/NewSearchItem.cs          \
	SidebarItems/StatusItem.cs             \
	SidebarItems/ISidebarItem.cs

BUILD_FILES = $(addprefix $(srcdir)/, $(FILES))

GLADE = $(top_srcdir)/resources/glade/meshwork.glade
BUILD_GLADE= $(foreach res,$(GLADE), $(addprefix -resource:,$(res)),FileFind.Meshwork.GtkClient.$(notdir $(res)))

UI_RESOURCES = \
	$(top_srcdir)/resources/ui/TrayPopupMenu.xml 	\
	$(top_srcdir)/resources/ui/MainWindow.xml	\
	$(top_srcdir)/resources/ui/SearchPopupMenu.xml


BUILD_UI_RESOURCES = $(foreach res,$(UI_RESOURCES), $(addprefix -resource:,$(res)),FileFind.Meshwork.GtkClient.$(notdir $(res)))

IMAGES = \
	attachment-col_16.png       		\
	firewall-small.png         		\
	meshwork.png                		\
	network1.png                		\
	tray_icon.png               		\
	avatar_generic.png          		\
	zoom.png                    		\
	star1_16.png				\
	arrow_down.png              		\
	arrow_up.png                		\
	stock_icons/info_24.png                 \
	stock_icons/add_16.png                  \
	stock_icons/add_24.png                  \
	stock_icons/remove_16.png               \
	stock_icons/remove_24.png               \
	stock_icons/audio-x-generic_16.png      \
	stock_icons/image-x-generic_16.png      \
	stock_icons/video-x-generic_16.png      \
	stock_icons/text-x-generic_16.png       \
	stock_icons/x-office-document_16.png    \
	stock_icons/application-x-executable_16.png \
	stock_icons/unknown_16.png    		\
	stock_icons/folder_16.png    		\
	stock_icons/connect_established_16.png  \
	stock_icons/system-search_16.png        \
	stock_icons/computer_24.png             \
	stock_icons/connect_24.png              \
	stock_icons/stock_internet_16.png       \
	stock_icons/stock_internet_24.png       \
	stock_icons/stock_person_24.png         \
	stock_icons/stock_person_16.png         \
	stock_icons/stock-close_12.png          \
	stock_icons/locked_16.png               \
	stock_icons/down_22.png                 \
	stock_icons/down_16.png                 \
	stock_icons/left_22.png                 \
	stock_icons/left_16.png                 \
	stock_icons/right_22.png                \
	stock_icons/right_16.png                \
	stock_icons/up_16.png                   \
	stock_icons/up_22.png	            	\
	stock_icons/stock_new-text_24.png       \
	stock_icons/internet-group-chat_16.png  \
	stock_icons/internet-group-chat_24.png  \
	stock_icons/mail_16.png                 \
	stock_icons/find_24.png                 \
	stock_icons/stock_refresh_16.png        \
	stock_icons/stock_refresh_24.png


RESOURCES = $(addprefix $(top_srcdir)/resources/images/, $(IMAGES))
BUILD_RESOURCES = $(foreach res,$(RESOURCES), $(addprefix -resource:,$(res)),FileFind.Meshwork.GtkClient.$(notdir $(res)))

REFERENCES = \
	-r:$(top_builddir)/build/FileFind.Meshwork.dll \
	-r:$(top_builddir)/build/gtkspell-sharp.dll \
	-r:$(top_builddir)/build/FileFind.Stun.dll \
	-r:$(top_builddir)/build/ige-mac-integration-sharp.dll \
	-r:System.Drawing \
	-r:Mono.GetOptions \
	-r:Mono.Posix \
	-r:Mono.Cairo \
	$(GTK_SHARP_LIBS) \
	$(GTKDOTNET_LIBS) \
	$(GLADE_SHARP_LIBS)

all: $(ASSEMBLY)

$(ASSEMBLY): $(FILES) $(GLADE) $(UI_RESOURCES) $(RESOURCES)
	mkdir -p $(BINDIR)
	$(MCS) $(CSC_FLAGS) -unsafe -nowarn:0169 -debug -out:$@ -target:exe $(REFERENCES) $(BUILD_FILES) \
	$(BUILD_GLADE) $(BUILD_RESOURCES) $(BUILD_UI_RESOURCES)
	cp $(ASSEMBLY_DLL).config $(BINDIR)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(GLADE) $(RESOURCES) $(GENERATED_FILES_IN)
meshworkdir = $(MESHWORK_DIR)
meshwork_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY_DLL).config
