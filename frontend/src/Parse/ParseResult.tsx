import _ from 'lodash';
import moment from 'moment';
import React from 'react';
import AlbumFormats from 'Album/AlbumFormats';
import AlbumTitleLink from 'Album/AlbumTitleLink';
import { ParseModel } from 'App/State/ParseAppState';
import ArtistNameLink from 'Artist/ArtistNameLink';
import FieldSet from 'Components/FieldSet';
import translate from 'Utilities/String/translate';
import ParseResultItem from './ParseResultItem';
import styles from './ParseResult.css';

interface ParseResultProps {
  item: ParseModel;
}

function ParseResult(props: ParseResultProps) {
  const { item } = props;
  const { customFormats, customFormatScore, albums, parsedAlbumInfo, artist } =
    item;

  const {
    releaseTitle,
    artistName,
    albumTitle,
    releaseGroup,
    discography,
    quality,
  } = parsedAlbumInfo;

  const sortedAlbums = _.sortBy(albums, (item) =>
    moment(item.releaseDate).unix()
  );

  return (
    <div>
      <FieldSet legend={translate('Release')}>
        <ParseResultItem
          title={translate('ReleaseTitle')}
          data={releaseTitle}
        />

        <ParseResultItem title={translate('ArtistName')} data={artistName} />

        <ParseResultItem title={translate('AlbumTitle')} data={albumTitle} />

        <ParseResultItem
          title={translate('ReleaseGroup')}
          data={releaseGroup ?? '-'}
        />
      </FieldSet>

      <FieldSet legend={translate('AlbumInfo')}>
        <div className={styles.container}>
          <div className={styles.column}>
            <ParseResultItem
              title={translate('Discography')}
              data={discography ? translate('True') : translate('False')}
            />
          </div>
        </div>
      </FieldSet>

      <FieldSet legend={translate('Quality')}>
        <div className={styles.container}>
          <div className={styles.column}>
            <ParseResultItem
              title={translate('Quality')}
              data={quality.quality.name}
            />
            <ParseResultItem
              title={translate('Proper')}
              data={
                quality.revision.version > 1 && !quality.revision.isRepack
                  ? translate('True')
                  : '-'
              }
            />

            <ParseResultItem
              title={translate('Repack')}
              data={quality.revision.isRepack ? translate('True') : '-'}
            />
          </div>

          <div className={styles.column}>
            <ParseResultItem
              title={translate('Version')}
              data={
                quality.revision.version > 1 ? quality.revision.version : '-'
              }
            />

            <ParseResultItem
              title={translate('Real')}
              data={quality.revision.real ? translate('True') : '-'}
            />
          </div>
        </div>
      </FieldSet>

      <FieldSet legend={translate('Details')}>
        <ParseResultItem
          title={translate('MatchedToArtist')}
          data={
            artist ? (
              <ArtistNameLink
                foreignArtistId={artist.foreignArtistId}
                artistName={artist.artistName}
              />
            ) : (
              '-'
            )
          }
        />

        <ParseResultItem
          title={translate('MatchedToAlbums')}
          data={
            sortedAlbums.length ? (
              <div>
                {sortedAlbums.map((album) => {
                  return (
                    <div key={album.id}>
                      <AlbumTitleLink
                        foreignAlbumId={album.foreignAlbumId}
                        title={album.title}
                        disambiguation={album.disambiguation}
                      />
                    </div>
                  );
                })}
              </div>
            ) : (
              '-'
            )
          }
        />

        <ParseResultItem
          title={translate('CustomFormats')}
          data={
            customFormats?.length ? (
              <AlbumFormats formats={customFormats} />
            ) : (
              '-'
            )
          }
        />

        <ParseResultItem
          title={translate('CustomFormatScore')}
          data={customFormatScore}
        />
      </FieldSet>
    </div>
  );
}

export default ParseResult;
