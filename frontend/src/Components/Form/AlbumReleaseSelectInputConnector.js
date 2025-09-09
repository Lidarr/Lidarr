import _ from 'lodash';
import PropTypes from 'prop-types';
import React, {Component} from 'react';
import {connect} from 'react-redux';
import {createSelector} from 'reselect';
import shortenList from 'Utilities/String/shortenList';
import titleCase from 'Utilities/String/titleCase';
import SelectInput from './SelectInput';

function createMapStateToProps() {
  return createSelector(
    (state, {albumReleases}) => albumReleases,
    (albumReleases) => {
      const values = _.map(albumReleases.value, (albumRelease) => {

        const {
          foreignReleaseId,
          title,
          disambiguation,
          mediumCount,
          trackCount,
          country,
          format
        } = albumRelease;

        return {
          key: foreignReleaseId,
          value: `${title}` +
            `${disambiguation ? ' (' : ''}${titleCase(disambiguation)}${disambiguation ? ')' : ''}` +
            `, ${mediumCount} med, ${trackCount} tracks` +
            `${country && country.length > 0 ? `, ${shortenList(country)}` : ''}` +
            `${format ? `, [${format}]` : ''}` +
            `, ${foreignReleaseId.toString()}`
        };
      });

      const sortedValues = _.orderBy(values, ['value']);

      const value = _.find(albumReleases.value, {monitored: true}).foreignReleaseId;

      return {
        values: sortedValues,
        value
      };
    }
  );
}

class AlbumReleaseSelectInputConnector extends Component {

  //
  // Listeners

  onChange = ({name, value}) => {
    const {
      albumReleases
    } = this.props;

    const updatedReleases = _.map(albumReleases.value, (e) => ({...e, monitored: false}));
    _.find(updatedReleases, {foreignReleaseId: value}).monitored = true;

    this.props.onChange({name, value: updatedReleases});
  };

  render() {

    return (
      <SelectInput
        {...this.props}
        onChange={this.onChange}
      />
    );
  }
}

AlbumReleaseSelectInputConnector.propTypes = {
  name: PropTypes.string.isRequired,
  onChange: PropTypes.func.isRequired,
  albumReleases: PropTypes.object
};

export default connect(createMapStateToProps)(AlbumReleaseSelectInputConnector);
