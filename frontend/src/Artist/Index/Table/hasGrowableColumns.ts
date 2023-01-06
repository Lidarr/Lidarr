import Column from 'Components/Table/Column';

const growableColumns = ['qualityProfileId', 'path', 'tags'];

export default function hasGrowableColumns(columns: Column[]) {
  return columns.some((column) => {
    const { name, isVisible } = column;

    return growableColumns.includes(name) && isVisible;
  });
}
