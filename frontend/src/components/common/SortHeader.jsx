// A clickable table heading that shows an arrow for the current sort
export default function SortHeader({ label, column, sort, onSort }) {
  const active = sort.by === column;
  const arrow = active ? (sort.descending ? ' \u25BC' : ' \u25B2') : '';

  return (
    <th scope="col" style={{ cursor: 'pointer', whiteSpace: 'nowrap' }} onClick={() => onSort(column)}>
      {label}
      {arrow}
    </th>
  );
}
