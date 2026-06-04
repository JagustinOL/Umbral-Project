import { captureAuthFromHash, getAuthSession } from '@/lib/auth/session';

export function getOperatorId(): string | null {
  captureAuthFromHash();

  const sessionUserId = getAuthSession()?.userId?.trim();
  if (sessionUserId && sessionUserId.length > 0) {
    return sessionUserId;
  }

  const value = process.env.NEXT_PUBLIC_OPERATOR_ID?.trim();
  return value && value.length > 0 ? value : null;
}
