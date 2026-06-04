import { listingService } from '$lib/services/listingService';
import { unwrap } from '$lib/services/unwrap';
import { toSvelteError } from '$lib/services/toSvelteError';



export const load = async (event) => {
    try {

        const p = event.url.searchParams;
        const page = Math.max(1, parseInt(p.get('page') ?? '1', 10) || 1);
        const pageSize = Math.min(100, Math.max(1, parseInt(p.get('pageSize') ?? '5', 10) || 5));

        const result = unwrap(await listingService.catalog(page, pageSize));
  
    return {
        listings: result.items,
        page: result.page,
        totalPages: result.totalPages ?? 1,
        pageSize: result.pageSize,
        totalCount: result.totalCount ?? 0,
        hasNextPage: result.hasNextPage ?? false,
        hasPreviousPage: result.hasPreviousPage ?? false
    };
    } catch (e) {
        toSvelteError(e);
    }
};
