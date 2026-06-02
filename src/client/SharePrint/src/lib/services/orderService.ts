import { api } from './apiService';

export type OrderItem = {
    id: string;
    listingId: string;
    listingTitle: string;
    unitPrice: number;
    downloadPath: boolean;
    printPath: boolean;
    downloadsRemaining: number | null;
};

export type OrderDetail = {
    id: string;
    status: string;
    totalPrice: number;
    currency: string;
    createdAt: string;
    items: OrderItem[];
};

export type DownloadSummary = {
    orderId: string;
    orderItemId: string;
    listingTitle: string;
    purchasedAt: string;
    downloadsRemaining: number;
};

export type ConfirmResult = {
    orderId: string;
    status: string;
};

export type CreateIntentResult = {
    clientSecret: string;
    orderId: string;
};

export type CheckoutCartItem = {
    id: string;
    option: 'print' | 'download' | null;
};

export const orderService = {
    createIntent: (items: CheckoutCartItem[]) =>
        api.post<CreateIntentResult, { items: CheckoutCartItem[] }>('checkout/create-intent', { items }),

    confirm: (paymentIntentId: string) =>
        api.post<ConfirmResult, { paymentIntentId: string }>('orders/confirm', { paymentIntentId }),

    detail: (id: string) =>
        api.get<OrderDetail>(`orders/${id}`),

    availableDownloads: () =>
        api.get<DownloadSummary[]>('me/downloads'),

    download: (orderId: string, orderItemId: string) => {
        window.location.href = `/api/orders/${orderId}/items/${orderItemId}/download`;
    }
};
