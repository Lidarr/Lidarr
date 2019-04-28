import PropTypes from 'prop-types';
import React from 'react';
import formatBytes from 'Utilities/Number/formatBytes';
import { kinds } from 'Helpers/Props';
import Label from 'Components/Label';

function getTooltip(title, quality, size) {
  if (!title) {
    return;
  }

  const revision = quality.revision;

  if (revision.real && revision.real > 0) {
    title += ' [REAL]';
  }

  if (revision.version && revision.version > 1) {
    title += ' [PROPER]';
  }

  if (size) {
    title += ` - ${formatBytes(size)}`;
  }

  return title;
}

function TrackQuality(props) {
  const {
    className,
    title,
    quality,
    size,
    isCutoffNotMet
  } = props;

  return (
    <Label
      className={className}
      kind={isCutoffNotMet ? kinds.INVERSE : kinds.DEFAULT}
      title={getTooltip(title, quality, size)}
    >
      {quality.quality.name}
    </Label>
  );
}

TrackQuality.propTypes = {
  className: PropTypes.string,
  title: PropTypes.string,
  quality: PropTypes.object.isRequired,
  size: PropTypes.number,
  isCutoffNotMet: PropTypes.bool
};

TrackQuality.defaultProps = {
  title: ''
};

export default TrackQuality;
