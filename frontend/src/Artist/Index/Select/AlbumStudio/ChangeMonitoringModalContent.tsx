import React, { useCallback, useState } from 'react';
import ArtistMonitoringOptionsPopoverContent from 'AddArtist/ArtistMonitoringOptionsPopoverContent';
import Alert from 'Components/Alert';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Icon from 'Components/Icon';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import Popover from 'Components/Tooltip/Popover';
import { icons, inputTypes, kinds, tooltipPositions } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './ChangeMonitoringModalContent.css';

const NO_CHANGE = 'noChange';

interface ChangeMonitoringModalContentProps {
  artistIds: number[];
  saveError?: object;
  onSavePress(monitor: string): void;
  onModalClose(): void;
}

function ChangeMonitoringModalContent(
  props: ChangeMonitoringModalContentProps
) {
  const { artistIds, onSavePress, onModalClose, ...otherProps } = props;

  const [monitor, setMonitor] = useState(NO_CHANGE);

  const onInputChange = useCallback(
    ({ value }: { value: string }) => {
      setMonitor(value);
    },
    [setMonitor]
  );

  const onSavePressWrapper = useCallback(() => {
    onSavePress(monitor);
  }, [monitor, onSavePress]);

  const selectedCount = artistIds.length;

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>{translate('MonitorArtists')}</ModalHeader>

      <ModalBody>
        <Alert kind={kinds.INFO} className={styles.message}>
          {translate('MonitorAlbumExistingOnlyWarning')}
        </Alert>

        <Form {...otherProps}>
          <FormGroup>
            <FormLabel>
              {translate('MonitorExistingAlbums')}

              <Popover
                anchor={<Icon className={styles.labelIcon} name={icons.INFO} />}
                title={translate('MonitoringOptions')}
                body={<ArtistMonitoringOptionsPopoverContent />}
                position={tooltipPositions.RIGHT}
              />
            </FormLabel>

            <FormInputGroup
              type={inputTypes.MONITOR_ALBUMS_SELECT}
              name="monitor"
              value={monitor}
              includeNoChange={true}
              onChange={onInputChange}
            />
          </FormGroup>
        </Form>
      </ModalBody>

      <ModalFooter className={styles.modalFooter}>
        <div className={styles.selected}>
          {translate('CountArtistsSelected', { count: selectedCount })}
        </div>

        <div>
          <Button onPress={onModalClose}>{translate('Cancel')}</Button>

          <Button onPress={onSavePressWrapper}>{translate('Save')}</Button>
        </div>
      </ModalFooter>
    </ModalContent>
  );
}

export default ChangeMonitoringModalContent;
