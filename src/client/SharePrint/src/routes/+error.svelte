<script lang="ts">
  import { page } from '$app/state';
</script>

{#if page.status === 404}
  <h1>Hittades inte</h1>
  <p>Resursen saknas.</p>
{:else if page.status === 401}
  <h1>Inloggning krävs</h1>
  <a href="/login">Logga in</a>
{:else if page.status === 502}
  <h1>Backend nere</h1>
  <p>Försök igen strax.</p>
{:else if page.status === 500}
  <h1>Serverfel</h1>
  <p>{page.error?.message}</p>
{:else}
  <h1>{page.status}</h1>
  <p>{page.error?.message}</p>
{/if}