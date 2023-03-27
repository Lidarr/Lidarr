import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Label from 'Components/Label';
import VirtualTableRowCell from 'Components/Table/Cells/VirtualTableRowCell';
import styles from './SelectArtistRow.css';

class SelectArtistRow extends Component {

  //
  // Listeners

  onPress = () => {
    this.props.onArtistSelect(this.props.id);
  };

  //
  // Render

  render() {
    return (
      <>
        <VirtualTableRowCell className={styles.artistName}>
          {this.props.artistName}
        </VirtualTableRowCell>

        <VirtualTableRowCell className={styles.foreignArtistId}>
          <Label>{this.props.foreignArtistId}</Label>
        </VirtualTableRowCell>
      </>
    );
  }
}

SelectArtistRow.propTypes = {
  id: PropTypes.number.isRequired,
  artistName: PropTypes.string.isRequired,
  foreignArtistId: PropTypes.string.isRequired,
  onArtistSelect: PropTypes.func.isRequired
};

export default SelectArtistRow;
