using System.IO;
using System.Xml;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.LocalMetadata.Savers
{
    /// <summary>
    /// Playlist xml saver.
    /// </summary>
    public class PlaylistXmlSaver : BaseXmlSaver
    {
        /// <summary>
        /// The default file name to use when creating a new playlist.
        /// </summary>
        public const string DefaultPlaylistFilename = "playlist.xml";

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaylistXmlSaver"/> class.
        /// </summary>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="userDataManager">Instance of the <see cref="IUserDataManager"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{PlaylistXmlSaver}"/> interface.</param>
        public PlaylistXmlSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager, ILogger<PlaylistXmlSaver> logger)
            : base(fileSystem, configurationManager, libraryManager, userManager, userDataManager, logger)
        {
        }

        /// <inheritdoc />
        public override bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
        {
            if (!item.SupportsLocalMetadata)
            {
                return false;
            }

            return item is Playlist && updateType >= ItemUpdateType.MetadataImport;
        }

        /// <inheritdoc />
        protected override void WriteCustomElements(BaseItem item, XmlWriter writer)
        {
            var game = (Playlist)item;

            if (!string.IsNullOrEmpty(game.PlaylistMediaType))
            {
                writer.WriteElementString("PlaylistMediaType", game.PlaylistMediaType);
            }
        }

        /// <inheritdoc />
        protected override string GetLocalSavePath(BaseItem item)
        {
            return GetSavePath(item.Path);
        }

        /// <summary>
        /// Get the save path.
        /// </summary>
        /// <param name="itemPath">The item path.</param>
        /// <returns>The save path.</returns>
        public static string GetSavePath(string itemPath)
        {
            var path = itemPath;

            if (Playlist.IsPlaylistFile(path))
            {
                return Path.ChangeExtension(itemPath, ".xml");
            }

            return Path.Combine(path, DefaultPlaylistFilename);
        }
    }
}
