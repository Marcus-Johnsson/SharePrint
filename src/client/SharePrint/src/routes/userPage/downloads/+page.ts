import { unwrap } from '$lib/services/unwrap';
import { orderService, type OrderDetail } from '$lib/services/orderService';


export const load = async () => {
    const res = await orderService.availableDownloads();
    if (res && 'code' in res) return { downloads: [], error: res.message };
    return { downloads: res ?? [] };
};