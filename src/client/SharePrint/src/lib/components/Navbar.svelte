<script lang="ts">
  import { page } from '$app/state';
  import { auth, logout } from '$lib/services/auth.svelte';
  import { cart } from '$lib/stores/cartStore.svelte';
  import CartPreview from "$lib/components/CartPreview.svelte";

  type DropItem = { href: string; label: string };

  const marketItems: DropItem[] = [
    { href: '/market', label: 'Katalog' },
    { href: '/market/featured', label: 'Utvalda' },
    { href: '/market/new', label: 'Nya produkter' },
    { href: '/market/popular', label: 'Populära' }
  ];

  const userItems: DropItem[] = [
    { href: '/userPage', label: 'Mina sidor' },
    { href: '/userPage/create-listing', label: 'Skapa annons' },
    { href: '/userPage/account', label: 'Kontoinformation' },
    { href: '/userPage/receipts', label: 'Kvitton' },
    { href: '/userPage/downloads', label: 'Mina nedladdningar' }
  ];
  let openMenu = $state<'market' | 'user' | 'cart' | null>(null);
  let cartItemCount = $derived(cart.products.length);

  function toggle(menu: 'market' | 'user' | 'cart') {
    openMenu = openMenu === menu ? null : menu;
  }

  function close() {
    openMenu = null;
  }

  function onWindowClick(e: MouseEvent) {
    const path = e.composedPath();
    for (const el of path) {
      if (el instanceof HTMLElement && el.classList.contains('dropdown')) return;
    }
    close();
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
        Marknad <span class="caret">{openMenu === 'market' ? '^' : 'v'}</span>
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
  {#if auth.isAuthenticated}
    <div class="dropdown user-dropdown">
      <button class="trigger user-trigger"
              onclick={() => toggle('user')}
              aria-expanded={openMenu === 'user'}
              aria-haspopup="menu">
        <span class="avatar" aria-hidden="true">
          {auth.Username?.[0]?.toUpperCase() ?? '?'}
        </span>
        <span class="username">{auth.Username ?? 'Konto'}</span>
        <span class="caret">{openMenu === 'user' ? '^' : 'v'}</span>
      </button>
      {#if openMenu === 'user'}
        <ul class="menu" role="menu">
          {#each userItems as item (item.href)}
            <li role="none">
              <a role="menuitem" href={item.href}>{item.label}</a>
            </li>
          {/each}
          <li class="separator" role="separator"></li>
          <li role="none">
            <button class="menu-action" role="menuitem" onclick={logout}>
              Logga ut
            </button>
          </li>
        </ul>
      {/if}
    </div>
  {:else}
    <a class="login-link" href="/login">Logga in</a>
  {/if}

    <div class="dropdown">
      <button onclick={() => toggle('cart')} class="cart" aria-label="Varukorg">
        Varukorg
        <span class="badge">{cartItemCount}</span>
      </button>
      <CartPreview showCart={openMenu === 'cart'} onClose={close}/>
    </div>
    
 
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
  .user-dropdown {
    margin-left: auto;
  }
  .user-trigger {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }
  .avatar {
    width: 28px;
    height: 28px;
    border-radius: 50%;
    background: var(--color-accent);
    color: #fff;
    display: grid;
    place-items: center;
    font-weight: 600;
    font-size: 0.85rem;
  }
  .username {
    font-weight: 500;
  }
  .menu .separator {
    height: 1px;
    background: var(--color-border);
    margin: var(--space-1) 0;
    list-style: none;
  }
  .menu-action {
    width: 100%;
    text-align: left;
    background: none;
    border: none;
    padding: var(--space-2) 0.9rem;
    color: var(--color-text);
    cursor: pointer;
    font: inherit;
  }
  .menu-action:hover {
    background: var(--color-bg);
    color: var(--color-accent);
  }
  .login-link {
    text-decoration: none;
    color: var(--color-text);
    padding: 0.4rem 0.7rem;
    border: 1px solid var(--color-border);
    border-radius: var(--radius-sm);
  }
  .login-link:hover {
    background: var(--color-bg);
  }
</style>
