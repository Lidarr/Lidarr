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
import AddIndexerItem from './AddIndexerItem';
import styles from './AddIndexerModalContent.css';

function mapIndexers(indexers, onIndexerSelect) {
  return indexers.map((indexer) => {
    return (
      <AddIndexerItem
        key={indexer.implementation}
        implementation={indexer.implementation}
        {...indexer}
        onIndexerSelect={onIndexerSelect}
      />
    );
  });
}

class AddIndexerModalContent extends Component {

  //
  // Render

  render() {
    const {
      isSchemaFetching,
      isSchemaPopulated,
      schemaError,
      usenetIndexers,
      torrentIndexers,
      otherIndexers,
      onIndexerSelect,
      onModalClose
    } = this.props;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          Add Indexer
        </ModalHeader>

        <ModalBody>
          {
            isSchemaFetching &&
              <LoadingIndicator />
          }

          {
            !isSchemaFetching && !!schemaError &&
              <div>
                {translate('UnableToAddANewIndexerPleaseTryAgain')}
              </div>
          }

          {
            isSchemaPopulated && !schemaError &&
              <div>

                <Alert kind={kinds.INFO}>
                  <div>
                    {translate('LidarrSupportsAnyIndexerThatUsesTheNewznabStandardAsWellAsOtherIndexersListedBelow')}
                  </div>
                  <div>
                    {translate('ForMoreInformationOnTheIndividualIndexersClickOnTheInfoButtons')}
                  </div>
                </Alert>

                <FieldSet legend={translate('Usenet')}>
                  <div className={styles.indexers}>
                    {
                      mapIndexers(usenetIndexers, onIndexerSelect)
                    }
                  </div>
                </FieldSet>

                <FieldSet legend={translate('Torrents')}>
                  <div className={styles.indexers}>
                    {
                      mapIndexers(torrentIndexers, onIndexerSelect)
                    }
                  </div>
                </FieldSet>

                {
                  otherIndexers.length ?
                    <FieldSet legend="Other">
                      <div className={styles.indexers}>
                        {
                          mapIndexers(otherIndexers, onIndexerSelect)
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

AddIndexerModalContent.propTypes = {
  isSchemaFetching: PropTypes.bool.isRequired,
  isSchemaPopulated: PropTypes.bool.isRequired,
  schemaError: PropTypes.object,
  usenetIndexers: PropTypes.arrayOf(PropTypes.object).isRequired,
  torrentIndexers: PropTypes.arrayOf(PropTypes.object).isRequired,
  otherIndexers: PropTypes.arrayOf(PropTypes.object).isRequired,
  onIndexerSelect: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default AddIndexerModalContent;
