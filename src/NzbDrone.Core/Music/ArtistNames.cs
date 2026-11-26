using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Music
{
    public static class ArtistNames
    {
        public static string ForLibraryArtist(Artist artist)
        {
            if (artist == null)
            {
                return null;
            }

            return artist.PreferredName.IsNotNullOrWhiteSpace()
                ? artist.PreferredName
                : artist.Name;
        }

        public static string ForMetadata(ArtistMetadata metadata, Artist libraryArtist)
        {
            if (libraryArtist != null && libraryArtist.PreferredName.IsNotNullOrWhiteSpace())
            {
                return libraryArtist.PreferredName;
            }

            return metadata?.Name;
        }

        public static string NormalizePreferredName(Artist artist, string preferredName)
        {
            if (preferredName.IsNullOrWhiteSpace())
            {
                return null;
            }

            preferredName = preferredName.Trim();

            if (artist?.Name != null && string.Equals(preferredName, artist.Name, StringComparison.Ordinal))
            {
                return null;
            }

            return preferredName;
        }

        public static bool IsAllowed(Artist artist, string preferredName)
        {
            if (preferredName.IsNullOrWhiteSpace())
            {
                return true;
            }

            preferredName = preferredName.Trim();

            if (artist == null)
            {
                return false;
            }

            if (artist.PreferredName.IsNotNullOrWhiteSpace() &&
                string.Equals(preferredName, artist.PreferredName, StringComparison.Ordinal))
            {
                return true;
            }

            return Choices(artist).Any(choice => string.Equals(choice, preferredName, StringComparison.Ordinal));
        }

        public static List<string> Choices(Artist artist)
        {
            var choices = new List<string>();
            var metadata = artist?.Metadata?.Value;

            if (metadata == null)
            {
                return choices;
            }

            AddDistinct(choices, metadata.Name);

            if (metadata.Aliases != null)
            {
                foreach (var alias in metadata.Aliases)
                {
                    AddDistinct(choices, alias);
                }
            }

            AddDistinct(choices, metadata.SortName);

            return choices;
        }

        private static void AddDistinct(List<string> choices, string name)
        {
            if (name.IsNullOrWhiteSpace())
            {
                return;
            }

            if (choices.Any(choice => string.Equals(choice, name, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            choices.Add(name);
        }
    }
}
