<script lang="ts">
import { goto } from '$app/navigation';
import ListingDetail from '$lib/components/ListingDetail.svelte';
import { isSeller } from '$lib/stores/auth.svelte';

let isSellerRole = $derived(isSeller());

function onSaved(id: string | null) {
    if (id) goto(`/userPage/listings/${id}`);
}
</script>

{#if isSellerRole === false}
    <p>Endast säljare kan skapa annonser.</p>
{:else}
    <ListingDetail listingId={null} {onSaved} />
{/if}
