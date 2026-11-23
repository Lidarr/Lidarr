namespace NzbDrone.Core.IndexerSearch
{
    public interface ISearchFormatConfigService
    {
        SearchFormatConfig GetConfig();
        void Save(SearchFormatConfig config);
    }

    public class SearchFormatConfigService : ISearchFormatConfigService
    {
        private readonly ISearchFormatConfigRepository _repository;

        public SearchFormatConfigService(ISearchFormatConfigRepository repository)
        {
            _repository = repository;
        }

        public SearchFormatConfig GetConfig()
        {
            var config = _repository.SingleOrDefault();

            if (config == null)
            {
                config = SearchFormatConfig.Default;
                config.Id = 1;
            }

            return config;
        }

        public void Save(SearchFormatConfig config)
        {
            var existing = _repository.SingleOrDefault();

            if (existing == null)
            {
                config.Id = 0;
                _repository.Insert(config);
            }
            else
            {
                config.Id = existing.Id;
                _repository.Update(config);
            }
        }
    }
}
