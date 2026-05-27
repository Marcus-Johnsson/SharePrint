<script lang="ts">
  let title = $state('');
  let price = $state('');
  let description = $state('');
</script>

<h1>Create listing</h1>

<form class="form" onsubmit={(e) => e.preventDefault()}>
  <label>
    Title
    <input type="text" bind:value={title} />
  </label>
  <label>
    Price
    <input type="number" bind:value={price} min="0" step="0.01" />
  </label>
  <label>
    Description
    <textarea bind:value={description} rows="4"></textarea>
  </label>
  <label>
    File
    <input type="file" />
  </label>
  <button type="submit" class="primary">Publish</button>
</form>

<style>
  .form {
    display: grid;
    gap: var(--space-3);
    max-width: 480px;
  }
  label {
    display: grid;
    gap: 0.3rem;
    font-size: 0.9rem;
  }
  .form :global(button) {
    justify-self: start;
  }
</style>
