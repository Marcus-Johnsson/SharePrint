<script lang="ts">
    import { goto } from '$app/navigation';
    import { navigating } from '$app/state';
    import ListingCard from '$lib/components/ListingCard.svelte';
    
    let { data } = $props();

    let currentPage = $state(1);
	let pageSize = $state(20);
    
    let search = $state('');
	const pageSizeOptions = [20, 30, 40];
	let totalPages = $derived(data.totalPages || 1);
	let totalCount = $derived(data.totalCount || 0);
	let hasNextPage = $derived(data.hasNextPage || false);
	let hasPreviousPage = $derived(data.hasPreviousPage || false);

    let isRefreshing = $derived(navigating.to !== null);
    
    function updateURL() {
		const params = new URLSearchParams();
		if (search) params.set('search', search);

		if (pageSize !== 20) params.set('pageSize', String(pageSize));

		if (currentPage > 1) params.set('page', String(currentPage));
		const url = params.toString() ? `/market?${params.toString()}` : '/market';
		goto(url, { replaceState: true, keepFocus: true, noScroll: true });
	}

	function goToPage(page: number) {
		currentPage = page;
		updateURL();
	}

    function changePageSize(size: number) {
        pageSize = size;
    }

</script>

<h1>Katalog</h1>

{#if data.listings.length === 0}
    <p>Inga annonser än.</p>
{:else}
    <section class="grid">
        {#each data.listings as listing (listing.id)}
            <ListingCard {listing} preview={false}/>
        {/each}
    </section>
    <div class="pagination">
    <div class="page-size-selector">
        <label for="pageSize-list">Visa</label>
        <select
            id="pageSize-list"
            bind:value={pageSize}
            onchange={() => changePageSize(pageSize)}
        >
            {#each pageSizeOptions as size}
                <option value={size}>{size}</option>
            {/each}
        </select>
        <span>per sida</span>
    </div>
        <div class="pagination-controls">
            <button
                onclick={() => goToPage(currentPage - 1)}
                disabled={!hasPreviousPage || isRefreshing}
            >
                Föregående
            </button>
            <span class="pagination-info">
                Sida {currentPage} av {totalPages}
            </span>
            <button
                onclick={() => goToPage(currentPage + 1)}
                disabled={!hasNextPage || isRefreshing}
            >
                Nästa
            </button>
        </div>
    </div>
{/if}
<style>
    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 1rem; }
</style>