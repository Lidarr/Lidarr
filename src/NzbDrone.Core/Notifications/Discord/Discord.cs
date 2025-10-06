using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;
using NzbDrone.Core.Notifications.Discord.Payloads;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Notifications.Discord
{
    public class Discord : NotificationBase<DiscordSettings>
    {
        private readonly IDiscordProxy _proxy;
        private readonly IConfigFileProvider _configFileProvider;

        public Discord(IDiscordProxy proxy, IConfigFileProvider configFileProvider)
        {
            _proxy = proxy;
            _configFileProvider = configFileProvider;
        }

        public override string Name => "Discord";
        public override string Link => "https://support.discordapp.com/hc/en-us/articles/228383668-Intro-to-Webhooks";

        public override void OnGrab(GrabMessage message)
        {
            var artist = message.Artist;
            var albums = message.RemoteAlbum.Albums;
            var artistMetadata = message.Artist.Metadata.Value;

            var embed = new Embed
            {
                Author = new DiscordAuthor
                {
                    Name = Settings.Author.IsNullOrWhiteSpace() ? _configFileProvider.InstanceName : Settings.Author,
                    IconUrl = "https://raw.githubusercontent.com/lidarr/Lidarr/develop/Logo/256.png"
                },
                Url = $"https://musicbrainz.org/artist/{artist.ForeignArtistId}",
                Description = "Album Grabbed",
                Title = GetTitle(artist, albums),
                Color = (int)DiscordColors.Standard,
                Fields = new List<DiscordField>(),
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };

            if (Settings.GrabFields.Contains((int)DiscordGrabFieldType.Poster))
            {
                embed.Thumbnail = new DiscordImage
                {
                    Url = albums.First().Images.FirstOrDefault(x => x.CoverType == MediaCoverTypes.Cover)?.Url
                };
            }

            if (Settings.GrabFields.Contains((int)DiscordGrabFieldType.Fanart))
            {
                embed.Image = new DiscordImage
                {
                    Url = artistMetadata.Images.FirstOrDefault(x => x.CoverType == MediaCoverTypes.Fanart)?.Url
                };
            }

            foreach (var field in Settings.GrabFields)
            {
                var discordField = new DiscordField();

                switch ((DiscordGrabFieldType)field)
                {
                    case DiscordGrabFieldType.Overview:
                        var overview = albums.First().Overview ?? "";
                        discordField.Name = "Overview";
                        discordField.Value = overview.Length <= 300 ? overview : $"{overview.AsSpan(0, 300)}...";
                        break;
                    case DiscordGrabFieldType.Rating:
                        discordField.Name = "Rating";
                        discordField.Value = albums.First().Ratings.Value.ToString();
                        break;
                    case DiscordGrabFieldType.Genres:
                        discordField.Name = "Genres";
                        discordField.Value = albums.First().Genres.Take(5).Join(", ");
                        break;
                    case DiscordGrabFieldType.Quality:
                        discordField.Name = "Quality";
                        discordField.Inline = true;
                        discordField.Value = message.Quality.Quality.Name;
                        break;
                    case DiscordGrabFieldType.Group:
                        discordField.Name = "Group";
                        discordField.Value = message.RemoteAlbum.ParsedAlbumInfo.ReleaseGroup;
                        break;
                    case DiscordGrabFieldType.Size:
                        discordField.Name = "Size";
                        discordField.Value = BytesToString(message.RemoteAlbum.Release.Size);
                        discordField.Inline = true;
                        break;
                    case DiscordGrabFieldType.Release:
                        discordField.Name = "Release";
                        discordField.Value = string.Format("```{0}```", message.RemoteAlbum.Release.Title);
                        break;
                    case DiscordGrabFieldType.Links:
                        discordField.Name = "Links";
                        discordField.Value = GetLinksString(artist);
                        break;
                    case DiscordGrabFieldType.CustomFormats:
                        discordField.Name = "Custom Formats";
                        discordField.Value = string.Join("|", message.RemoteAlbum.CustomFormats);
                        break;
                    case DiscordGrabFieldType.CustomFormatScore:
                        discordField.Name = "Custom Format Score";
                        discordField.Value = message.RemoteAlbum.CustomFormatScore.ToString();
                        break;
                    case DiscordGrabFieldType.Indexer:
                        discordField.Name = "Indexer";
                        discordField.Value = message.RemoteAlbum.Release.Indexer;
                        break;
                }

                if (discordField.Name.IsNotNullOrWhiteSpace() && discordField.Value.IsNotNullOrWhiteSpace())
                {
                    embed.Fields.Add(discordField);
                }
            }

            var payload = CreatePayload(null, new List<Embed> { embed });

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnReleaseImport(AlbumDownloadMessage message)
        {
            var artist = message.Artist;
            var artistMetadata = message.Artist.Metadata.Value;
            var album = message.Album;
            var episodes = message.TrackFiles;
            var isUpgrade = message.OldFiles.Count > 0;

            var embed = new Embed
            {
                Author = new DiscordAuthor
                {
                    Name = Settings.Author.IsNullOrWhiteSpace() ? _configFileProvider.InstanceName : Settings.Author,
                    IconUrl = "https://raw.githubusercontent.com/lidarr/Lidarr/develop/Logo/256.png"
                },
                Url = $"https://musicbrainz.org/artist/{artist.ForeignArtistId}",
                Description = isUpgrade ? "Album Upgraded" : "Album Imported",
                Title = GetTitle(artist, new List<Album> { album }),
                Color = isUpgrade ? (int)DiscordColors.Upgrade : (int)DiscordColors.Success,
                Fields = new List<DiscordField>(),
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };

            if (Settings.ImportFields.Contains((int)DiscordImportFieldType.Poster))
            {
                embed.Thumbnail = new DiscordImage
                {
                    Url = album.Images.FirstOrDefault(x => x.CoverType == MediaCoverTypes.Cover)?.Url
                };
            }

            if (Settings.ImportFields.Contains((int)DiscordImportFieldType.Fanart))
            {
                embed.Image = new DiscordImage
                {
                    Url = artistMetadata.Images.FirstOrDefault(x => x.CoverType == MediaCoverTypes.Fanart)?.Url
                };
            }

            foreach (var field in Settings.ImportFields)
            {
                var discordField = new DiscordField();

                switch ((DiscordImportFieldType)field)
                {
                    case DiscordImportFieldType.Overview:
                        var overview = album.Overview ?? "";
                        discordField.Name = "Overview";
                        discordField.Value = overview.Length <= 300 ? overview : $"{overview.AsSpan(0, 300)}...";
                        break;
                    case DiscordImportFieldType.Rating:
                        discordField.Name = "Rating";
                        discordField.Value = album.Ratings.Value.ToString();
                        break;
                    case DiscordImportFieldType.Genres:
                        discordField.Name = "Genres";
                        discordField.Value = album.Genres.Take(5).Join(", ");
                        break;
                    case DiscordImportFieldType.Quality:
                        discordField.Name = "Quality";
                        discordField.Inline = true;
                        discordField.Value = message.TrackFiles.First().Quality.Quality.Name;
                        break;
                    case DiscordImportFieldType.Group:
                        discordField.Name = "Group";
                        discordField.Value = message.TrackFiles.First().ReleaseGroup;
                        break;
                    case DiscordImportFieldType.Size:
                        discordField.Name = "Size";
                        discordField.Value = BytesToString(message.TrackFiles.Sum(x => x.Size));
                        discordField.Inline = true;
                        break;
                    case DiscordImportFieldType.Release:
                        discordField.Name = "Release";
                        discordField.Value = message.TrackFiles.First().SceneName;
                        break;
                    case DiscordImportFieldType.Links:
                        discordField.Name = "Links";
                        discordField.Value = GetLinksString(artist);
                        break;
                    case DiscordImportFieldType.CustomFormats:
                        discordField.Name = "Custom Formats";
                        discordField.Value = string.Join("|", message.EpisodeInfo.CustomFormats);
                        break;
                    case DiscordImportFieldType.CustomFormatScore:
                        discordField.Name = "Custom Format Score";
                        discordField.Value = message.EpisodeInfo.CustomFormatScore.ToString();
                        break;
                }

                if (discordField.Name.IsNotNullOrWhiteSpace() && discordField.Value.IsNotNullOrWhiteSpace())
                {
                    embed.Fields.Add(discordField);
                }
            }

            var payload = CreatePayload(null, new List<Embed> { embed });

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnRename(Artist artist, List<RenamedTrackFile> renamedFiles)
        {
            var attachments = new List<Embed>
                              {
                                  new Embed
                                  {
                                      Title = artist.Name,
                                  }
                              };

            var payload = CreatePayload("Renamed", attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnArtistAdd(ArtistAddMessage message)
        {
            var artist = message.Artist;

            var attachments = new List<Embed>
            {
                new Embed
                {
                    Title = artist.Name,
                    Description = message.Message
                }
            };

            var payload = CreatePayload("Artist Added", attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnArtistDelete(ArtistDeleteMessage deleteMessage)
        {
            var artist = deleteMessage.Artist;

            var attachments = new List<Embed>
            {
                new Embed
                {
                    Title = artist.Name,
                    Description = deleteMessage.DeletedFilesMessage
                }
            };

            var payload = CreatePayload("Artist Deleted", attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnAlbumDelete(AlbumDeleteMessage deleteMessage)
        {
            var album = deleteMessage.Album;

            var attachments = new List<Embed>
            {
                new Embed
                {
                    Title = album.Title,
                    Description = deleteMessage.DeletedFilesMessage
                }
            };

            var payload = CreatePayload("Album Deleted", attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            var attachments = new List<Embed>
                              {
                                  new Embed
                                  {
                                      Author = new DiscordAuthor
                                      {
                                          Name = Settings.Author.IsNullOrWhiteSpace() ? _configFileProvider.InstanceName : Settings.Author,
                                          IconUrl = "https://raw.githubusercontent.com/lidarr/Lidarr/develop/Logo/256.png"
                                      },
                                      Title = healthCheck.Source.Name,
                                      Description = healthCheck.Message,
                                      Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                                      Color = healthCheck.Type == HealthCheck.HealthCheckResult.Warning ? (int)DiscordColors.Warning : (int)DiscordColors.Danger
                                  }
                              };

            var payload = CreatePayload(null, attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnHealthRestored(HealthCheck.HealthCheck previousCheck)
        {
            var attachments = new List<Embed>
            {
                new Embed
                {
                    Author = new DiscordAuthor
                    {
                        Name = Settings.Author.IsNullOrWhiteSpace() ? _configFileProvider.InstanceName : Settings.Author,
                        IconUrl = "https://raw.githubusercontent.com/Lidarr/Lidarr/develop/Logo/256.png"
                    },
                    Title = "Health Issue Resolved: " + previousCheck.Source.Name,
                    Description = $"The following issue is now resolved: {previousCheck.Message}",
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    Color = (int)DiscordColors.Success
                }
            };

            var payload = CreatePayload(null, attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnTrackRetag(TrackRetagMessage message)
        {
            var attachments = new List<Embed>
                              {
                                  new Embed
                                  {
                                      Author = new DiscordAuthor
                                      {
                                          Name = Settings.Author.IsNullOrWhiteSpace() ? _configFileProvider.InstanceName : Settings.Author,
                                          IconUrl = "https://raw.githubusercontent.com/lidarr/Lidarr/develop/Logo/256.png"
                                      },
                                      Title = TRACK_RETAGGED_TITLE,
                                      Text = message.Message
                                  }
                              };

            var payload = CreatePayload($"Track file tags updated: {message.Message}", attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnDownloadFailure(DownloadFailedMessage message)
        {
            var attachments = new List<Embed>
            {
                new Embed
                {
                    Author = new DiscordAuthor
                    {
                        Name = Settings.Author.IsNullOrWhiteSpace() ? _configFileProvider.InstanceName : Settings.Author,
                        IconUrl = "https://raw.githubusercontent.com/lidarr/Lidarr/develop/Logo/256.png"
                    },
                    Description = message.Message,
                    Title = message.SourceTitle,
                    Text = message.Message,
                    Color = (int)DiscordColors.Danger
                }
            };
            var payload = CreatePayload($"Download Failed: {message.Message}", attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnImportFailure(AlbumDownloadMessage message)
        {
            var attachments = new List<Embed>
            {
                new Embed
                {
                    Author = new DiscordAuthor
                    {
                        Name = Settings.Author.IsNullOrWhiteSpace() ? _configFileProvider.InstanceName : Settings.Author,
                        IconUrl = "https://raw.githubusercontent.com/lidarr/Lidarr/develop/Logo/256.png"
                    },
                    Description = message.Message,
                    Title = message.Album?.Title ?? message.Message,
                    Text = message.Message,
                    Color = (int)DiscordColors.Warning
                }
            };
            var payload = CreatePayload($"Import Failed: {message.Message}", attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
            var attachments = new List<Embed>
                              {
                                  new Embed
                                  {
                                      Author = new DiscordAuthor
                                      {
                                          Name = Settings.Author.IsNullOrWhiteSpace() ? _configFileProvider.InstanceName : Settings.Author,
                                          IconUrl = "https://raw.githubusercontent.com/lidarr/Lidarr/develop/Logo/256.png"
                                      },
                                      Title = APPLICATION_UPDATE_TITLE,
                                      Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                                      Color = (int)DiscordColors.Standard,
                                      Fields = new List<DiscordField>()
                                      {
                                          new DiscordField()
                                          {
                                              Name = "Previous Version",
                                              Value = updateMessage.PreviousVersion.ToString()
                                          },
                                          new DiscordField()
                                          {
                                              Name = "New Version",
                                              Value = updateMessage.NewVersion.ToString()
                                          }
                                      },
                                  }
                              };

            var payload = CreatePayload(null, attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(TestMessage());

            return new ValidationResult(failures);
        }

        public ValidationFailure TestMessage()
        {
            try
            {
                var message = $"Test message from Lidarr posted at {DateTime.Now}";
                var payload = CreatePayload(message);

                _proxy.SendPayload(payload, Settings);
            }
            catch (DiscordException ex)
            {
                return new NzbDroneValidationFailure("Unable to post", ex.Message);
            }

            return null;
        }

        private DiscordPayload CreatePayload(string message, List<Embed> embeds = null)
        {
            var avatar = Settings.Avatar;

            var payload = new DiscordPayload
            {
                Username = Settings.Username,
                Content = message,
                Embeds = embeds
            };

            if (avatar.IsNotNullOrWhiteSpace())
            {
                payload.AvatarUrl = avatar;
            }

            if (Settings.Username.IsNotNullOrWhiteSpace())
            {
                payload.Username = Settings.Username;
            }

            return payload;
        }

        private string BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; // Longs run out around EB
            if (byteCount == 0)
            {
                return "0 " + suf[0];
            }

            var bytes = Math.Abs(byteCount);
            var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            var num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return string.Format("{0} {1}", (Math.Sign(byteCount) * num).ToString(), suf[place]);
        }

        private string GetLinksString(Artist artist)
        {
            var links = new List<string>();

            links.Add($"[MusicBrainz](https://musicbrainz.org/artist/{artist.ForeignArtistId})");

            return string.Join(" / ", links);
        }

        private string GetTitle(Artist artist, List<Album> albums)
        {
            var albumTitles = string.Join(" + ", albums.Select(e => e.Title));

            var title = $"{artist.Name} - {albumTitles}".Replace("`", "\\`");

            return title.Length > 256 ? $"{title.AsSpan(0, 253).TrimEnd('\\')}..." : title;
        }
    }
}
