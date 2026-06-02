<script lang="ts">
    import { orderService } from '$lib/services/orderService';
    import type { PageProps } from './$types';

    let { data }: PageProps = $props();

    let items = $derived(data.downloads);

  function download(orderId: string, orderItemId: string) {
    orderService.download(orderId, orderItemId);
  }
</script>

<h1>Nedladdningar</h1>
<p>Filer du kan ladda ner. Räknaren visar antal kvarvarande nedladdningar.</p>

<ul class="list">
  {#each items as item}
    <li class="row">
      <span class="title">{item.listingTitle}</span>
      <span class="count" class:empty={item.downloadsRemaining === 0}>
        {item.downloadsRemaining} kvar
      </span>
      <button type="button" class="primary" 
          onclick={() => download(item.orderId, item.orderItemId)}
           disabled={item.downloadsRemaining === 0}>
           Ladda ner</button>
    </li>
  {/each}
</ul>

<style>
  .list {
    list-style: none;
    padding: 0;
    display: grid;
    gap: var(--space-2);
  }
  .row {
    display: grid;
    grid-template-columns: 1fr max-content max-content max-content;
    align-items: center;
    gap: var(--space-3);
    padding: 0.6rem 0.8rem;
    border: 1px solid var(--color-border);
    border-radius: var(--radius-sm);
    background: var(--color-surface);
  }
  .title {
    font-weight: 500;
  }
  .count {
    font-size: 0.85rem;
    color: var(--color-muted);
  }
  .count.empty {
    color: var(--color-danger);
  }
</style>
