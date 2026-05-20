import type { AppError } from './apiService';

export function isAppError(x: unknown): x is AppError {
  return typeof x === 'object' && x !== null && 'code' in x;
}

export class AppErrorException extends Error {
  constructor(public appError: AppError) {
    super(appError.message);
    this.name = 'AppErrorException';
  }
}

export function unwrap<T>(res: T | null | AppError): T {
  if (isAppError(res)) throw new AppErrorException(res);
  if (res === null) throw new Error('Unexpected empty response');
  return res;
}