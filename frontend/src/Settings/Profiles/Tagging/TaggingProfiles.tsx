import React, { useCallback, useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import AppState from 'App/State/AppState';
import FieldSet from 'Components/FieldSet';
import Icon from 'Components/Icon';
import Link from 'Components/Link/Link';
import Measure from 'Components/Measure';
import PageSectionContent from 'Components/Page/PageSectionContent';
import Scroller from 'Components/Scroller/Scroller';
import { icons } from 'Helpers/Props';
import ScrollDirection from 'Helpers/Props/ScrollDirection';
import {
  deleteTaggingProfile,
  fetchTaggingProfiles,
  reorderTaggingProfile,
} from 'Store/Actions/settingsActions';
import createTagsSelector from 'Store/Selectors/createTagsSelector';
import translate from 'Utilities/String/translate';
import EditTaggingProfileModal from './EditTaggingProfileModal';
import TaggingProfile from './TaggingProfile';
import TaggingProfileDragPreview from './TaggingProfileDragPreview';
import TaggingProfileDragSource from './TaggingProfileDragSource';
import styles from './TaggingProfiles.css';

function TaggingProfiles() {
  const dispatch = useDispatch();

  const { isFetching, isPopulated, error, items } = useSelector(
    (state: AppState) => state.settings.taggingProfiles
  );

  const tagList = useSelector(createTagsSelector());

  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [width, setWidth] = useState(0);
  const [dragIndex, setDragIndex] = useState<number | null>(null);
  const [dropIndex, setDropIndex] = useState<number | null>(null);

  useEffect(() => {
    dispatch(fetchTaggingProfiles());
  }, [dispatch]);

  const defaultProfile = items.find((item) => item.id === 1);
  const profiles = items
    .filter((item) => item.id !== 1)
    .sort((a, b) => a.order - b.order);

  const onAddPress = useCallback(() => {
    setIsAddModalOpen(true);
  }, []);

  const onAddModalClose = useCallback(() => {
    setIsAddModalOpen(false);
  }, []);

  const onMeasure = useCallback(({ width: newWidth }: { width: number }) => {
    setWidth(newWidth);
  }, []);

  const onConfirmDeleteTaggingProfile = useCallback(
    (id: number) => {
      dispatch(deleteTaggingProfile({ id }));
    },
    [dispatch]
  );

  const onTaggingProfileDragMove = useCallback(
    (newDragIndex: number, newDropIndex: number) => {
      setDragIndex(newDragIndex);
      setDropIndex(newDropIndex);
    },
    []
  );

  const onTaggingProfileDragEnd = useCallback(
    ({ id }: { id: number }, didDrop: boolean) => {
      if (didDrop && dropIndex !== null) {
        dispatch(reorderTaggingProfile({ id, moveIndex: dropIndex - 1 }));
      }

      setDragIndex(null);
      setDropIndex(null);
    },
    [dispatch, dropIndex]
  );

  const isDragging = dropIndex !== null;
  const isDraggingUp =
    isDragging && dragIndex !== null && dropIndex < dragIndex;
  const isDraggingDown =
    isDragging && dragIndex !== null && dropIndex > dragIndex;

  return (
    <Measure onMeasure={onMeasure}>
      <FieldSet legend={translate('TaggingProfiles')}>
        <PageSectionContent
          errorMessage={translate('UnableToLoadTaggingProfiles')}
          isFetching={isFetching}
          isPopulated={isPopulated}
          error={error}
        >
          <Scroller
            className={styles.horizontalScroll}
            scrollDirection={ScrollDirection.Horizontal}
            autoFocus={false}
          >
            <div>
              <div className={styles.taggingProfilesHeader}>
                <div className={styles.name}>{translate('Name')}</div>
                <div className={styles.writeAudioTags}>
                  {translate('TagAudioFiles')}
                </div>
                <div className={styles.optionColumn}>
                  {translate('EmbedCoverArt')}
                </div>
                <div className={styles.optionColumn}>
                  {translate('ScrubExistingTags')}
                </div>
                <div className={styles.optionColumn}>
                  {translate('SkipHardlinkedFiles')}
                </div>
                <div className={styles.fillcolumn}>{translate('Tags')}</div>
                <div className={styles.actions} />
              </div>

              <div className={styles.taggingProfiles}>
                {profiles.map((item, index) => {
                  return (
                    <TaggingProfileDragSource
                      key={item.id}
                      tagList={tagList}
                      {...item}
                      index={index}
                      isDragging={isDragging}
                      isDraggingUp={isDraggingUp}
                      isDraggingDown={isDraggingDown}
                      onConfirmDeleteTaggingProfile={
                        onConfirmDeleteTaggingProfile
                      }
                      onTaggingProfileDragMove={onTaggingProfileDragMove}
                      onTaggingProfileDragEnd={onTaggingProfileDragEnd}
                    />
                  );
                })}

                <TaggingProfileDragPreview width={width} />
              </div>

              {defaultProfile ? (
                <div>
                  <TaggingProfile
                    tagList={tagList}
                    isDragging={false}
                    onConfirmDeleteTaggingProfile={
                      onConfirmDeleteTaggingProfile
                    }
                    {...defaultProfile}
                  />
                </div>
              ) : null}
            </div>
          </Scroller>

          <div className={styles.addTaggingProfile}>
            <Link className={styles.addButton} onPress={onAddPress}>
              <Icon name={icons.ADD} />
            </Link>
          </div>

          <EditTaggingProfileModal
            isOpen={isAddModalOpen}
            onModalClose={onAddModalClose}
          />
        </PageSectionContent>
      </FieldSet>
    </Measure>
  );
}

export default TaggingProfiles;
