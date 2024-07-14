import PropTypes from 'prop-types';
import React from 'react';
import Link from 'Components/Link/Link';

function AlbumTitleLink({ foreignAlbumId, title, disambiguation }) {
  const link = `/album/${foreignAlbumId}`;
  const albumTitle = `${title}${disambiguation ? ` (${disambiguation})` : ''}`;

  return (
    <Link to={link} title={albumTitle}>
      {albumTitle}
    </Link>
  );
}

AlbumTitleLink.propTypes = {
  foreignAlbumId: PropTypes.string.isRequired,
  title: PropTypes.string.isRequired,
  disambiguation: PropTypes.string
};

export default AlbumTitleLink;
