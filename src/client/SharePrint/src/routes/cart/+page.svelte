<script lang="ts">
  type CartItem = { id: string; title: string; price: number };
  const items: CartItem[] = [];
  const total = $derived(items.reduce((sum, i) => sum + i.price, 0));
</script>

<h1>Shopping cart</h1>

{#if items.length === 0}
  <p>Cart is empty.</p>
{:else}
  <ul class="list">
    {#each items as i (i.id)}
      <li>
        <span>{i.title}</span>
        <span>{i.price.toFixed(2)}</span>
      </li>
    {/each}
  </ul>
  <p class="total">Total: {total.toFixed(2)}</p>
  <button type="button" class="primary">Checkout</button>
{/if}

<style>
  .list {
    list-style: none;
    padding: 0;
    display: grid;
    gap: 0.4rem;
    max-width: 480px;
  }
  .list li {
    display: flex;
    justify-content: space-between;
    padding: var(--space-2) 0.7rem;
    border: 1px solid var(--color-border);
    border-radius: var(--radius-sm);
    background: var(--color-surface);
  }
  .total {
    font-weight: 600;
  }
</style>
