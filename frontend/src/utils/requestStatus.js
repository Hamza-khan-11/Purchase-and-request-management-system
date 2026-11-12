// The statuses of a purchase request, with the label and colour used on screen
export const REQUEST_STATUSES = [
  { value: 'Draft', label: 'Draft', badge: 'text-bg-secondary' },
  { value: 'Submitted', label: 'Submitted', badge: 'text-bg-primary' },
  { value: 'Approved', label: 'Approved', badge: 'text-bg-success' },
  { value: 'Rejected', label: 'Rejected', badge: 'text-bg-danger' },
  { value: 'PoCreated', label: 'PO created', badge: 'text-bg-info' },
  { value: 'Cancelled', label: 'Cancelled', badge: 'text-bg-warning' },
];

export function statusInfo(status) {
  return (
    REQUEST_STATUSES.find((item) => item.value === status) || {
      value: status,
      label: status,
      badge: 'text-bg-light',
    }
  );
}
