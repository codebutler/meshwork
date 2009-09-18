BINDIR       = $(top_builddir)/build/
ASSEMBLY_DLL = FileFind.Meshwork.dll
ASSEMBLY     = $(BINDIR)/$(ASSEMBLY_DLL)

FILES = \
	Core.cs								        \
	FileFind.Meshwork.Collections/NodeConnectionCollection.cs               \
	FileFind.Meshwork.Collections/MessageIdCollection.cs                    \
	FileFind/StringWriterWithEncoding.cs                                    \
	FileFind/Serialization.cs                                               \
	FileFind/IPv6Util.cs                                                    \
	FileFind/Common.cs                                                      \
	FileFind/Extensions.cs                                                  \
	FileFind/IPAddressExtensions.cs                                         \
	FileFind/ReadOnlyDictionary.cs                                          \
	FileFind/SerializableDictionary.cs                                      \
	FileFind.Stun/ChangeRequestAttribute.cs                                 \
	FileFind.Stun/SourceAddressAttribute.cs                                 \
	FileFind.Stun/MessageType.cs                                            \
	FileFind.Stun/Utility.cs                                                \
	FileFind.Stun/MessageAttribute.cs                                       \
	FileFind.Stun/StunClient.cs                                             \
	FileFind.Stun/AddressAttributeBase.cs                                   \
	FileFind.Stun/MessageHeader.cs                                          \
	FileFind.Stun/MappedAddressAttribute.cs                                 \
	FileFind.Meshwork.Destination/IDestination.cs                           \
	FileFind.Meshwork.Destination/DestinationManager.cs                     \
	FileFind.Meshwork.Destination/IPDestination.cs                          \
	FileFind.Meshwork.Destination/TCPDestination.cs                         \
	FileFind.Meshwork.Protocol/MessageContentClasses.cs                     \
	FileFind.Meshwork.Protocol/MemoInfo.cs                                  \
	FileFind.Meshwork.Filesystem/FileSystemProvider.cs                      \
	FileFind.Meshwork.Filesystem/IDirectoryItem.cs			        \
	FileFind.Meshwork.Filesystem/AbstractFile.cs				\
	FileFind.Meshwork.Filesystem/NetworkDirectory.cs			\
	FileFind.Meshwork.Filesystem/IFile.cs					\
	FileFind.Meshwork.Filesystem/ILocalDirectoryItem.cs			\
	FileFind.Meshwork.Filesystem/RemoteFile.cs				\
	FileFind.Meshwork.Filesystem/RemoteDirectory.cs				\
	FileFind.Meshwork.Filesystem/IRemoteDirectoryItem.cs			\
	FileFind.Meshwork.Filesystem/NodeDirectory.cs				\
	FileFind.Meshwork.Filesystem/AbstractDirectory.cs			\
	FileFind.Meshwork.Filesystem/LocalDirectory.cs				\
	FileFind.Meshwork.Filesystem/MyDirectory.cs				\
	FileFind.Meshwork.Filesystem/LocalFile.cs				\
	FileFind.Meshwork.Filesystem/IDirectory.cs				\
	FileFind.Meshwork.Filesystem/PathUtil.cs				\
	FileFind.Meshwork.Filesystem/RootDirectory.cs				\
	FileFind.Meshwork.FileTransfer/FileTransferManager.cs   	        \
	FileFind.Meshwork.FileTransfer/FileTransferDirection.cs   	        \
	FileFind.Meshwork.FileTransfer/FileTransferBase.cs   		        \
	FileFind.Meshwork.FileTransfer/IFileTransferPeer.cs                     \
	FileFind.Meshwork.FileTransfer/FileTransferPeerBase.cs                  \
	FileFind.Meshwork.FileTransfer/FileTransferPeerStatus.cs                \
	FileFind.Meshwork.FileTransfer/FileTransferStatus.cs                    \
	FileFind.Meshwork.FileTransfer/IFileTransfer.cs                         \
	FileFind.Meshwork.FileTransfer/IFileTransferProvider.cs                 \
	FileFind.Meshwork.FileTransfer.BitTorrent/BitTorrentFileTransfer.cs     \
	FileFind.Meshwork.FileTransfer.BitTorrent/BitTorrentFileTransferPeer.cs \
	FileFind.Meshwork.FileTransfer.BitTorrent/BitTorrentFileTransferProvider.cs \
	FileFind.Meshwork.FileTransfer.BitTorrent/MeshworkTrackerClient.cs      \
	FileFind.Meshwork.FileTransfer.BitTorrent/MeshworkPeerConnection.cs     \
	FileFind.Meshwork.FileTransfer.BitTorrent/MeshworkPeerConnectionListener.cs \
	FileFind.Meshwork.Transport/TransportBase.cs                           \
	FileFind.Meshwork.Transport/ITransportEncryptor.cs                     \
	FileFind.Meshwork.Transport/TcpListener.cs                             \
	FileFind.Meshwork.Transport/ITransport.cs                              \
	FileFind.Meshwork.Transport/ITransportListener.cs                      \
	FileFind.Meshwork.Transport/AESTransportEncryptor.cs                   \
	FileFind.Meshwork.Transport/TcpTransport.cs                            \
	FileFind.Meshwork.Transport/TransportManager.cs                        \
	FileFind.Meshwork.Transport/TransportState.cs                          \
	FileFind.Meshwork.Transport/IMeshworkOperation.cs                      \
	FileFind.Meshwork.Search/FileSearchManager.cs                          \
	FileFind.Meshwork.Search/FileSearch.cs                                 \
	FileFind.Meshwork.Search/FileSearchFilter.cs                           \
	FileFind.Meshwork.Search/FileSearchGroup.cs                            \
	FileFind.Meshwork.Search/SearchResult.cs                               \
	FileFind.Meshwork.Search/ISearchResult.cs                              \
	FileFind.Meshwork.Logging/ConsoleCrayon.cs                             \
	FileFind.Meshwork.Logging/LogLevel.cs                                  \
	FileFind.Meshwork.Logging/FileLogger.cs                                \
	FileFind.Meshwork.Logging/ILogger.cs                                   \
	FileFind.Meshwork.Logging/ConsoleLogger.cs                             \
	LoggingService.cs                                                      \
	AutoconnectManager.cs                                                  \
	AckMethod.cs                                                           \
	AssemblyInfo.cs                                                        \
	ConnectionState.cs                                                     \
	ChatRoom.cs                                                            \
	DataConverter.cs                                                       \
	Memo.cs                                                                \
	ConnectionType.cs                                                      \
	DebugInfo.cs                                                           \
	Exceptions.cs                                                          \
	EndianBitConverter.cs						       \
	PublicKey.cs                                                           \
	Node.cs                                                                \
	KeyFunctions.cs                                                        \
	Object.cs                                                              \
	IMeshworkPlugin.cs                                                     \
	PluginInfo.cs                                                          \
	NetworkState.cs                                                        \
	INodeConnection.cs                                                     \
	SettingsBase.cs							       \
	ISettings.cs							       \
	LocalNodeConnection.cs                                                 \
	MessageBuilder.cs                                                      \
	Message.cs                                                             \
	MessageProcessor.cs                                                    \
	Network.cs                                                             \
	NearbyNode.cs                                                          \
	RemoteNodeConnection.cs                                                \
	Validator.cs                                                           \
	Encryption.cs							       \
	ShareBuilder.cs                                                        \
	ShareHasher.cs                                                         \
	ShareWatcher.cs                                                        \
	TrustedNodeInfo.cs                                                     \
	NetworkInfo.cs                                                         \
	EventArgs.cs							       \
	IAvatarManager.cs                                                      \
	IPlatform.cs                                                           \
	Linux.cs                                                               \
	OSX.cs								       \
	Windows.cs                             

BUILD_FILES = $(addprefix $(srcdir)/, $(FILES))

REFERENCES = \
	-r:$(top_builddir)/build/DiffieHellman.dll \
	-r:$(top_builddir)/build/MonoTorrent.dll \
	-r:System.Web.Services \
	-r:System.Web \
	-r:System.Data \
	-r:Mono.Data \
	-r:Mono.Data.SqliteClient.dll \
	-r:Mono.Posix

all: $(ASSEMBLY)

$(ASSEMBLY): $(FILES)
	mkdir -p $(top_builddir)/
	$(MCS) -debug -out:$@ -target:library -unsafe -define:MONO_DATACONVERTER_PUBLIC $(REFERENCES) $(BUILD_FILES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES)

meshworkdir = $(MESHWORK_DIR)
meshwork_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb
