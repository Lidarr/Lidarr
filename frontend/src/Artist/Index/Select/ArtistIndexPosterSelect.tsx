import React, { useCallback } from 'react';
import { SelectActionType, useSelect } from 'App/SelectContext';
import IconButton from 'Components/Link/IconButton';
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
    (event) => {
      const shiftKey = event.nativeEvent.shiftKey;

      selectDispatch({
        type: SelectActionType.ToggleSelected,
        id: artistId,
        isSelected: !isSelected,
        shiftKey,
      });
    },
    [artistId, isSelected, selectDispatch]
  );

  return (
    <IconButton
      className={styles.checkContainer}
      iconClassName={isSelected ? styles.selected : styles.unselected}
      name={isSelected ? icons.CHECK_CIRCLE : icons.CIRCLE_OUTLINE}
      size={20}
      onPress={onSelectPress}
    />
  );
}

export default ArtistIndexPosterSelect;
