// Player-facing API errors come back in the UI language (the client sends Accept-Language,
// rama 23) as a plain-text body, or as `{ message }` / `{ error }`. Anything else — a
// validation problem-details object, a network error — gets the screen's own message.
export function apiErrorMessage(err: unknown, fallback: string): string {
  const body = (err as { error?: unknown } | null)?.error;
  if (typeof body === 'string' && body.trim()) return body;
  if (body && typeof body === 'object') {
    const { message, error } = body as { message?: unknown; error?: unknown };
    if (typeof message === 'string' && message.trim()) return message;
    if (typeof error === 'string' && error.trim()) return error;
  }
  return fallback;
}
