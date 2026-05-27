<script lang="ts">
    import ListingCard from '$lib/components/ListingCard.svelte';
    let { data } = $props();
</script>

<h1>Catalog</h1>

{#if data.listings.length === 0}
    <p>No listings yet.</p>
{:else}
    <section class="grid">
        {#each data.listings as listing (listing.id)}
            <ListingCard {listing} />
        {/each}
    </section>
{/if}

<style>
    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 1rem; }
</style>