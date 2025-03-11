import PropTypes from 'prop-types';
import React from 'react';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import DescriptionListItemDescription from 'Components/DescriptionList/DescriptionListItemDescription';
import DescriptionListItemTitle from 'Components/DescriptionList/DescriptionListItemTitle';
import Icon from 'Components/Icon';
import Link from 'Components/Link/Link';
import { icons } from 'Helpers/Props';
import formatDateTime from 'Utilities/Date/formatDateTime';
import formatAge from 'Utilities/Number/formatAge';
import formatCustomFormatScore from 'Utilities/Number/formatCustomFormatScore';
import translate from 'Utilities/String/translate';
import styles from './HistoryDetails.css';

function getDetailedList(statusMessages) {
  return (
    <div>
      {
        statusMessages.map(({ title, messages }) => {
          return (
            <div key={title}>
              {title}
              <ul>
                {
                  messages.map((message) => {
                    return (
                      <li key={message}>
                        {message}
                      </li>
                    );
                  })
                }
              </ul>
            </div>
          );
        })
      }
    </div>
  );
}

function formatMissing(value) {
  if (value === undefined || value === 0 || value === '0') {
    return (<Icon name={icons.BAN} size={12} />);
  }
  return value;
}

function formatChange(oldValue, newValue) {
  return (
    <div>
      {formatMissing(oldValue)} <Icon name={icons.ARROW_RIGHT_NO_CIRCLE} size={12} /> {formatMissing(newValue)}
    </div>
  );
}

