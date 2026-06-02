<script lang="ts">
  const sections = [
    { href: '/userPage/create-listing', label: 'Skapa annons' },
    { href: '/userPage/account', label: 'Kontoinformation' },
    { href: '/userPage/receipts', label: 'Kvitton' },
    { href: '/userPage/downloads', label: 'Nedladdningar' }
  ];
</script>

<h1>Mina sidor</h1>
<p>Välj en sektion.</p>
<ul class="list">
  {#each sections as s (s.href)}
    <li><a href={s.href}>{s.label}</a></li>
  {/each}
</ul>

<style>
  .list {
    list-style: none;
    padding: 0;
    display: grid;
    gap: var(--space-2);
    max-width: 320px;
  }
  .list a {
    display: block;
    padding: 0.6rem 0.9rem;
    border: 1px solid var(--color-border);
    border-radius: var(--radius-sm);
    text-decoration: none;
    color: var(--color-text);
    background: var(--color-surface);
  }
  .list a:hover {
    border-color: var(--color-accent);
    color: var(--color-accent);
  }
</style>
