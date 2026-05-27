import { unwrap } from '$lib/services/unwrap.js';
import { listingService } from '$lib/services/listingService';
import { error } from '@sveltejs/kit';

export const load = async ({ params }) => {
    try {
        const listing = unwrap(await listingService.detail(params.id));
        return { listing };
    } catch (e) {
        throw error(404, 'Listing not found');
    }
};