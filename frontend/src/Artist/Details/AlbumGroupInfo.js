import PropTypes from 'prop-types';
import React from 'react';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import formatBytes from 'Utilities/Number/formatBytes';
import styles from './AlbumGroupInfo.css';

function AlbumGroupInfo(props) {
  const {
    totalAlbumCount,
    monitoredAlbumCount,
    trackFileCount,
    sizeOnDisk
  } = props;

  return (
    <DescriptionList>
      <DescriptionListItem
        titleClassName={styles.title}
        descriptionClassName={styles.description}
        title="Total"
        data={totalAlbumCount}
      />

      <DescriptionListItem
        titleClassName={styles.title}
        descriptionClassName={styles.description}
        title="Monitored"
        data={monitoredAlbumCount}
      />

      <DescriptionListItem
        titleClassName={styles.title}
        descriptionClassName={styles.description}
        title="Track Files"
        data={trackFileCount}
      />

      <DescriptionListItem
        titleClassName={styles.title}
        descriptionClassName={styles.description}
        title="Size on Disk"
        data={formatBytes(sizeOnDisk)}
      />
    </DescriptionList>
  );
}

AlbumGroupInfo.propTypes = {
  totalAlbumCount: PropTypes.number.isRequired,
  monitoredAlbumCount: PropTypes.number.isRequired,
  trackFileCount: PropTypes.number.isRequired,
  sizeOnDisk: PropTypes.number.isRequired
};

export default AlbumGroupInfo;
