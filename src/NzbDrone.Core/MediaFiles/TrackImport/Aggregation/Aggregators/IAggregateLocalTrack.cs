namespace NzbDrone.Core.MediaFiles.TrackImport.Aggregation.Aggregators
{
    public interface IAggregate<T>
    {
        int Order { get; }
        T Aggregate(T item, bool otherFiles);
    }
}
