import PropTypes from 'prop-types';
import React from 'react';
import Link from 'Components/Link/Link';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import translate from 'Utilities/String/translate';
import styles from './SongPopularityCell.css';

function SongPopularityCell({ popularity }) {
  if (!popularity) {
    return <TableRowCell className={styles.cell} title={translate('SongPopularityUnknownHelp')}><span className={styles.unknown}>—</span></TableRowCell>;
  }
  const { position, providerScore, sourceUrl } = popularity;
  const level = 5 - [10, 25, 50, 75].filter((limit) => position > limit).length;

  return (
    <TableRowCell className={styles.cell}>
      <Link
        to={sourceUrl}
        title={translate('DeezerSongPositionHelp', [position, providerScore.toLocaleString()])}
        className={styles.source}
      >
        <span className={styles.bars} aria-hidden={true}>
          {
            [1, 2, 3, 4, 5].map((bar) => (
              <span
                key={bar}
                className={bar <= level ? styles.activeBar : styles.bar}
                style={{ height: `${5 + bar * 2}px` }}
              />
            ))
          }
        </span>
        <span>
          <span className={styles.position}>{translate('SongPopularityTopPosition', [position])}</span>
          <span className={styles.provider}>Deezer</span>
        </span>
      </Link>
    </TableRowCell>
  );
}

SongPopularityCell.propTypes = {
  popularity: PropTypes.shape({
    position: PropTypes.number.isRequired,
    providerScore: PropTypes.number.isRequired,
    sourceUrl: PropTypes.string.isRequired
  })
};

export default SongPopularityCell;
