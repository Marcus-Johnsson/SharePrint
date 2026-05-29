<script lang="ts">
  
</script>

<h1>Downloads</h1>
<p>Files you can download. Counter shows remaining downloads.</p>

<ul class="list">
  {#each items as item (item.id)}
    <li class="row">
      <span class="title">{item.title}</span>
      <span class="count" class:empty={item.remaining === 0}>
        {item.remaining} left
      </span>
      <button type="button" class="primary" disabled={item.remaining === 0}>Download</button>
      <button type="button" disabled={item.remaining === 0}>Print</button>
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
