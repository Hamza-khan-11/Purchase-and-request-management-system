export function formatDate(value) {
  if (!value) return '';
  return new Date(value).toLocaleDateString();
}

export function formatDateTime(value) {
  if (!value) return '';
  return new Date(value).toLocaleString();
}

// For values that are only a date (like "required by"): never shifted by time zone
export function formatDateOnly(value) {
  if (!value) return '';
  const [year, month, day] = String(value).slice(0, 10).split('-').map(Number);
  return new Date(year, month - 1, day).toLocaleDateString();
}

export function formatMoney(value) {
  return Number(value).toLocaleString(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });
}
