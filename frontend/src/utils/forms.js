// Turns the API error list { Email: ["..."] } into { email: "..." } for our forms
export function mapServerErrors(errors) {
  const result = {};
  if (!errors) return result;
  Object.entries(errors).forEach(([key, messages]) => {
    const field = key.charAt(0).toLowerCase() + key.slice(1);
    result[field] = Array.isArray(messages) ? messages[0] : String(messages);
  });
  return result;
}
