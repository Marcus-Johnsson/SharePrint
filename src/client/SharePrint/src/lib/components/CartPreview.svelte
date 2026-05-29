<script lang="ts">
    import { goto } from '$app/navigation';
    import { cart } from '$lib/stores/cartStore.svelte';

    let { showCart = false, onClose }: { showCart?: boolean; onClose?: () => void } = $props();

    function close() {
        onClose?.();
    }

    function proceedToCheckout() {
        close();
        goto('/cart');
    }
</script>

{#if showCart}
<div class="cart-preview" role="dialog" aria-label="Cart preview">
    <header class="head">
        <h2>Your Cart</h2>
        <button class="icon-btn" onclick={close} aria-label="Close cart">×</button>
    </header>

    <div class="items">
        {#if cart.products.length === 0}
            <p class="empty">Your cart is empty.</p>
        {:else}
            {#each cart.products as cartItem (cartItem.product.id)}
                <article class="item" title={cartItem.product.title}>
                    <span class="title">{cartItem.product.title}</span>
                    <span class="unit">{cartItem.product.price.toFixed(2)}</span>
                    <input
                        class="qty"
                        type="number"
                        min="0"
                        bind:value={cartItem.amount}
                        onchange={() => cart.setAmount(cartItem.product.id, cartItem.amount)}
                        aria-label="Amount"
                    />
                    <div class="stepper" role="group" aria-label="Quantity">
                        <button
                            class="step"
                            aria-label="Decrease"
                            onclick={() => cart.reduceAmount(cartItem.product.id)}
                        >−</button>
                        <button
                            class="step"
                            aria-label="Increase"
                            onclick={() => cart.addListing(cartItem.product)}
                        >+</button>
                    </div>
                    <span class="line-total">{(cartItem.product.price * cartItem.amount).toFixed(2)}</span>
                    <button
                        class="remove-btn"
                        aria-label="Remove item"
                        onclick={() => cart.remove(cartItem.product.id)}
                    >🗑️</button>
                </article>
            {/each}
        {/if}
    </div>

    <footer class="foot">
        <div class="total">
            <span>Total</span>
            <strong>{cart.total.toFixed(2)} SEK</strong>
        </div>
        <button
            class="checkout"
            onclick={proceedToCheckout}
            disabled={cart.products.length === 0}
        >Checkout</button>
    </footer>
</div>
{/if}

<style>
.cart-preview {
    position: fixed;
    top: 70px;
    right: var(--space-4);
    width: 420px;
    max-height: calc(100vh - 90px);
    display: flex;
    flex-direction: column;
    background: var(--color-surface);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-md);
    box-shadow: var(--shadow-menu), 0 12px 32px rgba(37, 52, 63, 0.18);
    z-index: 1000;
    overflow: hidden;
    animation: slide-in 180ms ease-out;
}

.head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: var(--space-3) var(--space-4);
    border-bottom: 1px solid var(--color-border);
    background: var(--color-bg);
}
.head h2 {
    margin: 0;
    font-size: 1rem;
    font-weight: 600;
}

.icon-btn {
    background: transparent;
    border: none;
    padding: 0;
    width: 28px;
    height: 28px;
    border-radius: var(--radius-sm);
    font-size: 1.25rem;
    line-height: 1;
    color: var(--color-muted);
}
.icon-btn:hover {
    background: var(--color-border);
    color: var(--color-text);
}

.items {
    flex: 1;
    overflow-y: auto;
    padding: var(--space-2) var(--space-3);
    display: flex;
    flex-direction: column;
}

.empty {
    margin: 0;
    padding: var(--space-4) 0;
    text-align: center;
    color: var(--color-muted);
}

.item {
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto auto auto auto auto;
    align-items: center;
    column-gap: var(--space-2);
    padding: var(--space-2) var(--space-1);
    border-bottom: 1px solid var(--color-border);
    font-size: 0.85rem;
}
.item:last-child {
    border-bottom: none;
}
.item:hover {
    background: var(--color-bg);
}

.title {
    font-weight: 600;
    color: var(--color-text);
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    min-width: 0;
}
.unit {
    color: var(--color-muted);
    font-variant-numeric: tabular-nums;
    white-space: nowrap;
    font-size: 0.8rem;
}

.stepper {
    display: inline-flex;
    border: 1px solid var(--color-border);
    border-radius: var(--radius-sm);
    overflow: hidden;
}
.step {
    width: 24px;
    height: 26px;
    background: var(--color-bg);
    border: none;
    border-radius: 0;
    color: var(--color-text);
    font-weight: 700;
    padding: 0;
    line-height: 1;
}
.step + .step {
    border-left: 1px solid var(--color-border);
}
.step:hover:not(:disabled) {
    background: var(--color-accent);
    color: #fff;
}
.qty {
    width: 42px;
    height: 26px;
    border: 1px solid var(--color-border);
    border-radius: var(--radius-sm);
    text-align: center;
    padding: 0;
    font-variant-numeric: tabular-nums;
    -moz-appearance: textfield;
}
.qty::-webkit-outer-spin-button,
.qty::-webkit-inner-spin-button {
    -webkit-appearance: none;
    margin: 0;
}

.line-total {
    font-weight: 600;
    color: var(--color-text);
    font-variant-numeric: tabular-nums;
    white-space: nowrap;
    min-width: 64px;
    text-align: right;
    font-size: 0.8rem;
}

.remove-btn {
    background: transparent;
    border: none;
    padding: 0;
    width: 24px;
    height: 24px;
    border-radius: var(--radius-sm);
    font-size: 0.9rem;
    line-height: 1;
    color: var(--color-muted);
    opacity: 0.6;
    transition: opacity 120ms ease, background 120ms ease;
}
.remove-btn:hover {
    opacity: 1;
    background: var(--color-border);
}

.foot {
    border-top: 1px solid var(--color-border);
    padding: var(--space-3) var(--space-4);
    background: var(--color-bg);
    display: flex;
    flex-direction: column;
    gap: var(--space-3);
}
.total {
    display: flex;
    justify-content: space-between;
    align-items: baseline;
    font-size: 0.95rem;
}
.total strong {
    font-size: 1.05rem;
    color: var(--color-text);
}

.checkout {
    width: 100%;
    padding: 0.6rem 1rem;
    background: var(--color-accent);
    border: 1px solid var(--color-accent);
    color: #fff;
    border-radius: var(--radius-sm);
    font-weight: 600;
}
.checkout:hover:not(:disabled) {
    background: var(--color-accent-hover);
    border-color: var(--color-accent-hover);
}
.checkout:disabled {
    opacity: 0.5;
    cursor: not-allowed;
}
</style>
