import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import { fetchTrackFiles } from 'Store/Actions/trackFileActions';
import getErrorMessage from 'Utilities/Object/getErrorMessage';
import FileDetails from './FileDetails';

function createMapStateToProps() {
  return createSelector(
    (state) => state.trackFiles,
    (trackFiles) => {
      return {
        ...trackFiles
      };
    }
  );
}

const mapDispatchToProps = {
  fetchTrackFiles
};

class FileDetailsConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    this.props.fetchTrackFiles({ id: this.props.id });
  }

  //
  // Render

  render() {
    const {
      items,
      id,
      isFetching,
      error
    } = this.props;

    const item = _.find(items, { id });
    const errorMessage = getErrorMessage(error, 'Unable to load manual import items');

    if (isFetching || !item.audioTags) {
      return (
        <LoadingIndicator />
      );
    } else if (error) {
      return (
        <div>{errorMessage}</div>
      );
    }

    return (
      <FileDetails
        audioTags={item.audioTags}
        filename={item.path}
      />
    );

  }
}

FileDetailsConnector.propTypes = {
  fetchTrackFiles: PropTypes.func.isRequired,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  id: PropTypes.number.isRequired,
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object
};

export default connect(createMapStateToProps, mapDispatchToProps)(FileDetailsConnector);
