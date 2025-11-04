using System;
using System.Collections.Generic;
using System.Linq;

namespace NzbDrone.Core.Plugins
{
    public class PluginVersion : IComparable<PluginVersion>, IEquatable<PluginVersion>
    {
        private static readonly IReadOnlyCollection<string> DefaultTreeNames = new[] { "master", "main" };

        public Version BaseVersion { get; }
        public string Suffix { get; }
        public bool HasSuffix => Suffix != null;

        public PluginVersion(Version version, string suffix = null)
        {
            BaseVersion = version ?? throw new ArgumentNullException(nameof(version));
            Suffix = CleanSuffix(suffix);
        }

        public static bool IsDefaultTree(string treeName) =>
            DefaultTreeNames.Contains(treeName, StringComparer.OrdinalIgnoreCase);

        public static PluginVersion Parse(string versionString)
        {
            if (string.IsNullOrWhiteSpace(versionString))
            {
                return null;
            }

            var parts = versionString.TrimStart('v').Split('-', 2);
            return Version.TryParse(parts[0], out var version) ? new PluginVersion(version, parts.Length > 1 ? parts[1] : null) : null;
        }

        public override string ToString() =>
            HasSuffix ? $"{BaseVersion}-{Suffix}" : BaseVersion.ToString();

        public string ToFormattedString()
        {
            var build = BaseVersion.Build == -1 ? 0 : BaseVersion.Build;
            var revision = BaseVersion.Revision == -1 ? 0 : BaseVersion.Revision;
            var baseVersionString = $"{BaseVersion.Major}.{BaseVersion.Minor}.{build}.{revision}";

            return HasSuffix && !IsDefaultTree(Suffix)
                ? $"{baseVersionString} ({Suffix})"
                : baseVersionString;
        }

        public int CompareTo(PluginVersion other)
        {
            if (other is null)
            {
                return 1;
            }

            var baseComp = BaseVersion.CompareTo(other.BaseVersion);
            if (baseComp != 0)
            {
                return baseComp;
            }

            return (Suffix, other.Suffix) switch
            {
                (null, not null) => 1,
                (not null, null) => -1,
                _ => string.Compare(Suffix, other.Suffix, StringComparison.OrdinalIgnoreCase)
            };
        }

        public bool Equals(PluginVersion other) =>
            other is not null &&
            (ReferenceEquals(this, other) ||
             (BaseVersion.Equals(other.BaseVersion) &&
              string.Equals(Suffix, other.Suffix, StringComparison.OrdinalIgnoreCase)));

        public override bool Equals(object obj) =>
            obj is PluginVersion other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(BaseVersion, Suffix?.ToUpperInvariant());

        public static bool operator ==(PluginVersion left, PluginVersion right) =>
            left is null ? right is null : left.Equals(right);

        public static bool operator !=(PluginVersion left, PluginVersion right) =>
            !(left == right);

        public static bool operator <(PluginVersion left, PluginVersion right) =>
            left is null ? right is not null : left.CompareTo(right) < 0;

        public static bool operator <=(PluginVersion left, PluginVersion right) =>
            left is null || left.CompareTo(right) <= 0;

        public static bool operator >(PluginVersion left, PluginVersion right) =>
            left is not null && left.CompareTo(right) > 0;

        public static bool operator >=(PluginVersion left, PluginVersion right) =>
            left is null ? right is null : left.CompareTo(right) >= 0;

        public static implicit operator PluginVersion(Version version) =>
            version is null ? null : new PluginVersion(version);

        public static explicit operator Version(PluginVersion pluginVersion)
            => pluginVersion?.BaseVersion;

        private static string CleanSuffix(string suffix)
        {
            if (string.IsNullOrWhiteSpace(suffix))
            {
                return null;
            }

            var plusIndex = suffix.IndexOf('+', StringComparison.OrdinalIgnoreCase);
            if (plusIndex >= 0)
            {
                suffix = suffix[..plusIndex];
            }

            return string.IsNullOrWhiteSpace(suffix) ? null : suffix.Trim();
        }
    }
}
