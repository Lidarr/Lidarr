import PropTypes from 'prop-types';
import React from 'react';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import styles from './SceneInfo.css';

function SceneInfo(props) {
  const {
    sceneSeasonNumber,
    sceneEpisodeNumber,
    sceneAbsoluteEpisodeNumber,
    alternateTitles,
    artistType
  } = props;

  return (
    <DescriptionList className={styles.descriptionList}>
      {
        sceneSeasonNumber !== undefined &&
          <DescriptionListItem
            titleClassName={styles.title}
            descriptionClassName={styles.description}
            title="Season"
            data={sceneSeasonNumber}
          />
      }

      {
        sceneEpisodeNumber !== undefined &&
          <DescriptionListItem
            titleClassName={styles.title}
            descriptionClassName={styles.description}
            title="Episode"
            data={sceneEpisodeNumber}
          />
      }

      {
        artistType === 'anime' && sceneAbsoluteEpisodeNumber !== undefined &&
          <DescriptionListItem
            titleClassName={styles.title}
            descriptionClassName={styles.description}
            title="Absolute"
            data={sceneAbsoluteEpisodeNumber}
          />
      }

      {
        !!alternateTitles.length &&
          <DescriptionListItem
            titleClassName={styles.title}
            descriptionClassName={styles.description}
            title={alternateTitles.length === 1 ? 'Title' : 'Titles'}
            data={
              <div>
                {
                  alternateTitles.map((alternateTitle) => {
                    return (
                      <div
                        key={alternateTitle.title}
                      >
                        {alternateTitle.title}
                      </div>
                    );
                  })
                }
              </div>
            }
          />
      }
    </DescriptionList>
  );
}

SceneInfo.propTypes = {
  sceneSeasonNumber: PropTypes.number,
  sceneEpisodeNumber: PropTypes.number,
  sceneAbsoluteEpisodeNumber: PropTypes.number,
  alternateTitles: PropTypes.arrayOf(PropTypes.object).isRequired,
  artistType: PropTypes.string
};

export default SceneInfo;
