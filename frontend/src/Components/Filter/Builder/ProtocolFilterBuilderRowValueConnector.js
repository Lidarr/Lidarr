import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import FilterBuilderRowValue from './FilterBuilderRowValue';

function createMapStateToProps() {
  return createSelector(
    (state) => state.settings.downloadClients,
    (state) => state.settings.indexers,
    (downloadClients, indexers) => {
      const protocols = Array.from(new Set([
        ...downloadClients.items.map((i) => i.protocol),
        ...indexers.items.map((i) => i.protocol)
      ]));

      console.log(protocols);
      const tagList = protocols.map((protocol) => {
        return {
          id: protocol,
          name: protocol.replace('DownloadProtocol', '')
        };
      });

      return {
        tagList
      };
    }
  );
}

export default connect(createMapStateToProps)(FilterBuilderRowValue);
