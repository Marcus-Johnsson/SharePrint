<script lang="ts">
  import { login } from '$lib/stores/auth.svelte';

  // prefill for easier testing, remove in production #WhatCouldGoWrong
  let email = $state('test@test.com');
  let password = $state('Test123!');

  function handleSubmit() {
    const payload = { email, password };
    login(payload);
  }
</script>

<h1>Logga in</h1>

<form class="form" onsubmit={(e) => e.preventDefault()}>
  <label>
    E-post
    <input type="email" bind:value={email} autocomplete="email" />
  </label>
  <label>
    Lösenord
    <input type="password" bind:value={password} autocomplete="current-password" />
  </label>
  <button type="submit" class="primary" onclick={handleSubmit}>Logga in</button>
  <p class="alt">Inget konto? <a href="/register">Registrera</a></p>
</form>

<style>
  .form {
    display: grid;
    gap: var(--space-3);
    max-width: 360px;
  }
  label {
    display: grid;
    gap: 0.3rem;
    font-size: 0.9rem;
  }
  .form :global(button) {
    justify-self: start;
  }
  .alt {
    font-size: 0.85rem;
    color: var(--color-muted);
  }
  .alt a {
    color: var(--color-accent);
  }
</style>
