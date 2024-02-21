import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes, kinds, scrollDirections } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './SelectIndexerFlagsModalContent.css';

class SelectIndexerFlagsModalContent extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    const {
      indexerFlags
    } = props;

    this.state = {
      indexerFlags
    };
  }

  //
  // Listeners

  onIndexerFlagsChange = ({ value }) => {
    this.setState({ indexerFlags: value });
  };

  onIndexerFlagsSelect = () => {
    this.props.onIndexerFlagsSelect(this.state);
  };

  //
  // Render

  render() {
    const {
      onModalClose
    } = this.props;

    const {
      indexerFlags
    } = this.state;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          Manual Import - Set indexer Flags
        </ModalHeader>

        <ModalBody
          className={styles.modalBody}
          scrollDirection={scrollDirections.NONE}
        >
          <Form>
            <FormGroup>
              <FormLabel>
                {translate('IndexerFlags')}
              </FormLabel>

              <FormInputGroup
                type={inputTypes.INDEXER_FLAGS_SELECT}
                name="indexerFlags"
                indexerFlags={indexerFlags}
                autoFocus={true}
                onChange={this.onIndexerFlagsChange}
              />
            </FormGroup>
          </Form>
        </ModalBody>

        <ModalFooter>
          <Button onPress={onModalClose}>
            {translate('Cancel')}
          </Button>

          <Button
            kind={kinds.SUCCESS}
            onPress={this.onIndexerFlagsSelect}
          >
            {translate('SetIndexerFlags')}
          </Button>
        </ModalFooter>
      </ModalContent>
    );
  }
}

SelectIndexerFlagsModalContent.propTypes = {
  indexerFlags: PropTypes.number.isRequired,
  onIndexerFlagsSelect: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default SelectIndexerFlagsModalContent;
