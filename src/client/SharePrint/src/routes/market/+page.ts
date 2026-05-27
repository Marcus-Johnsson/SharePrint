import { listingService } from '$lib/services/listingService';
import { unwrap } from '$lib/services/unwrap';
import { toSvelteError } from '$lib/services/toSvelteError';

export const load = async () => {
    try {
        const listings = unwrap(await listingService.catalog());
        return { listings };
    } catch (e) {
        toSvelteError(e);
    }
};
