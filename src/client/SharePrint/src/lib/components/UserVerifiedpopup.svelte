<script lang=ts>
    import { auth,isSeller } from '$lib/services/auth.svelte';

    let showPopup = $state(false); // Store based to enable popup across different pages, 
    let sellerRole = $state(isSeller.call(auth.Roles));

</script>

<div>
    {#if auth.isAuthenticated === true && showPopup === true && sellerRole === false}
        <div class="popup">
            <p>Registera ett bank kort för att bli veriferad.</p>
            <button class="btn" onclick={() => showPopup = false}>Stäng</button>
        </div>
    
    {/if}
</div>

