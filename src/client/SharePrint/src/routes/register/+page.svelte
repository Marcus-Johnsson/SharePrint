<script lang="ts">
  import { register } from '$lib/stores/auth.svelte';

  let email = $state('');
  let username = $state('');
  let password = $state('');

  function handleSubmit() {
    const paylod  = { email, username, password };
    register(paylod);;
  }
</script>

<h1>Registrera</h1>

<form class="form" onsubmit={(e) => e.preventDefault()}>
  <label>
    E-post
    <input type="email" bind:value={email} autocomplete="email" />
  </label>
  <label>
    Användarnamn
    <input type="text" bind:value={username} />
  </label>
  <label>
    Lösenord
    <input type="password" bind:value={password} autocomplete="new-password" />
  </label>
  <button type="submit" class="primary" onclick={handleSubmit}>Skapa konto</button>
  <p class="alt">Har du redan ett konto? <a href="/login">Logga in</a></p>
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
