import { api } from '$lib/services/apiService';
import { unwrap } from '$lib/services/unwrap';
import { toSvelteError } from '$lib/services/toSvelteError';

export async function load() {
  try {
    const health = unwrap(await api.get<{ status: string }>('health'));
    return { health };
  } catch (e) {
    toSvelteError(e);
  }
}
