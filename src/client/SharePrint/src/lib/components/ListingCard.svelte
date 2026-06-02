<script lang="ts">
    import type { ListingSummary } from '$lib/services/listingService';
    import { cart } from '$lib/stores/cartStore.svelte';
    import { auth } from '$lib/services/auth.svelte';

    let { listing, preview = false }:
        { listing: ListingSummary, preview?: boolean } = $props();

    const isOwnListing = $derived(listing.sellerUsername === auth.Username);
</script>

<article class="card">
    <svelte:element this={preview ? 'div' : 'a'}
        href={preview ? undefined : `/market/${listing.id}`}
        data-sveltekit-preload-data={preview ? undefined : 'hover'}>
        <div class="same-row">
            <h3>{listing.title}</h3>
            <div class="flags">
                {#if listing.downloadAble}<span class="flag">📥</span>{/if}
                {#if listing.printAble}<span class="flag">🖨️</span>{/if}
            </div>
        </div>
        <img src={listing.marketPictureLocation} alt={listing.title} loading="lazy" />
        <div>
            <p>{listing.descriptionPreview}</p>
        </div>
        <span class="price">{listing.price.toFixed(2)} SEK</span>
        <div class="same-row">
            <small class="seller">Säljs av <span class="sellerName">{listing.sellerUsername}</span></small>
        </div>
    </svelte:element>
    {#if !preview}
        <button class="btn" type="button"
            disabled={isOwnListing}
            onclick={() => cart.addListing(listing)}>🛒</button>
    {/if}
</article>
<style>
    .same-row {
        display: flex;
        justify-content: space-between;
        align-items: center;
    }
    .card {
        position: relative;
        border: 1px solid var(--color-border);
        border-radius: var(--radius-md);
        background: var(--color-surface);
        transition: transform .15s, box-shadow .15s, border-color .15s;
    }
    .card:hover {
        transform: translateY(-2px);
        box-shadow: var(--shadow-menu);
        border-color: var(--color-accent);
    }
    .card > a,
    .card > div {
        display: block;
        padding: var(--space-4);
        padding-right: calc(var(--space-4) + 2.5rem);
        color: var(--color-text);
        text-decoration: none;
    }
    .same-row .sellerName {
        color: var(--color-accent);
        font-size: 1rem;
    }
    .card img {
        width: 100%;
        aspect-ratio: 4/3;
        object-fit: cover;
        border-radius: var(--radius-sm);
        background: var(--color-bg);
    }
    .card h3 {
        margin: var(--space-2) 0 var(--space-1);
    }
    .price {
        font-weight: 600;
        color: var(--color-text);
    }
    .btn {
        position: absolute;
        bottom: var(--space-3);
        right: var(--space-3);
        font-size: 1.1rem;
        color: var(--color-accent);
        font-weight: 500;
        background: var(--color-surface);
        border: 1px solid var(--color-border);
        border-radius: var(--radius-sm);
        padding: 0.35rem 0.6rem;
        cursor: pointer;
        transition: background 120ms, border-color 120ms;
    }
    .btn:hover:not(:disabled) {
        background: var(--color-bg);
        border-color: var(--color-accent);
    }
    .btn:disabled {
        opacity: 0.4;
        cursor: not-allowed;
    }
    .flags {
        display: flex;
        gap: var(--space-1);
        margin-top: var(--space-2);
        min-height: 1.5rem;
    }
    .flag {
        font-size: 1rem;
        padding: 0.1rem 0.1rem;
        border-radius: 10px;
        background: var(--color-bg);
        color: var(--color-muted);
        border: 1px solid var(--color-border);
    }
    .seller {
        color: var(--color-muted);
        display: block;
        margin-top: var(--space-2);
        font-size: .8rem;
    }
</style>
