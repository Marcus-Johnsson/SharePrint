<script lang="ts">
    import { onMount } from 'svelte';
    import { goto } from '$app/navigation';
    import { cart } from '$lib/stores/cartStore.svelte';

    type OrderItem = {
        id: string;
        listingId: string;
        listingTitle: string;
        unitPrice: number;
        downloadPath: boolean;
        printPath: boolean;
        downloadsRemaining: number | null;
    };
    type Order = {
        id: string;
        status: string;
        totalPrice: number;
        currency: string;
        createdAt: string;
        items: OrderItem[];
    };

    let order = $state<Order | null>(null);
    let error = $state('');
    let loading = $state(true);

    onMount(async () => {
        const params = new URLSearchParams(window.location.search);
        const piId = params.get('payment_intent');
        const status = params.get('redirect_status');

        if (!piId) {
            error = 'Missing payment information.';
            loading = false;
            return;
        }
        if (status && status !== 'succeeded') {
            error = `Payment ${status}.`;
            loading = false;
            return;
        }

        // 1. Confirm: flip Order.Status=Paid + issue grants. Idempotent.
        const confirmRes = await fetch('/api/orders/confirm', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({ paymentIntentId: piId })
        });
        if (!confirmRes.ok) {
            error = `Order confirmation failed (${confirmRes.status}).`;
            loading = false;
            return;
        }
        const { orderId } = await confirmRes.json();

        // 2. Fetch order details for display.
        const orderRes = await fetch(`/api/orders/${orderId}`, { credentials: 'include' });
        if (!orderRes.ok) {
            error = `Could not load order (${orderRes.status}).`;
            loading = false;
            return;
        }
        order = await orderRes.json();
        loading = false;

        // 3. Cart is done.
        cart.clear();
    });

    async function download(item: OrderItem) {
        if (!order) return;
        // Open in a new tab so server can stream as attachment.
        window.location.href = `/api/orders/${order.id}/items/${item.id}/download`;
    }
</script>

<div class="success">
    {#if loading}
        <p>Finalizing your order…</p>
    {:else if error}
        <h1>Something went wrong</h1>
        <p class="err">{error}</p>
        <button onclick={() => goto('/')}>Back to home</button>
    {:else if order}
        <h1>Thanks for your purchase!</h1>
        <p>Order <code>{order.id}</code> · Total {order.totalPrice.toFixed(2)} {order.currency}</p>

        <section>
            <h2>Downloads</h2>
            {#if order.items.some(i => i.downloadPath)}
                <ul class="list">
                    {#each order.items.filter(i => i.downloadPath) as item (item.id)}
                        <li>
                            <span class="title">{item.listingTitle}</span>
                            <span class="meta">
                                {item.downloadsRemaining ?? 0} download{(item.downloadsRemaining ?? 0) === 1 ? '' : 's'} left
                            </span>
                            <button
                                onclick={() => download(item)}
                                disabled={(item.downloadsRemaining ?? 0) <= 0}
                            >Download</button>
                        </li>
                    {/each}
                </ul>
            {:else}
                <p class="muted">No downloads in this order.</p>
            {/if}
        </section>

        {#if order.items.some(i => i.printPath)}
            <section>
                <h2>Print orders</h2>
                <p class="muted">Print fulfillment is handled separately. We'll contact you when ready.</p>
                <ul class="list">
                    {#each order.items.filter(i => i.printPath) as item (item.id)}
                        <li>
                            <span class="title">{item.listingTitle}</span>
                            <span class="meta">Print</span>
                        </li>
                    {/each}
                </ul>
            </section>
        {/if}

        <button class="home" onclick={() => goto('/')}>Back to home</button>
    {/if}
</div>

<style>
.success {
    max-width: 640px;
    margin: 2rem auto;
    padding: var(--space-5);
    background: var(--color-surface);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-md);
}
h1 { margin-top: 0; }
section { margin-top: var(--space-4); }
.list {
    list-style: none;
    padding: 0;
    margin: 0;
    display: flex;
    flex-direction: column;
    gap: var(--space-2);
}
.list li {
    display: grid;
    grid-template-columns: 1fr auto auto;
    align-items: center;
    gap: var(--space-3);
    padding: var(--space-2) var(--space-3);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-sm);
}
.title {
    font-weight: 600;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}
.meta { color: var(--color-muted); font-size: 0.85rem; white-space: nowrap; }
.muted { color: var(--color-muted); font-style: italic; }
.err { color: var(--color-danger); }
code { background: var(--color-bg); padding: 0.1rem 0.3rem; border-radius: var(--radius-sm); }
.home { margin-top: var(--space-4); }
</style>
