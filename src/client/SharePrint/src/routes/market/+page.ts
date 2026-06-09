import { listingService } from '$lib/services/listingService';
import { unwrap } from '$lib/services/unwrap';
import { toSvelteError } from '$lib/services/toSvelteError';

const VALID_FILTERS = ['None', 'Withdrawal', 'Download'] as const;
type FilterWire = typeof VALID_FILTERS[number];

export const load = async (event) => {
    try {
        const p = event.url.searchParams;
        const page = Math.max(1, parseInt(p.get('page') ?? '1', 10) || 1);
        const pageSize = Math.min(100, Math.max(1, parseInt(p.get('pageSize') ?? '5', 10) || 5));

        const rawFilter = p.get('filters') ?? 'None';
        const filter: FilterWire = (VALID_FILTERS as readonly string[]).includes(rawFilter)
            ? (rawFilter as FilterWire)
            : 'None';

        const result = unwrap(await listingService.catalog(page, pageSize, filter));

        return {
            listings: result.items,
            page: result.page,
            filter,
            pageSize: result.pageSize,
            totalPages: result.totalPages ?? 1,
            totalCount: result.totalCount ?? 0,
            hasNextPage: result.hasNextPage ?? false,
            hasPreviousPage: result.hasPreviousPage ?? false,
        };
    } catch (e) {
        toSvelteError(e);
    }
};
