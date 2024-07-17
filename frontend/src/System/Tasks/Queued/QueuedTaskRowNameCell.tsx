import React from 'react';
import { useSelector } from 'react-redux';
import { CommandBody } from 'Commands/Command';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import createMultiArtistsSelector from 'Store/Selectors/createMultiArtistsSelector';
import sortByProp from 'Utilities/Array/sortByProp';
import translate from 'Utilities/String/translate';
import styles from './QueuedTaskRowNameCell.css';

function formatTitles(titles: string[]) {
  if (!titles) {
    return null;
  }

  if (titles.length > 11) {
    return (
      <span title={titles.join(', ')}>
        {titles.slice(0, 10).join(', ')}, {titles.length - 10} more
      </span>
    );
  }

  return <span>{titles.join(', ')}</span>;
}

export interface QueuedTaskRowNameCellProps {
  commandName: string;
  body: CommandBody;
  clientUserAgent?: string;
}

export default function QueuedTaskRowNameCell(
  props: QueuedTaskRowNameCellProps
) {
  const { commandName, body, clientUserAgent } = props;
  const movieIds = [...(body.artistIds ?? [])];

  if (body.artistId) {
    movieIds.push(body.artistId);
  }

  const artists = useSelector(createMultiArtistsSelector(movieIds));
  const sortedArtists = artists.sort(sortByProp('sortName'));

  return (
    <TableRowCell>
      <span className={styles.commandName}>
        {commandName}
        {sortedArtists.length ? (
          <span> - {formatTitles(sortedArtists.map((a) => a.artistName))}</span>
        ) : null}
      </span>

      {clientUserAgent ? (
        <span
          className={styles.userAgent}
          title={translate('TaskUserAgentTooltip')}
        >
          {translate('From')}: {clientUserAgent}
        </span>
      ) : null}
    </TableRowCell>
  );
}
