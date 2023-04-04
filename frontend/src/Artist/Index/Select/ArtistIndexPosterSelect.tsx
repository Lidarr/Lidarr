import React, { SyntheticEvent, useCallback } from 'react';
import { useSelect } from 'App/SelectContext';
import Icon from 'Components/Icon';
import Link from 'Components/Link/Link';
import { icons } from 'Helpers/Props';
import styles from './ArtistIndexPosterSelect.css';

interface ArtistIndexPosterSelectProps {
  artistId: number;
}

function ArtistIndexPosterSelect(props: ArtistIndexPosterSelectProps) {
  const { artistId } = props;
  const [selectState, selectDispatch] = useSelect();
  const isSelected = selectState.selectedState[artistId];

  const onSelectPress = useCallback(
    (event: SyntheticEvent) => {
      const nativeEvent = event.nativeEvent as PointerEvent;
      const shiftKey = nativeEvent.shiftKey;

      selectDispatch({
        type: 'toggleSelected',
        id: artistId,
        isSelected: !isSelected,
        shiftKey,
      });
    },
    [artistId, isSelected, selectDispatch]
  );

  return (
    <Link className={styles.checkButton} onPress={onSelectPress}>
      <span className={styles.checkContainer}>
        <Icon
          className={isSelected ? styles.selected : styles.unselected}
          name={isSelected ? icons.CHECK_CIRCLE : icons.CIRCLE_OUTLINE}
          size={20}
        />
      </span>
    </Link>
  );
}

export default ArtistIndexPosterSelect;
