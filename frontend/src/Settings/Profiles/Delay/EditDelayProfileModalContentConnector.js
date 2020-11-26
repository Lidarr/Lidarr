import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { saveDelayProfile, setDelayProfileValue } from 'Store/Actions/settingsActions';
import selectSettings from 'Store/Selectors/selectSettings';
import EditDelayProfileModalContent from './EditDelayProfileModalContent';

const newDelayProfile = {
  items: [
    {
      name: 'Usenet',
      protocol: 'usenet',
      allowed: true,
      delay: 0
    },
    {
      name: 'Torrent',
      protocol: 'torrent',
      allowed: true,
      delay: 0
    },
    {
      name: 'Deemix',
      protocol: 'deemix',
      allowed: true,
      delay: 0
    }
  ],
  tags: []
};

function createDelayProfileSelector() {
  return createSelector(
    (state, { id }) => id,
    (state) => state.settings.delayProfiles,
    (id, delayProfiles) => {
      const {
        isFetching,
        error,
        isSaving,
        saveError,
        pendingChanges,
        items
      } = delayProfiles;

      const profile = id ? _.find(items, { id }) : newDelayProfile;
      const settings = selectSettings(profile, pendingChanges, saveError);

      return {
        id,
        isFetching,
        error,
        isSaving,
        saveError,
        item: settings.settings,
        ...settings
      };
    }
  );
}

function createMapStateToProps() {
  return createSelector(
    createDelayProfileSelector(),
    (delayProfile) => {
      return {
        ...delayProfile
      };
    }
  );
}

const mapDispatchToProps = {
  setDelayProfileValue,
  saveDelayProfile
};

class EditDelayProfileModalContentConnector extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      dragIndex: null,
      dropIndex: null,
      dropPosition: null
    };
  }

  componentDidMount() {
    if (!this.props.id) {
      Object.keys(newDelayProfile).forEach((name) => {
        this.props.setDelayProfileValue({
          name,
          value: newDelayProfile[name]
        });
      });
    }
  }

  componentDidUpdate(prevProps, prevState) {
    if (prevProps.isSaving && !this.props.isSaving && !this.props.saveError) {
      this.props.onModalClose();
    }
  }

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.props.setDelayProfileValue({ name, value });
  }

  onSavePress = () => {
    this.props.saveDelayProfile({ id: this.props.id });
  }

  onDownloadProtocolItemFieldChange = (protocol, name, value) => {
    console.log('on allowed');
    const delayProfile = _.cloneDeep(this.props.item);
    const items = delayProfile.items.value;
    const item = _.find(delayProfile.items.value, (i) => i.protocol === protocol);

    item[name] = value;

    this.props.setDelayProfileValue({
      name: 'items',
      value: items
    });
  }

  onDownloadProtocolItemDragMove = ({ dragIndex, dropIndex, dropPosition }) => {
    if (
      (dropPosition === 'below' && dropIndex + 1 === dragIndex) ||
      (dropPosition === 'above' && dropIndex - 1 === dragIndex)
    ) {
      if (
        this.state.dragIndex != null &&
        this.state.dropIndex != null &&
        this.state.dropPosition != null
      ) {
        this.setState({
          dragIndex: null,
          dropIndex: null,
          dropPosition: null
        });
      }

      return;
    }

    if (this.state.dragIndex !== dragIndex ||
        this.state.dropIndex !== dropIndex ||
        this.state.dropPosition !== dropPosition) {
      this.setState({
        dragIndex,
        dropIndex,
        dropPosition
      });
    }
  }

  onDownloadProtocolItemDragEnd = (didDrop) => {
    const {
      dragIndex,
      dropIndex
    } = this.state;

    if (didDrop && dropIndex !== null) {
      console.log(`dragged from ${dragIndex} to ${dropIndex}`);

      const delayProfile = _.cloneDeep(this.props.item);
      const items = delayProfile.items.value;
      const item = items.splice(dragIndex, 1)[0];

      items.splice(dropIndex, 0, item);

      this.props.setDelayProfileValue({
        name: 'items',
        value: items
      });
    }

    this.setState({
      dragIndex: null,
      dropIndex: null
    });
  }

  //
  // Render

  render() {
    return (
      <EditDelayProfileModalContent
        {...this.state}
        {...this.props}
        onSavePress={this.onSavePress}
        onInputChange={this.onInputChange}
        onDownloadProtocolItemFieldChange={this.onDownloadProtocolItemFieldChange}
        onDownloadProtocolItemDragMove={this.onDownloadProtocolItemDragMove}
        onDownloadProtocolItemDragEnd={this.onDownloadProtocolItemDragEnd}
      />
    );
  }
}

EditDelayProfileModalContentConnector.propTypes = {
  id: PropTypes.number,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  item: PropTypes.object.isRequired,
  setDelayProfileValue: PropTypes.func.isRequired,
  saveDelayProfile: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(EditDelayProfileModalContentConnector);
