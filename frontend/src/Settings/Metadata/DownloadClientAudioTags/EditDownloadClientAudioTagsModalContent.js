import PropTypes from 'prop-types';
import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import SpinnerButton from 'Components/Link/SpinnerButton';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes } from 'Helpers/Props';
import { bulkEditDownloadClients } from 'Store/Actions/settingsActions';
import translate from 'Utilities/String/translate';

function EditDownloadClientAudioTagsModalContent({ onModalClose }) {
  const dispatch = useDispatch();

  const { items, isSaving } = useSelector(
    (state) => state.settings.downloadClients
  );

  const sortedItems = useMemo(() => {
    return [...items].sort((a, b) => a.name.localeCompare(b.name));
  }, [items]);

  const [selected, setSelected] = useState({});

  useEffect(() => {
    setSelected(
      Object.fromEntries(items.map((item) => [item.id, item.writeAudioTags]))
    );
  }, [items]);

  const onInputChange = useCallback(({ name, value }) => {
    setSelected((current) => ({ ...current, [name]: value }));
  }, []);

  const onSavePress = useCallback(() => {
    const enabledIds = items
      .filter((item) => selected[item.id] && !item.writeAudioTags)
      .map((item) => item.id);

    const disabledIds = items
      .filter((item) => !selected[item.id] && item.writeAudioTags)
      .map((item) => item.id);

    if (enabledIds.length) {
      dispatch(bulkEditDownloadClients({ ids: enabledIds, writeAudioTags: true }));
    }

    if (disabledIds.length) {
      dispatch(bulkEditDownloadClients({ ids: disabledIds, writeAudioTags: false }));
    }

    onModalClose();
  }, [items, selected, dispatch, onModalClose]);

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {translate('WriteMetadataTags')}
      </ModalHeader>

      <ModalBody>
        <Form>
          {
            sortedItems.map((item) => {
              return (
                <FormGroup key={item.id}>
                  <FormLabel>{item.name}</FormLabel>

                  <FormInputGroup
                    type={inputTypes.CHECK}
                    name={String(item.id)}
                    value={selected[item.id] || false}
                    onChange={onInputChange}
                  />
                </FormGroup>
              );
            })
          }
        </Form>
      </ModalBody>

      <ModalFooter>
        <Button onPress={onModalClose}>
          {translate('Cancel')}
        </Button>

        <SpinnerButton
          isSpinning={isSaving}
          onPress={onSavePress}
        >
          {translate('Save')}
        </SpinnerButton>
      </ModalFooter>
    </ModalContent>
  );
}

EditDownloadClientAudioTagsModalContent.propTypes = {
  onModalClose: PropTypes.func.isRequired
};

export default EditDownloadClientAudioTagsModalContent;
