<script lang="ts">
  const receipts: { id: string; date: string; total: string }[] = [];
</script>

<h1>Kvitton</h1>

{#if receipts.length === 0}
  <p>Inga kvitton än.</p>
{:else}
  <table class="table">
    <thead>
      <tr><th>ID</th><th>Datum</th><th>Summa</th></tr>
    </thead>
    <tbody>
      {#each receipts as r (r.id)}
        <tr><td>{r.id}</td><td>{r.date}</td><td>{r.total}</td></tr>
      {/each}
    </tbody>
  </table>
{/if}

<style>
  .table {
    border-collapse: collapse;
    width: 100%;
    max-width: 640px;
    background: var(--color-surface);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-md);
    overflow: hidden;
  }
  th, td {
    border-bottom: 1px solid var(--color-border);
    padding: var(--space-2) 0.7rem;
    text-align: left;
  }
  th {
    background: var(--color-bg);
    color: var(--color-muted);
    font-size: 0.85rem;
    text-transform: uppercase;
    letter-spacing: 0.04em;
  }
  tbody tr:last-child td {
    border-bottom: none;
  }
</style>
