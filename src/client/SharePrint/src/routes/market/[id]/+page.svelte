<script lang="ts">
    import type { PageProps } from './$types';
    import type { ListingDetail } from '$lib/services/listingService';

    let { data }: PageProps = $props();

    let listing = data.listing as ListingDetail;
    let pics = $derived(listing.descriptionPictures ?? []);
    let activeIndex = $state(0);
    let primarySrc = $derived(pics[activeIndex]?.url ?? listing.marketPictureLocation);
</script>

<article class="detail">
    <div class="gallery">
        <img class="primary" src={primarySrc} alt={listing.title} />
        {#if pics.length > 1}
            <div class="thumbs">
                {#each pics as pic, i (pic.id)}
                    <button type="button" class:active={i === activeIndex} onclick={() => activeIndex = i}>
                        <img src={pic.url} alt="" loading="lazy" />
                    </button>
                {/each}
            </div>
        {/if}
        <div class="description">
          {listing.description}
        </div>
    </div>
    <section class="info">
        <h1>{listing.title}</h1>
        <p class="seller">Säljs av {listing.sellerUsername}</p>
        <p class="price">{listing.price.toFixed(2)} SEK</p>
        <p> Tags area: 🟩🟦🟫🟥</p>
        <div class="flags">
            {#if listing.downloadAble}<span class="flag">📥 Download</span>{/if}
            {#if listing.printAble}<span class="flag">🖨️ Print</span>{/if}
        </div>
        <div class="actions">
            <button type="button" class="primary">Add to cart</button>
        </div>
    </section>
</article>

<style>
    .description {
      margin-top: var(--space-4);
      border:solid 1px var(--color-border);
      padding: var(--space-4);
      border-radius: var(--radius-md);
      background: var(--color-surface);
      box-shadow: var(--shadow-menu);
      height: auto;
      width: 80%;
    }
    .detail {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 2rem;
    }
    .primary {
        width: 100%;
        aspect-ratio: 4/3;
        object-fit: cover;
        border-radius: var(--radius-md);
        background: var(--color-bg);
    }
    .thumbs {
        display: flex;
        gap: var(--space-2);
        margin-top: var(--space-2);
        flex-wrap: wrap;
    }
    .thumbs button {
        width: 64px;
        height: 64px;
        padding: 0;
        border: 2px solid transparent;
        background: none;
        cursor: pointer;
        border-radius: var(--radius-sm);
        overflow: hidden;
    }
    .thumbs button.active {
        border-color: var(--color-accent);
    }
    .thumbs img {
        width: 100%;
        height: 100%;
        object-fit: cover;
    }
    .seller {
        color: var(--color-muted);
        margin: 0;
    }
    .price {
        font-size: 1.5rem;
        font-weight: 700;
        color: var(--color-text);
    }
    .flags {
        display: flex;
        gap: var(--space-2);
        margin: var(--space-2) 0;
    }
    .flag {
        font-size: 0.8rem;
        padding: 0.2rem 0.55rem;
        border-radius: 999px;
        background: var(--color-bg);
        border: 1px solid var(--color-border);
        color: var(--color-muted);
    }

    .actions {
        margin-top: var(--space-4);
    }
    @media (max-width: 760px) {
        .detail { grid-template-columns: 1fr; }
    }
</style>
