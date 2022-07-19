import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Alert from 'Components/Alert';
import FieldSet from 'Components/FieldSet';
import Button from 'Components/Link/Button';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import AddDownloadClientItem from './AddDownloadClientItem';
import styles from './AddDownloadClientModalContent.css';

function mapDownloadClients(clients, onDownloadClientSelect) {
  return clients.map((downloadClient) => {
    return (
      <AddDownloadClientItem
        key={downloadClient.implementation}
        implementation={downloadClient.implementation}
        {...downloadClient}
        onDownloadClientSelect={onDownloadClientSelect}
      />
    );
  });
}

class AddDownloadClientModalContent extends Component {

  //
  // Render

  render() {
    const {
      isSchemaFetching,
      isSchemaPopulated,
      schemaError,
      usenetDownloadClients,
      torrentDownloadClients,
      otherDownloadClients,
      onDownloadClientSelect,
      onModalClose
    } = this.props;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          Add Download Client
        </ModalHeader>

        <ModalBody>
          {
            isSchemaFetching &&
              <LoadingIndicator />
          }

          {
            !isSchemaFetching && !!schemaError &&
              <div>
                {translate('UnableToAddANewDownloadClientPleaseTryAgain')}
              </div>
          }

          {
            isSchemaPopulated && !schemaError &&
              <div>

                <Alert kind={kinds.INFO}>
                  <div>
                    {translate('LidarrSupportsAnyDownloadClientThatUsesTheNewznabStandardAsWellAsOtherDownloadClientsListedBelow')}
                  </div>
                  <div>
                    {translate('ForMoreInformationOnTheIndividualDownloadClientsClickOnTheInfoButtons')}
                  </div>
                </Alert>

                <FieldSet legend={translate('Usenet')}>
                  <div className={styles.downloadClients}>
                    {
                      mapDownloadClients(usenetDownloadClients, onDownloadClientSelect)
                    }
                  </div>
                </FieldSet>

                <FieldSet legend={translate('Torrents')}>
                  <div className={styles.downloadClients}>
                    {
                      mapDownloadClients(torrentDownloadClients, onDownloadClientSelect)
                    }
                  </div>
                </FieldSet>

                {
                  otherDownloadClients.length ?
                    <FieldSet legend="Other">
                      <div className={styles.downloadClients}>
                        {
                          mapDownloadClients(otherDownloadClients, onDownloadClientSelect)
                        }
                      </div>
                    </FieldSet> :
                    null
                }
              </div>
          }
        </ModalBody>
        <ModalFooter>
          <Button
            onPress={onModalClose}
          >
            Close
          </Button>
        </ModalFooter>
      </ModalContent>
    );
  }
}

AddDownloadClientModalContent.propTypes = {
  isSchemaFetching: PropTypes.bool.isRequired,
  isSchemaPopulated: PropTypes.bool.isRequired,
  schemaError: PropTypes.object,
  usenetDownloadClients: PropTypes.arrayOf(PropTypes.object).isRequired,
  torrentDownloadClients: PropTypes.arrayOf(PropTypes.object).isRequired,
  otherDownloadClients: PropTypes.arrayOf(PropTypes.object).isRequired,
  onDownloadClientSelect: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default AddDownloadClientModalContent;
