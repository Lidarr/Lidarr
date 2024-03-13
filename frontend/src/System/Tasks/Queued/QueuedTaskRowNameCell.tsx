import React from 'react';
import { useSelector } from 'react-redux';
import { CommandBody } from 'Commands/Command';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import createMultiArtistsSelector from 'Store/Selectors/createMultiArtistsSelector';
import translate from 'Utilities/String/translate';
import styles from './QueuedTaskRowNameCell.css';

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
  const sortedArtists = artists.sort((a, b) =>
    a.sortName.localeCompare(b.sortName)
  );

  return (
    <TableRowCell>
      <span className={styles.commandName}>
        {commandName}
        {sortedArtists.length ? (
          <span> - {sortedArtists.map((a) => a.artistName).join(', ')}</span>
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
