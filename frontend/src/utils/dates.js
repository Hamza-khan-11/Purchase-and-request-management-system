// Date helpers that work on "YYYY-MM-DD" text, in UTC, the same way the server does

export function utcToday() {
  return new Date().toISOString().slice(0, 10);
}

export function addDays(dateString, days) {
  const [year, month, day] = String(dateString).slice(0, 10).split('-').map(Number);
  return new Date(Date.UTC(year, month - 1, day + days)).toISOString().slice(0, 10);
}

// How many days from "from" to "to" (negative if "to" is earlier)
export function daysBetween(from, to) {
  const [y1, m1, d1] = String(from).slice(0, 10).split('-').map(Number);
  const [y2, m2, d2] = String(to).slice(0, 10).split('-').map(Number);
  return Math.round((Date.UTC(y2, m2 - 1, d2) - Date.UTC(y1, m1 - 1, d1)) / 86400000);
}
