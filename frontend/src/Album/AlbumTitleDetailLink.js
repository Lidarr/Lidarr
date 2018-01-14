import PropTypes from 'prop-types';
import React from 'react';
import Link from 'Components/Link/Link';

function AlbumTitleDetailLink({ foreignArtistId, foreignAlbumId, title }) {
  const link = `/album/${foreignAlbumId}`;

  return (
    <Link to={link}>
      {title}
    </Link>
  );
}

AlbumTitleDetailLink.propTypes = {
  foreignArtistId: PropTypes.string.isRequired,
  foreignAlbumId: PropTypes.string.isRequired,
  title: PropTypes.string.isRequired
};

export default AlbumTitleDetailLink;
