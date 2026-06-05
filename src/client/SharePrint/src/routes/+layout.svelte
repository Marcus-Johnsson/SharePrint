<script lang="ts">
  import '../styles/global.css';
  import { onMount } from 'svelte';
  import favicon from '$lib/assets/favicon.svg';
  import Navbar from '$lib/components/Navbar.svelte';
  import UserVerifiedPopup from '$lib/components/UserVerifiedpopup.svelte';
  import { bootstrapAuth } from '$lib/stores/auth.svelte';

  let { children } = $props();

  onMount(() => {
    bootstrapAuth();
  });
</script>

<svelte:head>
  <link rel="icon" href={favicon} />
</svelte:head>

<Navbar />

<main class="content">
  {@render children()}
</main>

<UserVerifiedPopup />

<style>
  .content {
    padding: var(--space-5);
    max-width: 1100px;
    margin: 0 auto;
  }
</style>
