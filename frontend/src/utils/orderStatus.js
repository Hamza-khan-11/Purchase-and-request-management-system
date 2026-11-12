// The statuses of a purchase order, with the label and colour used on screen
export const ORDER_STATUSES = [
  { value: 'Created', label: 'Created', badge: 'text-bg-secondary' },
  { value: 'Sent', label: 'Sent', badge: 'text-bg-primary' },
  { value: 'Delivered', label: 'Delivered', badge: 'text-bg-success' },
  { value: 'Cancelled', label: 'Cancelled', badge: 'text-bg-warning' },
];

export function orderStatusInfo(status) {
  return (
    ORDER_STATUSES.find((item) => item.value === status) || {
      value: status,
      label: status,
      badge: 'text-bg-light',
    }
  );
}
