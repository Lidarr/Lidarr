import React, { useCallback } from 'react';
import Link from 'Components/Link/Link';
import translate from 'Utilities/String/translate';
import styles from './SelectDownloadClientRow.css';

interface SelectDownloadClientRowProps {
  id: number;
  name: string;
  priority: number;
  onDownloadClientSelect(downloadClientId: number): unknown;
}

function SelectDownloadClientRow(props: SelectDownloadClientRowProps) {
  const { id, name, priority, onDownloadClientSelect } = props;

  const onDownloadClientSelectWrapper = useCallback(() => {
    onDownloadClientSelect(id);
  }, [id, onDownloadClientSelect]);

  return (
    <Link
      className={styles.downloadClient}
      component="div"
      onPress={onDownloadClientSelectWrapper}
    >
      <div>{name}</div>
      <div>{translate('PrioritySettings', { priority })}</div>
    </Link>
  );
}

export default SelectDownloadClientRow;
