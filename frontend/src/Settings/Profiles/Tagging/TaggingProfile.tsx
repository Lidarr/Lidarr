import classNames from 'classnames';
import React, { useCallback, useState } from 'react';
import { Tag } from 'App/State/TagsAppState';
import Icon from 'Components/Icon';
import Link from 'Components/Link/Link';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import TagList from 'Components/TagList';
import { icons, kinds } from 'Helpers/Props';
import { WriteAudioTagsType } from 'typings/TaggingProfile';
import translate from 'Utilities/String/translate';
import EditTaggingProfileModal from './EditTaggingProfileModal';
import writeAudioTagOptions from './writeAudioTagOptions';
import styles from './TaggingProfile.css';

interface TaggingProfileProps {
  id: number;
  name: string;
  tags: number[];
  writeAudioTags: WriteAudioTagsType;
  scrubAudioTags: boolean;
  embedCoverArt: boolean;
  skipHardlinkedFiles: boolean;
  tagList?: Tag[];
  isDragging: boolean;
  connectDragSource?: (node: React.ReactNode) => React.ReactNode;
  onConfirmDeleteTaggingProfile?: (id: number) => void;
}

function TaggingProfile(props: TaggingProfileProps) {
  const {
    id,
    name,
    tags,
    writeAudioTags,
    scrubAudioTags,
    embedCoverArt,
    skipHardlinkedFiles,
    tagList = [],
    isDragging,
    connectDragSource = (node: React.ReactNode) => node,
    onConfirmDeleteTaggingProfile,
  } = props;

  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);

  const onEditPress = useCallback(() => {
    setIsEditModalOpen(true);
  }, []);

  const onEditModalClose = useCallback(() => {
    setIsEditModalOpen(false);
  }, []);

  const onDeletePress = useCallback(() => {
    setIsEditModalOpen(false);
    setIsDeleteModalOpen(true);
  }, []);

  const onDeleteModalClose = useCallback(() => {
    setIsDeleteModalOpen(false);
  }, []);

  const onConfirmDelete = useCallback(() => {
    onConfirmDeleteTaggingProfile?.(id);
  }, [id, onConfirmDeleteTaggingProfile]);

  const writeModeLabel =
    writeAudioTagOptions.find((option) => option.key === writeAudioTags)
      ?.value ?? writeAudioTags;

  return (
    <div
      className={classNames(
        styles.taggingProfile,
        isDragging && styles.isDragging
      )}
    >
      <div className={styles.name}>{name}</div>

      <div className={styles.writeAudioTags} title={writeModeLabel}>
        {writeModeLabel}
      </div>

      <div className={styles.optionColumn}>
        {embedCoverArt ? translate('Yes') : translate('No')}
      </div>

      <div className={styles.optionColumn}>
        {scrubAudioTags ? translate('Yes') : translate('No')}
      </div>

      <div className={styles.optionColumn}>
        {skipHardlinkedFiles ? translate('Yes') : translate('No')}
      </div>

      <TagList className={styles.fillcolumn} tags={tags} tagList={tagList} />

      <div className={styles.actions}>
        <Link
          className={id === 1 ? styles.editButton : undefined}
          onPress={onEditPress}
        >
          <Icon name={icons.EDIT} />
        </Link>

        {id !== 1 &&
          connectDragSource(
            <div className={styles.dragHandle}>
              <Icon className={styles.dragIcon} name={icons.REORDER} />
            </div>
          )}
      </div>

      <EditTaggingProfileModal
        id={id}
        isOpen={isEditModalOpen}
        onModalClose={onEditModalClose}
        onDeleteTaggingProfilePress={onDeletePress}
      />

      <ConfirmModal
        isOpen={isDeleteModalOpen}
        kind={kinds.DANGER}
        title={translate('DeleteTaggingProfile')}
        message={translate('DeleteTaggingProfileMessageText')}
        confirmLabel={translate('Delete')}
        onConfirm={onConfirmDelete}
        onCancel={onDeleteModalClose}
      />
    </div>
  );
}

export default TaggingProfile;
