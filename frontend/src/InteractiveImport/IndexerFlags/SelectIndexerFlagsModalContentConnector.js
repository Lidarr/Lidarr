import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { saveInteractiveImportItem, updateInteractiveImportItems } from 'Store/Actions/interactiveImportActions';
import SelectIndexerFlagsModalContent from './SelectIndexerFlagsModalContent';

const mapDispatchToProps = {
  dispatchUpdateInteractiveImportItems: updateInteractiveImportItems,
  dispatchSaveInteractiveImportItems: saveInteractiveImportItem
};

class SelectIndexerFlagsModalContentConnector extends Component {

  //
  // Listeners

  onIndexerFlagsSelect = ({ indexerFlags }) => {
    const {
      ids,
      dispatchUpdateInteractiveImportItems,
      dispatchSaveInteractiveImportItems
    } = this.props;

    dispatchUpdateInteractiveImportItems({
      ids,
      indexerFlags
    });

    dispatchSaveInteractiveImportItems({ ids });

    this.props.onModalClose(true);
  };

  //
  // Render

  render() {
    return (
      <SelectIndexerFlagsModalContent
        {...this.props}
        onIndexerFlagsSelect={this.onIndexerFlagsSelect}
      />
    );
  }
}

SelectIndexerFlagsModalContentConnector.propTypes = {
  ids: PropTypes.arrayOf(PropTypes.number).isRequired,
  dispatchUpdateInteractiveImportItems: PropTypes.func.isRequired,
  dispatchSaveInteractiveImportItems: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default connect(null, mapDispatchToProps)(SelectIndexerFlagsModalContentConnector);
