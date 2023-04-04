import React, { useCallback } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes } from 'Helpers/Props';
import { setArtistOverviewOption } from 'Store/Actions/artistIndexActions';
import translate from 'Utilities/String/translate';
import selectOverviewOptions from '../selectOverviewOptions';

const posterSizeOptions = [
  {
    key: 'small',
    get value() {
      return translate('Small');
    },
  },
  {
    key: 'medium',
    get value() {
      return translate('Medium');
    },
  },
  {
    key: 'large',
    get value() {
      return translate('Large');
    },
  },
];

interface ArtistIndexOverviewOptionsModalContentProps {
  onModalClose(...args: unknown[]): void;
}

function ArtistIndexOverviewOptionsModalContent(
  props: ArtistIndexOverviewOptionsModalContentProps
) {
  const { onModalClose } = props;

  const {
    detailedProgressBar,
    size,
    showMonitored,
    showQualityProfile,
    showLastAlbum,
    showAdded,
    showAlbumCount,
    showPath,
    showSizeOnDisk,
    showSearchAction,
  } = useSelector(selectOverviewOptions);

  const dispatch = useDispatch();

  const onOverviewOptionChange = useCallback(
    ({ name, value }: { name: string; value: unknown }) => {
      dispatch(setArtistOverviewOption({ [name]: value }));
    },
    [dispatch]
  );

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>{translate('OverviewOptions')}</ModalHeader>

      <ModalBody>
        <Form>
          <FormGroup>
            <FormLabel>{translate('PosterSize')}</FormLabel>

            <FormInputGroup
              type={inputTypes.SELECT}
              name="size"
              value={size}
              values={posterSizeOptions}
              onChange={onOverviewOptionChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('DetailedProgressBar')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="detailedProgressBar"
              value={detailedProgressBar}
              helpText={translate('DetailedProgressBarHelpText')}
              onChange={onOverviewOptionChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('ShowMonitored')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="showMonitored"
              value={showMonitored}
              onChange={onOverviewOptionChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('ShowQualityProfile')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="showQualityProfile"
              value={showQualityProfile}
              onChange={onOverviewOptionChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('ShowLastAlbum')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="showLastAlbum"
              value={showLastAlbum}
              onChange={onOverviewOptionChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('ShowDateAdded')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="showAdded"
              value={showAdded}
              onChange={onOverviewOptionChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('ShowAlbumCount')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="showAlbumCount"
              value={showAlbumCount}
              onChange={onOverviewOptionChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('ShowPath')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="showPath"
              value={showPath}
              onChange={onOverviewOptionChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('ShowSizeOnDisk')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="showSizeOnDisk"
              value={showSizeOnDisk}
              onChange={onOverviewOptionChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('ShowSearch')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="showSearchAction"
              value={showSearchAction}
              helpText={translate('ShowSearchActionHelpText')}
              onChange={onOverviewOptionChange}
            />
          </FormGroup>
        </Form>
      </ModalBody>

      <ModalFooter>
        <Button onPress={onModalClose}>{translate('Close')}</Button>
      </ModalFooter>
    </ModalContent>
  );
}

export default ArtistIndexOverviewOptionsModalContent;
