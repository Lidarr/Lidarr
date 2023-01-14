import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { saveInteractiveImportItem, updateInteractiveImportItems } from 'Store/Actions/interactiveImportActions';
import SelectReleaseGroupModalContent from './SelectReleaseGroupModalContent';

const mapDispatchToProps = {
  dispatchUpdateInteractiveImportItems: updateInteractiveImportItems,
  dispatchSaveInteractiveImportItems: saveInteractiveImportItem
};

class SelectReleaseGroupModalContentConnector extends Component {

  //
  // Listeners

  onReleaseGroupSelect = ({ releaseGroup }) => {
    const {
      ids,
      dispatchUpdateInteractiveImportItems,
      dispatchSaveInteractiveImportItems
    } = this.props;

    dispatchUpdateInteractiveImportItems({
      ids,
      releaseGroup
    });

    dispatchSaveInteractiveImportItems({ ids });

    this.props.onModalClose(true);
  };

  //
  // Render

  render() {
    return (
      <SelectReleaseGroupModalContent
        {...this.props}
        onReleaseGroupSelect={this.onReleaseGroupSelect}
      />
    );
  }
}

SelectReleaseGroupModalContentConnector.propTypes = {
  ids: PropTypes.arrayOf(PropTypes.number).isRequired,
  dispatchUpdateInteractiveImportItems: PropTypes.func.isRequired,
  dispatchSaveInteractiveImportItems: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default connect(null, mapDispatchToProps)(SelectReleaseGroupModalContentConnector);
