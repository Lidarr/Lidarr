import classNames from 'classnames';
import React from 'react';
import { useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import { ColorImpairedConsumer } from 'App/ColorImpairedContext';
import ArtistAppState from 'App/State/ArtistAppState';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import createClientSideCollectionSelector from 'Store/Selectors/createClientSideCollectionSelector';
import createDeepEqualSelector from 'Store/Selectors/createDeepEqualSelector';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';
import styles from './ArtistIndexFooter.css';

function createUnoptimizedSelector() {
  return createSelector(
    createClientSideCollectionSelector('artist', 'artistIndex'),
    (artist: ArtistAppState) => {
      return artist.items.map((s) => {
        const { monitored, status, statistics } = s;

        return {
          monitored,
          status,
          statistics,
        };
      });
    }
  );
}

function createArtistSelector() {
  return createDeepEqualSelector(
    createUnoptimizedSelector(),
    (artist) => artist
  );
}

export default function ArtistIndexFooter() {
  const artist = useSelector(createArtistSelector());
  const count = artist.length;
  let tracks = 0;
  let trackFiles = 0;
  let ended = 0;
  let continuing = 0;
  let monitored = 0;
  let totalFileSize = 0;

  artist.forEach((a) => {
    const { statistics = { trackCount: 0, trackFileCount: 0, sizeOnDisk: 0 } } =
      a;

    const { trackCount = 0, trackFileCount = 0, sizeOnDisk = 0 } = statistics;

    tracks += trackCount;
    trackFiles += trackFileCount;

    if (a.status === 'ended') {
      ended++;
    } else {
      continuing++;
    }

    if (a.monitored) {
      monitored++;
    }

    totalFileSize += sizeOnDisk;
  });

  return (
    <ColorImpairedConsumer>
      {(enableColorImpairedMode) => {
        return (
          <div className={styles.footer}>
            <div>
              <div className={styles.legendItem}>
                <div
                  className={classNames(
                    styles.continuing,
                    enableColorImpairedMode && 'colorImpaired'
                  )}
                />
                <div>{translate('ContinuingAllTracksDownloaded')}</div>
              </div>

              <div className={styles.legendItem}>
                <div
                  className={classNames(
                    styles.ended,
                    enableColorImpairedMode && 'colorImpaired'
                  )}
                />
                <div>{translate('EndedAllTracksDownloaded')}</div>
              </div>

              <div className={styles.legendItem}>
                <div
                  className={classNames(
                    styles.missingMonitored,
                    enableColorImpairedMode && 'colorImpaired'
                  )}
                />
                <div>{translate('MissingTracksArtistMonitored')}</div>
              </div>

              <div className={styles.legendItem}>
                <div
                  className={classNames(
                    styles.missingUnmonitored,
                    enableColorImpairedMode && 'colorImpaired'
                  )}
                />
                <div>{translate('MissingTracksArtistNotMonitored')}</div>
              </div>
            </div>

            <div className={styles.statistics}>
              <DescriptionList>
                <DescriptionListItem title={translate('Artist')} data={count} />

                <DescriptionListItem
                  title={translate('Inactive')}
                  data={ended}
                />

                <DescriptionListItem
                  title={translate('Continuing')}
                  data={continuing}
                />
              </DescriptionList>

              <DescriptionList>
                <DescriptionListItem
                  title={translate('Monitored')}
                  data={monitored}
                />

                <DescriptionListItem
                  title={translate('Unmonitored')}
                  data={count - monitored}
                />
              </DescriptionList>

              <DescriptionList>
                <DescriptionListItem
                  title={translate('Tracks')}
                  data={tracks}
                />

                <DescriptionListItem
                  title={translate('Files')}
                  data={trackFiles}
                />
              </DescriptionList>

              <DescriptionList>
                <DescriptionListItem
                  title={translate('TotalFileSize')}
                  data={formatBytes(totalFileSize)}
                />
              </DescriptionList>
            </div>
          </div>
        );
      }}
    </ColorImpairedConsumer>
  );
}