function HistoryDetails(props) {
  const {
    eventType,
    sourceTitle,
    data,
    downloadId,
    shortDateFormat,
    timeFormat
  } = props;

  if (eventType === 'grabbed') {
    const {
      indexer,
      releaseGroup,
      customFormatScore,
      nzbInfoUrl,
      downloadClient,
      downloadClientName,
      age,
      ageHours,
      ageMinutes,
      publishedDate
    } = data;

    const downloadClientNameInfo = downloadClientName ?? downloadClient;

    return (
      <DescriptionList>
        <DescriptionListItem
          descriptionClassName={styles.description}
          title={translate('Name')}
          data={sourceTitle}
        />

        {
          indexer ?
            <DescriptionListItem
              title={translate('Indexer')}
              data={indexer}
            /> :
            null
        }

        {
          releaseGroup ?
            <DescriptionListItem
              descriptionClassName={styles.description}
              title={translate('ReleaseGroup')}
              data={releaseGroup}
            /> :
            null
        }

        {
          customFormatScore && customFormatScore !== '0' ?
            <DescriptionListItem
              title={translate('CustomFormatScore')}
              data={formatCustomFormatScore(customFormatScore)}
            /> :
            null
        }

        {
          nzbInfoUrl ?
            <span>
              <DescriptionListItemTitle>
                {translate('InfoUrl')}
              </DescriptionListItemTitle>

              <DescriptionListItemDescription>
                <Link to={nzbInfoUrl}>{nzbInfoUrl}</Link>
              </DescriptionListItemDescription>
            </span> :
            null
        }

        {
          downloadClientNameInfo ?
            <DescriptionListItem
              title={translate('DownloadClient')}
              data={downloadClientNameInfo}
            /> :
            null
        }

        {
          downloadId ?
            <DescriptionListItem
              title={translate('GrabId')}
              data={downloadId}
            /> :
            null
        }

        {
          age || ageHours || ageMinutes ?
            <DescriptionListItem
              title={translate('AgeWhenGrabbed')}
              data={formatAge(age, ageHours, ageMinutes)}
            /> :
            null
        }

        {
          publishedDate ?
            <DescriptionListItem
              title={translate('PublishedDate')}
              data={formatDateTime(publishedDate, shortDateFormat, timeFormat, { includeSeconds: true })}
            /> :
            null
        }
      </DescriptionList>
    );
  }

  if (eventType === 'downloadFailed') {
    const {
      message,
      indexer
    } = data;

    return (
      <DescriptionList>
        <DescriptionListItem
          descriptionClassName={styles.description}
          title={translate('Name')}
          data={sourceTitle}
        />

        {
          downloadId ?
            <DescriptionListItem
              title={translate('GrabId')}
              data={downloadId}
            /> :
            null
        }

        {
          indexer ? (
            <DescriptionListItem
              title={translate('Indexer')}
              data={indexer}
            />
          ) : null}

        {
          message ?
            <DescriptionListItem
              title={translate('Message')}
              data={message}
            /> :
            null
        }
      </DescriptionList>
    );
  }

  if (eventType === 'trackFileImported') {
    const {
      customFormatScore,
      droppedPath,
      importedPath
    } = data;

    return (
      <DescriptionList>
        <DescriptionListItem
          descriptionClassName={styles.description}
          title={translate('Name')}
          data={sourceTitle}
        />

        {
          droppedPath ?
            <DescriptionListItem
              descriptionClassName={styles.description}
              title={translate('Source')}
              data={droppedPath}
            /> :
            null
        }

        {
          importedPath ?
            <DescriptionListItem
              descriptionClassName={styles.description}
              title={translate('ImportedTo')}
              data={importedPath}
            /> :
            null
        }

        {
          customFormatScore && customFormatScore !== '0' ?
            <DescriptionListItem
              title={translate('CustomFormatScore')}
              data={formatCustomFormatScore(customFormatScore)}
            /> :
            null
        }
      </DescriptionList>
    );
  }

  if (eventType === 'trackFileDeleted') {
    const {
      reason,
      customFormatScore
    } = data;

    let reasonMessage = '';

    switch (reason) {
      case 'Manual':
        reasonMessage = 'File was deleted by via UI';
        break;
      case 'MissingFromDisk':
        reasonMessage = 'Lidarr was unable to find the file on disk so the file was unlinked from the album/track in the database';
        break;
      case 'Upgrade':
        reasonMessage = 'File was deleted to import an upgrade';
        break;
      default:
        reasonMessage = '';
    }

    return (
      <DescriptionList>
        <DescriptionListItem
          title={translate('Name')}
          data={sourceTitle}
        />

        <DescriptionListItem
          title={translate('Reason')}
          data={reasonMessage}
        />

        {
          customFormatScore && customFormatScore !== '0' ?
            <DescriptionListItem
              title={translate('CustomFormatScore')}
              data={formatCustomFormatScore(customFormatScore)}
            /> :
            null
        }
      </DescriptionList>
    );
  }

  if (eventType === 'trackFileRenamed') {
    const {
      sourcePath,
      path
    } = data;

    return (
      <DescriptionList>
        <DescriptionListItem
          title={translate('SourcePath')}
          data={sourcePath}
        />

        <DescriptionListItem
          title={translate('DestinationPath')}
          data={path}
        />
      </DescriptionList>
    );
  }

  if (eventType === 'trackFileRetagged') {
    const {
      diff,
      tagsScrubbed
    } = data;

    return (
      <DescriptionList>
        <DescriptionListItem
          title={translate('Path')}
          data={sourceTitle}
        />
        {
          JSON.parse(diff).map(({ field, oldValue, newValue }) => {
            return (
              <DescriptionListItem
                key={field}
                title={field}
                data={formatChange(oldValue, newValue)}
              />
            );
          })
        }
        <DescriptionListItem
          title={translate('ExistingTagsScrubbed')}
          data={tagsScrubbed === 'True' ? <Icon name={icons.CHECK} /> : <Icon name={icons.REMOVE} />}
        />
      </DescriptionList>
    );
  }

  if (eventType === 'albumImportIncomplete') {
    const {
      statusMessages
    } = data;

    return (
      <DescriptionList>
        <DescriptionListItem
          title={translate('Name')}
          data={sourceTitle}
        />

        {
          !!statusMessages &&
            <DescriptionListItem
              title={translate('ImportFailures')}
              data={getDetailedList(JSON.parse(statusMessages))}
            />
        }
      </DescriptionList>
    );
  }

  if (eventType === 'downloadImported') {
    const {
      indexer,
      releaseGroup,
      customFormatScore,
      nzbInfoUrl,
      downloadClient,
      age,
      ageHours,
      ageMinutes,
      publishedDate
    } = data;

    return (
      <DescriptionList>
        <DescriptionListItem
          title={translate('Name')}
          data={sourceTitle}
        />

        {
          indexer ?
            <DescriptionListItem
              title={translate('Indexer')}
              data={indexer}
            /> :
            null
        }

        {
          releaseGroup ?
            <DescriptionListItem
              title={translate('ReleaseGroup')}
              data={releaseGroup}
            /> :
            null
        }

        {
          customFormatScore && customFormatScore !== '0' ?
            <DescriptionListItem
              title={translate('CustomFormatScore')}
              data={formatCustomFormatScore(customFormatScore)}
            /> :
            null
        }

        {
          nzbInfoUrl ?
            <span>
              <DescriptionListItemTitle>
                {translate('InfoUrl')}
              </DescriptionListItemTitle>

              <DescriptionListItemDescription>
                <Link to={nzbInfoUrl}>{nzbInfoUrl}</Link>
              </DescriptionListItemDescription>
            </span> :
            null
        }

        {
          downloadClient ?
            <DescriptionListItem
              title={translate('DownloadClient')}
              data={downloadClient}
            /> :
            null
        }

        {
          downloadId ?
            <DescriptionListItem
              title={translate('GrabId')}
              data={downloadId}
            /> :
            null
        }

        {
          age || ageHours || ageMinutes ?
            <DescriptionListItem
              title={translate('AgeWhenGrabbed')}
              data={formatAge(age, ageHours, ageMinutes)}
            /> :
            null
        }

        {
          publishedDate ?
            <DescriptionListItem
              title={translate('PublishedDate')}
              data={formatDateTime(publishedDate, shortDateFormat, timeFormat, { includeSeconds: true })}
            /> :
            null
        }
      </DescriptionList>
    );
  }

  if (eventType === 'downloadIgnored') {
    const {
      message
    } = data;

    return (
      <DescriptionList>
        <DescriptionListItem
          descriptionClassName={styles.description}
          title={translate('Name')}
          data={sourceTitle}
        />

        {
          downloadId ?
            <DescriptionListItem
              title={translate('GrabId')}
              data={downloadId}
            /> :
            null
        }

        {
          message ?
            <DescriptionListItem
              title={translate('Message')}
              data={message}
            /> :
            null
        }
      </DescriptionList>
    );
  }

  return (
    <DescriptionList>
      <DescriptionListItem
        descriptionClassName={styles.description}
        title={translate('Name')}
        data={sourceTitle}
      />
    </DescriptionList>
  );
}

HistoryDetails.propTypes = {
  eventType: PropTypes.string.isRequired,
  sourceTitle: PropTypes.string.isRequired,
  data: PropTypes.object.isRequired,
  downloadId: PropTypes.string,
  shortDateFormat: PropTypes.string.isRequired,
  timeFormat: PropTypes.string.isRequired
};

export default HistoryDetails;
