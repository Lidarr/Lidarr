import React from 'react';
import styles from './VirtualTableRowCell.css';

interface VirtualTableRowCellProps extends React.HTMLAttributes<HTMLDivElement> {
  className?: string;
  children?: React.ReactNode;
}

function VirtualTableRowCell({
  className = styles.cell,
  children,
  ...otherProps
}: VirtualTableRowCellProps) {
  return (
    <div className={className} {...otherProps}>
      {children}
    </div>
  );
}

export default VirtualTableRowCell;