import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { fetchDelayProfileSchema, saveDelayProfile, setDelayProfileValue } from 'Store/Actions/settingsActions';
import createProviderSettingsSelector from 'Store/Selectors/createProviderSettingsSelector';
import EditDelayProfileModalContent from './EditDelayProfileModalContent';

function createMapStateToProps() {
  return createSelector(
    createProviderSettingsSelector('delayProfiles'),
    (delayProfile) => {
      return {
        ...delayProfile
      };
    }
  );
}

const mapDispatchToProps = {
  fetchDelayProfileSchema,
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
    if (!this.props.id && !this.props.isPopulated) {
      this.props.fetchDelayProfileSchema();
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
  };

  onSavePress = () => {
    this.props.saveDelayProfile({ id: this.props.id });
  };

  onDownloadProtocolItemFieldChange = (protocol, name, value) => {
    const delayProfile = _.cloneDeep(this.props.item);
    const items = delayProfile.items.value;
    const item = _.find(delayProfile.items.value, (i) => i.protocol === protocol);

    item[name] = value;

    this.props.setDelayProfileValue({
      name: 'items',
      value: items
    });
  };

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
  };

  onDownloadProtocolItemDragEnd = (didDrop) => {
    const {
      dragIndex,
      dropIndex
    } = this.state;

    if (didDrop && dropIndex !== null) {
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
  };

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
  isPopulated: PropTypes.bool.isRequired,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  item: PropTypes.object.isRequired,
  fetchDelayProfileSchema: PropTypes.func.isRequired,
  setDelayProfileValue: PropTypes.func.isRequired,
  saveDelayProfile: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(EditDelayProfileModalContentConnector);
