<script lang="ts">
    import type { ListingSummary } from '$lib/services/listingService';
    let { listing }: { listing: ListingSummary } = $props();

</script>

<article class="card">
    <a href="/market/{listing.id}" data-sveltekit-preload-data="hover">
        <div class="same-row">
            <h3>{listing.title}</h3>
            <div class="flags">
                {#if listing.downloadAble}<span class="flag">📥</span>{/if} <!-- Switch to tags and move flags up to the right-->
                {#if listing.printAble}<span class="flag">🖨️</span>{/if}
            </div>
        </div>
        <img src={listing.marketPictureLocation} alt={listing.title} loading="lazy" />
        <div>
           <p>🟥🟫🟦🟩</p>
        </div>
        <span class="price">{listing.price.toFixed(2)} SEK</span>
        <div class="same-row">
            <small class="seller">Säljs av {listing.sellerUsername}</small>
            <button class="btn" type="button">🛒</button>
        </div>
    </a>
</article>
<style>
    .same-row {
        display: flex;
        justify-content: space-between;
        align-items: center;
    }
    .card {
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
    .card a {
        display: block;
        padding: var(--space-4);
        color: var(--color-text);
        text-decoration: none;
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
    .row {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-top: var(--space-2);
    }
    .price {
        font-weight: 600;
        color: var(--color-text);
    }
    .btn {
        font-size: .9rem;
        color: var(--color-accent);
        font-weight: 500;
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
