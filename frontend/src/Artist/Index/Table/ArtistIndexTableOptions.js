import PropTypes from 'prop-types';
import React, { Component, Fragment } from 'react';
import { inputTypes } from 'Helpers/Props';
import FormGroup from 'Components/Form/FormGroup';
import FormLabel from 'Components/Form/FormLabel';
import FormInputGroup from 'Components/Form/FormInputGroup';

class ArtistIndexTableOptions extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      showBanners: props.showBanners,
      showSearchAction: props.showSearchAction
    };
  }

  componentDidUpdate(prevProps) {
    const {
      showBanners,
      showSearchAction
    } = this.props;

    if (
      showBanners !== prevProps.showBanners ||
      showSearchAction !== prevProps.showSearchAction
    ) {
      this.setState({
        showBanners,
        showSearchAction
      });
    }
  }

  //
  // Listeners

  onTableOptionChange = ({ name, value }) => {
    this.setState({
      [name]: value
    }, () => {
      this.props.onTableOptionChange({
        tableOptions: {
          ...this.state,
          [name]: value
        }
      });
    });
  }

  //
  // Render

  render() {
    const {
      showBanners,
      showSearchAction
    } = this.state;

    return (
      <Fragment>
        <FormGroup>
          <FormLabel>Show Banners</FormLabel>

          <FormInputGroup
            type={inputTypes.CHECK}
            name="showBanners"
            value={showBanners}
            helpText="Show banners instead of names"
            onChange={this.onTableOptionChange}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel>Show Search</FormLabel>

          <FormInputGroup
            type={inputTypes.CHECK}
            name="showSearchAction"
            value={showSearchAction}
            helpText="Show search button on hover"
            onChange={this.onTableOptionChange}
          />
        </FormGroup>
      </Fragment>
    );
  }
}

ArtistIndexTableOptions.propTypes = {
  showBanners: PropTypes.bool.isRequired,
  showSearchAction: PropTypes.bool.isRequired,
  onTableOptionChange: PropTypes.func.isRequired
};

export default ArtistIndexTableOptions;
