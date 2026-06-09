<script lang="ts">
    import { goto } from '$app/navigation';
    import { navigating } from '$app/state';
    import ListingCard from '$lib/components/ListingCard.svelte';

    let { data } = $props();

    const FILTER_LABELS = {
        None:       'Inget filter',
        Withdrawal: 'Utskrivning',
        Download:   'Nedladdning',
    } as const;
    type Filter = keyof typeof FILTER_LABELS;
    const filterOptions = Object.keys(FILTER_LABELS) as Filter[];
    const pageSizeOptions = [5, 30, 40];

    let currentPage = $state(data.page);
    let pageSize = $state(data.pageSize);
    let filter = $state<Filter>(data.filter as Filter);

    $effect(() => {
        currentPage = data.page;
        pageSize = data.pageSize;
        filter = data.filter as Filter;
    });

    let totalPages = $derived(data.totalPages);
    let hasNextPage = $derived(data.hasNextPage);
    let hasPreviousPage = $derived(data.hasPreviousPage);
    let isRefreshing = $derived(navigating.to !== null);

    function updateURL() {
        const params = new URLSearchParams();
        if (pageSize !== 5) params.set('pageSize', String(pageSize));
        if (filter !== 'None') params.set('filters', filter);
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
        currentPage = 1;
        updateURL();
    }
</script>

<h1>Katalog</h1>

{#if data.listings.length === 0}
    <p>Inga annonser än.</p>
{:else}
    <section class="grid">
        {#each data.listings as listing (listing.id)}
            <ListingCard {listing} href='market/{listing.id}'/>
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
    <div class="page-size-selector">
        <label for="filter-list">Filter</label>
    <select
        id="filter-list"
        bind:value={filter}
        onchange={() => { currentPage = 1; updateURL(); }}
    >
        {#each filterOptions as key}
            <option value={key}>{FILTER_LABELS[key]}</option>
        {/each}
    </select>
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