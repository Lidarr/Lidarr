import React from 'react';
import styles from './TableRow.css';

interface TableRowProps extends React.HTMLAttributes<HTMLTableRowElement> {
  className?: string;
  children?: React.ReactNode;
}

function TableRow({
  className = styles.row,
  children,
  ...otherProps
}: TableRowProps) {
  return (
    <tr className={className} {...otherProps}>
      {children}
    </tr>
  );
}

export default TableRow;
