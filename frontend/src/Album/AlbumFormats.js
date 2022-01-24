import PropTypes from 'prop-types';
import React from 'react';
import Label from 'Components/Label';
import { kinds } from 'Helpers/Props';

function AlbumFormats({ formats }) {
  return (
    <div>
      {
        formats.map((format) => {
          return (
            <Label
              key={format.id}
              kind={kinds.INFO}
            >
              {format.name}
            </Label>
          );
        })
      }
    </div>
  );
}

AlbumFormats.propTypes = {
  formats: PropTypes.arrayOf(PropTypes.object).isRequired
};

AlbumFormats.defaultProps = {
  formats: []
};

export default AlbumFormats;
