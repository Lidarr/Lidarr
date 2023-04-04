import { IconDefinition } from '@fortawesome/fontawesome-common-types';
import React, { useCallback } from 'react';
import { useSelect } from 'App/SelectContext';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';

interface ArtistIndexSelectModeButtonProps {
  label: string;
  iconName: IconDefinition;
  isSelectMode: boolean;
  overflowComponent: React.FunctionComponent<never>;
  onPress: () => void;
}

function ArtistIndexSelectModeButton(props: ArtistIndexSelectModeButtonProps) {
  const { label, iconName, isSelectMode, onPress } = props;
  const [, selectDispatch] = useSelect();

  const onPressWrapper = useCallback(() => {
    if (isSelectMode) {
      selectDispatch({
        type: 'reset',
      });
    }

    onPress();
  }, [isSelectMode, onPress, selectDispatch]);

  return (
    <PageToolbarButton
      label={label}
      iconName={iconName}
      onPress={onPressWrapper}
    />
  );
}

export default ArtistIndexSelectModeButton;
