<script lang="ts">
  import { auth, isSeller } from '$lib/services/auth.svelte';
  import { ui } from '$lib/services/auth.svelte';

  let sellerRole = $state(isSeller.call(auth.Roles));
</script>

<h1>Account information</h1>

<dl class="info">
  <dt>Display name</dt>
  <dd>{auth.Username ?? '—'}</dd>
  <dt>Email</dt>
  <dd>{auth.Email ?? '—'}</dd>
  <dt>Veriferad användare</dt>
  <dd>{sellerRole ?? '—'} 
    {#if sellerRole === false}
      <button class="verified" onclick={() => ui.showVerifyPopup = true}>verifiera</button>
    {/if}
    </dd>
</dl>

<style>
  .info {
    display: grid;
    grid-template-columns: max-content 1fr;
    gap: 0.4rem var(--space-4);
    max-width: 480px;
    padding: var(--space-4);
    background: var(--color-surface);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-md);
  }
  dt {
    font-weight: 600;
    color: var(--color-muted);
  }
  dd {
    margin: 0;
  }
  .verified {
    margin-left: var(--space-2);
    padding: 0.05rem 0.3rem;
    font-size: 0.9rem;
    color: var(--color-accent);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-sm);
    background: var(--color-bg);
  }
  .verified:hover {
    background: var(--color-accent);
    color: var(--color-surface);
    border-color: var(--color-accent);
  }
</style>
