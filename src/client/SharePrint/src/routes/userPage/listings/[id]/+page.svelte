<script lang="ts">
import { page } from '$app/stores';
import ListingDetail from '$lib/components/ListingDetail.svelte';
import { isSeller } from '$lib/stores/auth.svelte';

let isSellerRole = $derived(isSeller());
let id = $derived($page.params.id ?? null);

function onSaved() {
    // stay on edit page; could toast "sparat" here
}
</script>

{#if isSellerRole === false}
    <p>Endast säljare kan redigera annonser.</p>
{:else}
    {#key id}
        <ListingDetail listingId={id} {onSaved} />
    {/key}
{/if}
