/** Empty text to null, for optional fields. */
export function optional(text: string) {
  return text.trim() === '' ? null : text.trim();
}

export function optionalNumber(text: string) {
  return text.trim() === '' ? null : Number(text);
}

/** Escapes text to put it inside HTML (map popups). */
export const escapeHtml = (s: string) => s.replace(/[&<>"']/g, (c) => `&#${c.charCodeAt(0)};`);
