import PropTypes from 'prop-types';
import React from 'react';
import TrackQuality from 'Album/TrackQuality';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableSelectCell from 'Components/Table/Cells/TableSelectCell';
import TableRow from 'Components/Table/TableRow';
import padNumber from 'Utilities/Number/padNumber';

function TrackFileEditorRow(props) {
  const {
    id,
    trackNumber,
    path,
    quality,
    qualityCutoffNotMet,
    isSelected,
    onSelectedChange
  } = props;

  return (
    <TableRow>
      <TableSelectCell
        id={id}
        isSelected={isSelected}
        onSelectedChange={onSelectedChange}
      />

      <TableRowCell>
        {padNumber(trackNumber, 2)}
      </TableRowCell>

      <TableRowCell>
        {path}
      </TableRowCell>

      <TableRowCell>
        <TrackQuality
          quality={quality}
          isCutoffNotMet={qualityCutoffNotMet}
        />
      </TableRowCell>
    </TableRow>
  );
}

TrackFileEditorRow.propTypes = {
  id: PropTypes.number.isRequired,
  trackNumber: PropTypes.string.isRequired,
  path: PropTypes.string.isRequired,
  quality: PropTypes.object.isRequired,
  qualityCutoffNotMet: PropTypes.bool.isRequired,
  isSelected: PropTypes.bool,
  onSelectedChange: PropTypes.func.isRequired
};

export default TrackFileEditorRow;
