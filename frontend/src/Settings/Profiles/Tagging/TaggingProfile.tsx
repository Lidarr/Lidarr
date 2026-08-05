import classNames from 'classnames';
import React, { useCallback, useState } from 'react';
import { useSelector } from 'react-redux';
import AppState from 'App/State/AppState';
import { Tag } from 'App/State/TagsAppState';
import Icon from 'Components/Icon';
import Label from 'Components/Label';
import Link from 'Components/Link/Link';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import TagList from 'Components/TagList';
import { icons, kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import EditTaggingProfileModal from './EditTaggingProfileModal';
import styles from './TaggingProfile.css';

interface TaggingProfileProps {
  id: number;
  name: string;
  tags: number[];
  downloadClientIds: number[];
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
    downloadClientIds,
    tagList = [],
    isDragging,
    connectDragSource = (node: React.ReactNode) => node,
    onConfirmDeleteTaggingProfile,
  } = props;

  const downloadClients = useSelector(
    (state: AppState) => state.settings.downloadClients.items
  );

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

  return (
    <div
      className={classNames(
        styles.taggingProfile,
        isDragging && styles.isDragging
      )}
    >
      <div className={styles.name}>{name}</div>

      <div className={styles.fillcolumn}>
        {downloadClientIds.length ? (
          downloadClientIds.map((clientId) => {
            const downloadClient = downloadClients.find(
              (c) => c.id === clientId
            );

            return downloadClient ? (
              <Label key={clientId} kind={kinds.INFO}>
                {downloadClient.name}
              </Label>
            ) : null;
          })
        ) : (
          <Label kind={kinds.DEFAULT}>{translate('Any')}</Label>
        )}
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
