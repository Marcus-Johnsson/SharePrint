<script lang="ts">
  import '../styles/global.css';
  import favicon from '$lib/assets/favicon.svg';
  import Navbar from '$lib/components/Navbar.svelte';

  let { children } = $props();
</script>

<svelte:head>
  <link rel="icon" href={favicon} />
</svelte:head>

<Navbar />

<main class="content">
  {@render children()}
</main>

<style>
  .content {
    padding: var(--space-5);
    max-width: 1100px;
    margin: 0 auto;
  }
</style>
