import React from 'react';
import Column from 'Components/Table/Column';
import VirtualTableHeader from 'Components/Table/VirtualTableHeader';
import VirtualTableHeaderCell from 'Components/Table/VirtualTableHeaderCell';
import styles from './SelectArtistModalTableHeader.css';

interface SelectArtistModalTableHeaderProps {
  columns: Column[];
}

function SelectArtistModalTableHeader(
  props: SelectArtistModalTableHeaderProps
) {
  const { columns } = props;

  return (
    <VirtualTableHeader>
      {columns.map((column) => {
        const { name, label, isVisible } = column;

        if (!isVisible) {
          return null;
        }

        return (
          <VirtualTableHeaderCell
            key={name}
            className={
              // eslint-disable-next-line @typescript-eslint/ban-ts-comment
              // @ts-ignore
              styles[name]
            }
            name={name}
          >
            {typeof label === 'function' ? label() : label}
          </VirtualTableHeaderCell>
        );
      })}
    </VirtualTableHeader>
  );
}

export default SelectArtistModalTableHeader;
