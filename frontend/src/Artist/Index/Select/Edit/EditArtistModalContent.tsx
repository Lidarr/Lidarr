import React, { useCallback, useState } from 'react';
import MoveArtistModal from 'Artist/MoveArtist/MoveArtistModal';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './EditArtistModalContent.css';

interface SavePayload {
  monitored?: boolean;
  monitorNewItems?: string;
  qualityProfileId?: number;
  metadataProfileId?: number;
  rootFolderPath?: string;
  moveFiles?: boolean;
}

interface EditArtistModalContentProps {
  artistIds: number[];
  onSavePress(payload: object): void;
  onModalClose(): void;
}

const NO_CHANGE = 'noChange';

const monitoredOptions = [
  {
    key: NO_CHANGE,
    get value() {
      return translate('NoChange');
    },
    disabled: true,
  },
  {
    key: 'monitored',
    get value() {
      return translate('Monitored');
    },
  },
  {
    key: 'unmonitored',
    get value() {
      return translate('Unmonitored');
    },
  },
];

function EditArtistModalContent(props: EditArtistModalContentProps) {
  const { artistIds, onSavePress, onModalClose } = props;

  const [monitored, setMonitored] = useState(NO_CHANGE);
  const [monitorNewItems, setMonitorNewItems] = useState(NO_CHANGE);
  const [qualityProfileId, setQualityProfileId] = useState<string | number>(
    NO_CHANGE
  );
  const [metadataProfileId, setMetadataProfileId] = useState<string | number>(
    NO_CHANGE
  );
  const [rootFolderPath, setRootFolderPath] = useState(NO_CHANGE);
  const [isConfirmMoveModalOpen, setIsConfirmMoveModalOpen] = useState(false);

  const save = useCallback(
    (moveFiles: boolean) => {
      let hasChanges = false;
      const payload: SavePayload = {};

      if (monitored !== NO_CHANGE) {
        hasChanges = true;
        payload.monitored = monitored === 'monitored';
      }

      if (monitorNewItems !== NO_CHANGE) {
        hasChanges = true;
        payload.monitorNewItems = monitorNewItems;
      }

      if (qualityProfileId !== NO_CHANGE) {
        hasChanges = true;
        payload.qualityProfileId = qualityProfileId as number;
      }

      if (metadataProfileId !== NO_CHANGE) {
        hasChanges = true;
        payload.metadataProfileId = metadataProfileId as number;
      }

      if (rootFolderPath !== NO_CHANGE) {
        hasChanges = true;
        payload.rootFolderPath = rootFolderPath;
        payload.moveFiles = moveFiles;
      }

      if (hasChanges) {
        onSavePress(payload);
      }

      onModalClose();
    },
    [
      monitored,
      monitorNewItems,
      qualityProfileId,
      metadataProfileId,
      rootFolderPath,
      onSavePress,
      onModalClose,
    ]
  );

  const onInputChange = useCallback(
    ({ name, value }: { name: string; value: string }) => {
      switch (name) {
        case 'monitored':
          setMonitored(value);
          break;
        case 'monitorNewItems':
          setMonitorNewItems(value);
          break;
        case 'qualityProfileId':
          setQualityProfileId(value);
          break;
        case 'metadataProfileId':
          setMetadataProfileId(value);
          break;
        case 'rootFolderPath':
          setRootFolderPath(value);
          break;
        default:
          console.warn('EditArtistModalContent Unknown Input');
      }
    },
    [setMonitored]
  );

  const onSavePressWrapper = useCallback(() => {
    if (rootFolderPath === NO_CHANGE) {
      save(false);
    } else {
      setIsConfirmMoveModalOpen(true);
    }
  }, [rootFolderPath, save]);

  const onCancelPress = useCallback(() => {
    setIsConfirmMoveModalOpen(false);
  }, [setIsConfirmMoveModalOpen]);

  const onDoNotMoveArtistPress = useCallback(() => {
    setIsConfirmMoveModalOpen(false);
    save(false);
  }, [setIsConfirmMoveModalOpen, save]);

  const onMoveArtistPress = useCallback(() => {
    setIsConfirmMoveModalOpen(false);
    save(true);
  }, [setIsConfirmMoveModalOpen, save]);

  const selectedCount = artistIds.length;

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>{translate('EditSelectedArtists')}</ModalHeader>

      <ModalBody>
        <FormGroup>
          <FormLabel>{translate('Monitored')}</FormLabel>

          <FormInputGroup
            type={inputTypes.SELECT}
            name="monitored"
            value={monitored}
            values={monitoredOptions}
            onChange={onInputChange}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel>{translate('MonitorNewItems')}</FormLabel>

          <FormInputGroup
            type={inputTypes.MONITOR_NEW_ITEMS_SELECT}
            name="monitorNewItems"
            value={monitorNewItems}
            includeNoChange={true}
            includeNoChangeDisabled={false}
            onChange={onInputChange}
            helpText={translate('MonitorNewItemsHelpText')}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel>{translate('QualityProfile')}</FormLabel>

          <FormInputGroup
            type={inputTypes.QUALITY_PROFILE_SELECT}
            name="qualityProfileId"
            value={qualityProfileId}
            includeNoChange={true}
            includeNoChangeDisabled={false}
            onChange={onInputChange}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel>{translate('MetadataProfile')}</FormLabel>

          <FormInputGroup
            type={inputTypes.METADATA_PROFILE_SELECT}
            name="metadataProfileId"
            value={metadataProfileId}
            includeNoChange={true}
            includeNoChangeDisabled={false}
            includeNone={true}
            onChange={onInputChange}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel>{translate('RootFolder')}</FormLabel>

          <FormInputGroup
            type={inputTypes.ROOT_FOLDER_SELECT}
            name="rootFolderPath"
            value={rootFolderPath}
            includeNoChange={true}
            includeNoChangeDisabled={false}
            selectedValueOptions={{ includeFreeSpace: false }}
            helpText={translate('ArtistsEditRootFolderHelpText')}
            onChange={onInputChange}
          />
        </FormGroup>
      </ModalBody>

      <ModalFooter className={styles.modalFooter}>
        <div className={styles.selected}>
          {translate('CountArtistsSelected', { count: selectedCount })}
        </div>

        <div>
          <Button onPress={onModalClose}>{translate('Cancel')}</Button>

          <Button onPress={onSavePressWrapper}>
            {translate('ApplyChanges')}
          </Button>
        </div>
      </ModalFooter>

      <MoveArtistModal
        isOpen={isConfirmMoveModalOpen}
        destinationRootFolder={rootFolderPath}
        onModalClose={onCancelPress}
        onSavePress={onDoNotMoveArtistPress}
        onMoveArtistPress={onMoveArtistPress}
      />
    </ModalContent>
  );
}

export default EditArtistModalContent;
