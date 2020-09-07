import PropTypes from 'prop-types';
import React from 'react';
import Icon from 'Components/Icon';
import Popover from 'Components/Tooltip/Popover';
import { icons, kinds, tooltipPositions } from 'Helpers/Props';
import SceneInfo from './SceneInfo';
import styles from './EpisodeNumber.css';

function EpisodeNumber(props) {
  const {
    episodeNumber,
    absoluteEpisodeNumber,
    sceneSeasonNumber,
    sceneEpisodeNumber,
    sceneAbsoluteEpisodeNumber,
    unverifiedSceneNumbering,
    alternateTitles,
    artistType
  } = props;

  const hasSceneInformation = sceneSeasonNumber !== undefined ||
    sceneEpisodeNumber !== undefined ||
    (artistType === 'anime' && sceneAbsoluteEpisodeNumber !== undefined) ||
    !!alternateTitles.length;

  return (
    <span>
      {
        hasSceneInformation ?
          <Popover
            anchor={
              <span>
                {episodeNumber}

                {
                  artistType === 'anime' && !!absoluteEpisodeNumber &&
                    <span className={styles.absoluteEpisodeNumber}>
                      ({absoluteEpisodeNumber})
                    </span>
                }
              </span>
            }
            title="Scene Information"
            body={
              <SceneInfo
                sceneSeasonNumber={sceneSeasonNumber}
                sceneEpisodeNumber={sceneEpisodeNumber}
                sceneAbsoluteEpisodeNumber={sceneAbsoluteEpisodeNumber}
                alternateTitles={alternateTitles}
                artistType={artistType}
              />
            }
            position={tooltipPositions.RIGHT}
          /> :
          <span>
            {episodeNumber}

            {
              artistType === 'anime' && !!absoluteEpisodeNumber &&
                <span className={styles.absoluteEpisodeNumber}>
                  ({absoluteEpisodeNumber})
                </span>
            }
          </span>
      }

      {
        unverifiedSceneNumbering &&
          <Icon
            className={styles.warning}
            name={icons.WARNING}
            kind={kinds.WARNING}
            title="Scene number hasn't been verified yet"
          />
      }

      {
        artistType === 'anime' && !absoluteEpisodeNumber &&
          <Icon
            className={styles.warning}
            name={icons.WARNING}
            kind={kinds.WARNING}
            title="Episode does not have an absolute episode number"
          />
      }
    </span>
  );
}

EpisodeNumber.propTypes = {
  seasonNumber: PropTypes.number.isRequired,
  episodeNumber: PropTypes.number.isRequired,
  absoluteEpisodeNumber: PropTypes.number,
  sceneSeasonNumber: PropTypes.number,
  sceneEpisodeNumber: PropTypes.number,
  sceneAbsoluteEpisodeNumber: PropTypes.number,
  unverifiedSceneNumbering: PropTypes.bool.isRequired,
  alternateTitles: PropTypes.arrayOf(PropTypes.object).isRequired,
  artistType: PropTypes.string
};

EpisodeNumber.defaultProps = {
  unverifiedSceneNumbering: false,
  alternateTitles: []
};

export default EpisodeNumber;
