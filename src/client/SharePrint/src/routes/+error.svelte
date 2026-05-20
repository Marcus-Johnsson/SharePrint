<script lang="ts">
  import { page } from '$app/state';
</script>

{#if page.status === 404}
  <h1>Not found</h1>
  <p>Resource missing.</p>
{:else if page.status === 401}
  <h1>Login required</h1>
  <a href="/login">Sign in</a>
{:else if page.status === 502}
  <h1>Backend down</h1>
  <p>Try again soon.</p>
{:else if page.status === 500}
  <h1>Server error</h1>
  <p>{page.error?.message}</p>
{:else}
  <h1>{page.status}</h1>
  <p>{page.error?.message}</p>
{/if}