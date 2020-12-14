import PropTypes from 'prop-types';
import React, { Component } from 'react';
import NoArtist from 'Artist/NoArtist';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import FilterMenu from 'Components/Menu/FilterMenu';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import PageToolbarSeparator from 'Components/Page/Toolbar/PageToolbarSeparator';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import TableOptionsModalWrapper from 'Components/Table/TableOptions/TableOptionsModalWrapper';
import { align, icons, sortDirections } from 'Helpers/Props';
import getErrorMessage from 'Utilities/Object/getErrorMessage';
import getSelectedIds from 'Utilities/Table/getSelectedIds';
import selectAll from 'Utilities/Table/selectAll';
import toggleSelected from 'Utilities/Table/toggleSelected';
import ArtistEditorFilterModalConnector from './ArtistEditorFilterModalConnector';
import ArtistEditorFooter from './ArtistEditorFooter';
import ArtistEditorRowConnector from './ArtistEditorRowConnector';
import RetagArtistModal from './AudioTags/RetagArtistModal';
import OrganizeArtistModal from './Organize/OrganizeArtistModal';

class ArtistEditor extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      allSelected: false,
      allUnselected: false,
      lastToggled: null,
      selectedState: {},
      isOrganizingArtistModalOpen: false,
      isRetaggingArtistModalOpen: false
    };
  }

  componentDidUpdate(prevProps) {
    const {
      isDeleting,
      deleteError
    } = this.props;

    const hasFinishedDeleting = prevProps.isDeleting &&
                                !isDeleting &&
                                !deleteError;

    if (hasFinishedDeleting) {
      this.onSelectAllChange({ value: false });
    }
  }

  //
  // Control

  getSelectedIds = () => {
    return getSelectedIds(this.state.selectedState);
  }

  //
  // Listeners

  onSelectAllChange = ({ value }) => {
    this.setState(selectAll(this.state.selectedState, value));
  }

  onSelectedChange = ({ id, value, shiftKey = false }) => {
    this.setState((state) => {
      return toggleSelected(state, this.props.items, id, value, shiftKey);
    });
  }

  onSaveSelected = (changes) => {
    this.props.onSaveSelected({
      artistIds: this.getSelectedIds(),
      ...changes
    });
  }

  onOrganizeArtistPress = () => {
    this.setState({ isOrganizingArtistModalOpen: true });
  }

  onOrganizeArtistModalClose = (organized) => {
    this.setState({ isOrganizingArtistModalOpen: false });

    if (organized === true) {
      this.onSelectAllChange({ value: false });
    }
  }

  onRetagArtistPress = () => {
    this.setState({ isRetaggingArtistModalOpen: true });
  }

  onRetagArtistModalClose = (organized) => {
    this.setState({ isRetaggingArtistModalOpen: false });

    if (organized === true) {
      this.onSelectAllChange({ value: false });
    }
  }

  //
  // Render

  render() {
    const {
      isFetching,
      isPopulated,
      error,
      totalItems,
      items,
      columns,
      selectedFilterKey,
      filters,
      customFilters,
      sortKey,
      sortDirection,
      isSaving,
      saveError,
      isDeleting,
      deleteError,
      isOrganizingArtist,
      isRetaggingArtist,
      onTableOptionChange,
      onSortPress,
      onFilterSelect
    } = this.props;

    const {
      allSelected,
      allUnselected,
      selectedState
    } = this.state;

    const selectedArtistIds = this.getSelectedIds();

    return (
      <PageContent title="Artist Editor">
        <PageToolbar>
          <PageToolbarSection />
          <PageToolbarSection alignContent={align.RIGHT}>
            <TableOptionsModalWrapper
              columns={columns}
              onTableOptionChange={onTableOptionChange}
            >
              <PageToolbarButton
                label="Options"
                iconName={icons.TABLE}
              />
            </TableOptionsModalWrapper>

            <PageToolbarSeparator />

            <FilterMenu
              alignMenu={align.RIGHT}
              selectedFilterKey={selectedFilterKey}
              filters={filters}
              customFilters={customFilters}
              filterModalConnectorComponent={ArtistEditorFilterModalConnector}
              onFilterSelect={onFilterSelect}
            />
          </PageToolbarSection>
        </PageToolbar>

        <PageContentBody>
          {
            isFetching && !isPopulated &&
              <LoadingIndicator />
          }

          {
            !isFetching && !!error &&
              <div>{getErrorMessage(error, 'Failed to load artist from API')}</div>
          }

          {
            !error && isPopulated && !!items.length &&
              <div>
                <Table
                  columns={columns}
                  sortKey={sortKey}
                  sortDirection={sortDirection}
                  selectAll={true}
                  allSelected={allSelected}
                  allUnselected={allUnselected}
                  onSortPress={onSortPress}
                  onSelectAllChange={this.onSelectAllChange}
                >
                  <TableBody>
                    {
                      items.map((item) => {
                        return (
                          <ArtistEditorRowConnector
                            key={item.id}
                            {...item}
                            columns={columns}
                            isSaving={isSaving}
                            isSelected={selectedState[item.id]}
                            onSelectedChange={this.onSelectedChange}
                          />
                        );
                      })
                    }
                  </TableBody>
                </Table>
              </div>
          }

          {
            !error && isPopulated && !items.length &&
              <NoArtist totalItems={totalItems} />
          }
        </PageContentBody>

        <ArtistEditorFooter
          artistIds={selectedArtistIds}
          selectedCount={selectedArtistIds.length}
          isSaving={isSaving}
          saveError={saveError}
          isDeleting={isDeleting}
          deleteError={deleteError}
          isOrganizingArtist={isOrganizingArtist}
          isRetaggingArtist={isRetaggingArtist}
          columns={columns}
          showMetadataProfile={columns.find((column) => column.name === 'metadataProfileId').isVisible}
          onSaveSelected={this.onSaveSelected}
          onOrganizeArtistPress={this.onOrganizeArtistPress}
          onRetagArtistPress={this.onRetagArtistPress}
        />

        <OrganizeArtistModal
          isOpen={this.state.isOrganizingArtistModalOpen}
          artistIds={selectedArtistIds}
          onModalClose={this.onOrganizeArtistModalClose}
        />

        <RetagArtistModal
          isOpen={this.state.isRetaggingArtistModalOpen}
          artistIds={selectedArtistIds}
          onModalClose={this.onRetagArtistModalClose}
        />

      </PageContent>
    );
  }
}

ArtistEditor.propTypes = {
  isFetching: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired,
  error: PropTypes.object,
  totalItems: PropTypes.number.isRequired,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  sortKey: PropTypes.string,
  sortDirection: PropTypes.oneOf(sortDirections.all),
  selectedFilterKey: PropTypes.oneOfType([PropTypes.string, PropTypes.number]).isRequired,
  filters: PropTypes.arrayOf(PropTypes.object).isRequired,
  customFilters: PropTypes.arrayOf(PropTypes.object).isRequired,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  isDeleting: PropTypes.bool.isRequired,
  deleteError: PropTypes.object,
  isOrganizingArtist: PropTypes.bool.isRequired,
  isRetaggingArtist: PropTypes.bool.isRequired,
  onTableOptionChange: PropTypes.func.isRequired,
  onSortPress: PropTypes.func.isRequired,
  onFilterSelect: PropTypes.func.isRequired,
  onSaveSelected: PropTypes.func.isRequired
};

export default ArtistEditor;
