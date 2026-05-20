import { error } from '@sveltejs/kit';
import { AppErrorException } from './unwrap';

const codeToStatus: Record<string, number> = {
  NOT_FOUND: 404,
  AUTH_ERROR: 401,
  NETWORK_ERROR: 502,
  API_ERROR: 500
};

export function toSvelteError(e: unknown): never {
  if (e instanceof AppErrorException) {
    const status = e.appError.status ?? codeToStatus[e.appError.code] ?? 500;
    throw error(status, e.message);
  }
  throw error(500, e instanceof Error ? e.message : 'Unknown error');
}
