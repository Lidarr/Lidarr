import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import sortByProp from 'Utilities/Array/sortByProp';
import shortenList from 'Utilities/String/shortenList';
import titleCase from 'Utilities/String/titleCase';
import SelectInput from './SelectInput';

function createMapStateToProps() {
  return createSelector(
    (state, { albumReleases }) => albumReleases,
    (albumReleases) => {
      const sortedValues = albumReleases.value
        .map((albumRelease) => {
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
            `${format ? `, [${format}]` : ''}`
          };
        })
        .sort(sortByProp('value'));

      const value = albumReleases.value.find((release) => release.monitored)?.foreignReleaseId;

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

  onChange = ({ name, value }) => {
    const {
      albumReleases
    } = this.props;

    const updatedReleases = albumReleases.value.map((e) => ({ ...e, monitored: false }));
    updatedReleases.find((e) => e.foreignReleaseId === value).monitored = true;

    this.props.onChange({ name, value: updatedReleases });
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
