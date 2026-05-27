<script lang="ts">
  import { register } from '$lib/services/auth.svelte';

  let email = $state('');
  let username = $state('');
  let password = $state('');

  function handleSubmit() {
    const paylod  = { email, username, password };
    register(paylod);;
  }
</script>

<h1>Register</h1>

<form class="form" onsubmit={(e) => e.preventDefault()}>
  <label>
    Email
    <input type="email" bind:value={email} autocomplete="email" />
  </label>
  <label>
    username
    <input type="text" bind:value={username} />
  </label>
  <label>
    Password
    <input type="password" bind:value={password} autocomplete="new-password" />
  </label>
  <button type="submit" class="primary" onclick={handleSubmit}>Create account</button>
  <p class="alt">Have account? <a href="/login">Sign in</a></p>
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
