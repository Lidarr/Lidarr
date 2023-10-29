namespace NzbDrone.Core.Indexers
{
    public interface ITorrentIndexerSettings : IIndexerSettings
    {
        int MinimumSeeders { get; set; }

        SeedCriteriaSettings SeedCriteria { get; set; }
        bool RejectBlocklistedTorrentHashesWhileGrabbing { get; set; }
    }
}
