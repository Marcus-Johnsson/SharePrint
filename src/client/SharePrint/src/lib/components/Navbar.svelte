<script lang="ts">
  import { page } from '$app/state';
  import { auth, logout } from '$lib/services/auth.svelte';

  type DropItem = { href: string; label: string };

  const marketItems: DropItem[] = [
    { href: '/market', label: 'Catalog' },
    { href: '/market/featured', label: 'Featured' },
    { href: '/market/new', label: 'Nya produkter' },
    { href: '/market/popular', label: 'Populära' }
  ];

  const userItems: DropItem[] = [
    { href: '/userPage', label: 'Mina sidor' },
    { href: '/userPage/create-listing', label: 'Skapa en försälj' },
    { href: '/userPage/account', label: 'Konto information' },
    { href: '/userPage/receipts', label: 'Kvitton' },
    { href: '/userPage/downloads', label: 'Mina nedladdningar' }
  ];

  let openMenu = $state<'market' | 'user' | null>(null);

  function toggle(menu: 'market' | 'user') {
    openMenu = openMenu === menu ? null : menu;
  }

  function close() {
    openMenu = null;
  }

  function onWindowClick(e: MouseEvent) {
    const target = e.target as HTMLElement;
    if (!target.closest('.dropdown')) close();
  }

  $effect(() => {
    window.addEventListener('click', onWindowClick);
    return () => window.removeEventListener('click', onWindowClick);
  });

  // close on route change
  $effect(() => {
    page.url.pathname;
    close();
  });
</script>

<nav class="navbar">
  <a class="brand" href="/">SharePrint</a>

  <div class="links">
    <div class="dropdown">
      <button class="trigger" onclick={() => toggle('market')} aria-expanded={openMenu === 'market'}>
        Market <span class="caret">{openMenu === 'market' ? '^' : 'v'}</span>
      </button>
      {#if openMenu === 'market'}
        <ul class="menu">
          {#each marketItems as item (item.href)}
            <li><a href={item.href}>{item.label}</a></li>
          {/each}
        </ul>
      {/if}
    </div>
  </div>
  <div class="userData">
    {#if auth.isAuthenticated}
      <span>Auth: {auth.isAuthenticated} | Username: {auth.Username ?? 'ingen data'} | Email: {auth.Email}</span>
    {/if}
  </div>
  {#if auth.isAuthenticated}
  <div class="dropdown">
    <button class="trigger" onclick={() => toggle('user')} aria-expanded={openMenu === 'user'}>
      User <span class="caret">{openMenu === 'user' ? '^' : 'v'}</span>
    </button>
    {#if openMenu === 'user'}
    <ul class="menu">
      {#each userItems as item (item.href)}
      <li><a href={item.href}>{item.label}</a></li>
      {/each}
      <li><button onclick={logout}>logga ut</button></li>
    </ul>
    {/if}
  </div>
  
  {:else}
    <a class="cart" href="/login" aria-label="Login">
    Logga in
  </a>
    {/if}
  <a class="cart" href="/cart" aria-label="Shopping cart">
    Cart
    <span class="badge">0</span>
  </a>
</nav>

<style>
  .navbar {
    display: flex;
    align-items: center;
    gap: 1.5rem;
    padding: var(--space-3) var(--space-5);
    border-bottom: 1px solid var(--color-border);
    background: var(--color-surface);
  }
  .brand {
    font-weight: 700;
    text-decoration: none;
    color: var(--color-text);
  }
  .links {
    display: flex;
    gap: var(--space-2);
    flex: 1;
  }
  .dropdown {
    position: relative;
  }
  .trigger {
    background: none;
    border: none;
    padding: 0.4rem 0.7rem;
  }
  .trigger:hover {
    background: var(--color-bg);
  }
  .caret {
    font-size: 0.7em;
    margin-left: var(--space-1);
  }
  .menu {
    position: absolute;
    top: 100%;
    left: 0;
    margin: var(--space-1) 0 0;
    padding: var(--space-1) 0;
    list-style: none;
    background: var(--color-surface);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-sm);
    box-shadow: var(--shadow-menu);
    min-width: 180px;
    z-index: 10;
  }
  .menu li a {
    display: block;
    padding: var(--space-2) 0.9rem;
    text-decoration: none;
    color: var(--color-text);
  }
  .menu li a:hover {
    background: var(--color-bg);
    color: var(--color-accent);
  }
  .cart {
    text-decoration: none;
    color: var(--color-text);
    padding: 0.4rem 0.7rem;
    border: 1px solid var(--color-border);
    border-radius: var(--radius-sm);
    background: var(--color-surface);
  }
  .cart:hover {
    background: var(--color-bg);
  }
  .badge {
    display: inline-block;
    margin-left: 0.35rem;
    padding: 0.05rem 0.4rem;
    font-size: 0.75rem;
    background: var(--color-accent);
    color: #fff;
    border-radius: 999px;
  }
</style>
