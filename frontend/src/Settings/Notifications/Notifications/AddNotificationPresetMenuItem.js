import PropTypes from 'prop-types';
import React, { Component } from 'react';
import MenuItem from 'Components/Menu/MenuItem';

class AddNotificationPresetMenuItem extends Component {

  //
  // Listeners

  onPress = () => {
    const {
      name,
      implementation
    } = this.props;

    this.props.onPress({
      name,
      implementation
    });
  }

  //
  // Render

  render() {
    const {
      name,
      /* eslint-disable no-unused-vars */
      implementation,
      /* eslint-enable no-unused-vars */
      ...otherProps
    } = this.props;

    return (
      <MenuItem
        {...otherProps}
        onPress={this.onPress}
      >
        {name}
      </MenuItem>
    );
  }
}

AddNotificationPresetMenuItem.propTypes = {
  name: PropTypes.string.isRequired,
  implementation: PropTypes.string.isRequired,
  onPress: PropTypes.func.isRequired
};

export default AddNotificationPresetMenuItem;
