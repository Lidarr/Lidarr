import PropTypes from 'prop-types';
import React, { Component } from 'react';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import TableOptionsModalWrapper from 'Components/Table/TableOptions/TableOptionsModalWrapper';
import VirtualTable from 'Components/Table/VirtualTable';
import VirtualTableRow from 'Components/Table/VirtualTableRow';
import { align, icons, sortDirections } from 'Helpers/Props';
import UnmappedFilesTableHeader from './UnmappedFilesTableHeader';
import UnmappedFilesTableRow from './UnmappedFilesTableRow';

class UnmappedFilesTable extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      scroller: null
    };
  }

  //
  // Control

  setScrollerRef = (ref) => {
    this.setState({ scroller: ref });
  }

  rowRenderer = ({ key, rowIndex, style }) => {
    const {
      items,
      columns,
      deleteUnmappedFile
    } = this.props;

    const item = items[rowIndex];

    return (
      <VirtualTableRow
        key={key}
        style={style}
      >
        <UnmappedFilesTableRow
          key={item.id}
          columns={columns}
          deleteUnmappedFile={deleteUnmappedFile}
          {...item}
        />
      </VirtualTableRow>
    );
  }

  render() {

    const {
      isFetching,
      isPopulated,
      error,
      items,
      columns,
      sortKey,
      sortDirection,
      onTableOptionChange,
      onSortPress,
      isScanningFolders,
      onAddMissingArtistsPress,
      ...otherProps
    } = this.props;

    const {
      scroller
    } = this.state;

    return (
      <PageContent title="UnmappedFiles">
        <PageToolbar>
          <PageToolbarSection>
            <PageToolbarButton
              label="Add missing"
              iconName={icons.ADD_MISSING_ARTISTS}
              isDisabled={isPopulated && !error && !items.length}
              isSpinning={isScanningFolders}
              onPress={onAddMissingArtistsPress}
            />
          </PageToolbarSection>

          <PageToolbarSection alignContent={align.RIGHT}>
            <TableOptionsModalWrapper
              {...otherProps}
              columns={columns}
              onTableOptionChange={onTableOptionChange}
            >
              <PageToolbarButton
                label="Options"
                iconName={icons.TABLE}
              />
            </TableOptionsModalWrapper>

          </PageToolbarSection>
        </PageToolbar>

        <PageContentBody
          registerScroller={this.setScrollerRef}
        >
          {
            isFetching && !isPopulated &&
              <LoadingIndicator />
          }

          {
            isPopulated && !error && !items.length &&
              <div>
                Success! My work is done, all files on disk are matched to known tracks.
              </div>
          }

          {
            isPopulated && !error && !!items.length && scroller &&
              <VirtualTable
                items={items}
                columns={columns}
                scroller={scroller}
                isSmallScreen={false}
                overscanRowCount={10}
                rowRenderer={this.rowRenderer}
                header={
                  <UnmappedFilesTableHeader
                    columns={columns}
                    sortKey={sortKey}
                    sortDirection={sortDirection}
                    onTableOptionChange={onTableOptionChange}
                    onSortPress={onSortPress}
                  />
                }
                sortKey={sortKey}
                sortDirection={sortDirection}
              />
          }
        </PageContentBody>
      </PageContent>
    );
  }
}

UnmappedFilesTable.propTypes = {
  isFetching: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired,
  error: PropTypes.object,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  sortKey: PropTypes.string,
  sortDirection: PropTypes.oneOf(sortDirections.all),
  onTableOptionChange: PropTypes.func.isRequired,
  onSortPress: PropTypes.func.isRequired,
  deleteUnmappedFile: PropTypes.func.isRequired,
  isScanningFolders: PropTypes.bool.isRequired,
  onAddMissingArtistsPress: PropTypes.func.isRequired
};

export default UnmappedFilesTable;
