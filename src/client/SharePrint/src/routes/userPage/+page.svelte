<script lang="ts">
  const sections = [
    { href: '/userPage/create-listing', label: 'Create listing' },
    { href: '/userPage/account', label: 'Account information' },
    { href: '/userPage/receipts', label: 'Receipts' },
    { href: '/userPage/downloads', label: 'Downloads' }
  ];
</script>

<h1>User page</h1>
<p>Pick a section.</p>
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
