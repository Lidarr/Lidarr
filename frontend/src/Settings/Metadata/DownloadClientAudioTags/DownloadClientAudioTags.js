import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import Icon from 'Components/Icon';
import Label from 'Components/Label';
import Link from 'Components/Link/Link';
import Scroller from 'Components/Scroller/Scroller';
import { icons, kinds } from 'Helpers/Props';
import { fetchDownloadClients } from 'Store/Actions/settingsActions';
import translate from 'Utilities/String/translate';
import EditDownloadClientAudioTagsModal from './EditDownloadClientAudioTagsModal';
import styles from './DownloadClientAudioTags.css';

function DownloadClientAudioTags() {
  const dispatch = useDispatch();
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);

  const items = useSelector((state) => state.settings.downloadClients.items);

  const enabled = useMemo(() => {
    return items
      .filter((item) => item.writeAudioTags)
      .sort((a, b) => a.name.localeCompare(b.name));
  }, [items]);

  useEffect(() => {
    dispatch(fetchDownloadClients());
  }, [dispatch]);

  const onEditPress = useCallback(() => {
    setIsEditModalOpen(true);
  }, []);

  const onModalClose = useCallback(() => {
    setIsEditModalOpen(false);
  }, []);

  return (
    <Scroller className={styles.horizontalScroll}>
      <div className={styles.downloadClientsHeader}>
        <div className={styles.headerName}>
          {translate('Name')}
        </div>

        <div className={styles.headerFillcolumn}>
          {translate('DownloadClients')}
        </div>

        <div className={styles.headerActions} />
      </div>

      <div className={styles.downloadClients}>
        <div className={styles.downloadClient}>
          <div className={styles.name}>
            {translate('DownloadClient')}
          </div>

          <div className={styles.fillcolumn}>
            {
              enabled.length ?
                enabled.map((item) => {
                  return (
                    <Label
                      key={item.id}
                      kind={kinds.INFO}
                    >
                      {item.name}
                    </Label>
                  );
                }) :
                <Label kind={kinds.DISABLED}>
                  {translate('None')}
                </Label>
            }
          </div>

          <div className={styles.actions}>
            <Link
              className={styles.editButton}
              onPress={onEditPress}
            >
              <Icon name={icons.EDIT} />
            </Link>
          </div>
        </div>
      </div>

      <EditDownloadClientAudioTagsModal
        isOpen={isEditModalOpen}
        onModalClose={onModalClose}
      />
    </Scroller>
  );
}

export default DownloadClientAudioTags;
