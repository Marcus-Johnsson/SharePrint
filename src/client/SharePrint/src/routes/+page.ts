import { ApiService } from '$lib/services/apiService';
import { unwrap } from '$lib/services/unwrap';
import { toSvelteError } from '$lib/services/toSvelteError';

export async function load({ fetch }) {
  try {
    const health = unwrap(await new ApiService(fetch).get<{ status: string }>('health'));
    return { health };
  } catch (e) {
    toSvelteError(e);
  }
}
