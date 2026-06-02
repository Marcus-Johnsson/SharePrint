<script lang="ts">
  import type { ListinCreation } from "$lib/services/listingService";
  import { listingService } from "$lib/services/listingService";
  import { isSeller } from "$lib/services/auth.svelte";
  import ListingCard from "$lib/components/ListingCard.svelte";

  let title = $state('');
  let price = $state('');
  let description = $state('');
  let thumbList = $state<FileList>();
  let galleryPictures = $state<FileList>();
  let fileList = $state<FileList>();
  let downloadAble = $state(false);
  let printAble = $state(false);
  let isSellerRole = $derived(isSeller());

  const MAX_WORDS = 30;
  const MAX_CHARS = 200;

  let preViewDescription = $derived.by(() => {
    const words = description.trim().split(/\s+/);
    let out = words.length > MAX_WORDS
      ? words.slice(0, MAX_WORDS).join(' ')
      : description;
    if (out.length > MAX_CHARS) out = out.slice(0, MAX_CHARS).trimEnd();
    return out.length < description.length ? out + '…' : out;
});

  // Object URLs for local preview. Revoke on change to avoid leaks.
  let thumbUrl = $state<string | null>(null);
  let galleryUrls = $state<string[]>([]);

  $effect(() => {
    const f = thumbList?.[0];
    if (!f) { thumbUrl = null; return; }
    const url = URL.createObjectURL(f);
    thumbUrl = url;
    return () => URL.revokeObjectURL(url);
  });

  $effect(() => {
    const files = galleryPictures ? Array.from(galleryPictures) : [];
    const urls = files.map((f) => URL.createObjectURL(f));
    galleryUrls = urls;
    return () => urls.forEach(URL.revokeObjectURL);
  });

  let previewListing = $derived({
    id: 'preview',
    title: title || '(Titel)',
    descriptionPreview: preViewDescription || '(Beskrivning)',
    price: Number(price) || 0,
    marketPictureLocation: thumbUrl ?? '',
    sellerUsername: 'Du',
    downloadAble,
    printAble
  });

  async function submit() {
    if (!fileList?.[0] || !thumbList?.[0] || !galleryPictures?.length) return;
    if (isSellerRole === false) return;
    const fd = new FormData();
    fd.append('title', title);
    fd.append('description', description);
    fd.append('price', String(price ?? 0));
    for (const g of Array.from(galleryPictures)) fd.append('GalleryImages', g);
    fd.append('file', fileList[0]);
    for (const t of Array.from(thumbList)) fd.append('thumbnail', t);
    fd.append('downloadAble', String(downloadAble));
    fd.append('printAble', String(printAble));
    try {
      let respond = await listingService.create(fd) as Response;

      if(respond.ok)
      {
        clearFields();
      }
    }
    catch (e) {
      console.error(e);
    }
     
  }

  function clearFields() {
    title = '';
    price = '';
    description = '';
    thumbList = undefined;
    galleryPictures = undefined;
    fileList = undefined;
    downloadAble = false;
    printAble = false;
  }
</script>
<div class="same-row">
  <h1>Skapa annons</h1>
  <h1>Förhandsvisning</h1>
</div>
<div class="same-row">
  
  <form class="form" onsubmit={(e) => e.preventDefault()}>
    <label>
      <span class="required">Titel</span>
      <input type="text" bind:value={title} />
    </label>
    
    <label>
      <span class="required">Pris</span>
      <input type="number" bind:value={price} min="0" step="0.01" />
    </label>
    
    <div class="descriptionField">
      <label class="description">
        <span class="required">Beskrivning</span>
        <textarea bind:value={description} rows="4"></textarea>
      </label>
    </div>
    
    <label>
      <span class="required">Fil</span>
      
      <input type="file" bind:files={fileList}/>
      <span class="required">Visningsbild</span>
      <input type="file" bind:files={thumbList}/>
      <span class="required">Galleribilder</span>
      <input type="file" multiple bind:files={galleryPictures}/>
    </label>
    <button type="submit" class="primary" onclick={submit}>Publicera</button>
    
    <label>
      <input type="checkbox" bind:checked={downloadAble} />
      Nedladdningsbar
      <input type="checkbox" bind:checked={printAble} />
      Utskriftsbar
    </label>
  </form>
  
  <div class="preview-grid">
    <ListingCard listing={previewListing} preview={true}/>
    
    {#if galleryUrls.length}
    <div class="gallery">
      <h3>Galleri</h3>
      <div class="gallery-grid">
        {#each galleryUrls as url, i (url)}
        <img src={url} alt="Galleribild {i + 1}" />
        {/each}
      </div>
    </div>
    {/if}
  </div>
</div>
  <style>

  .form {
    display: grid;
    gap: var(--space-3);
    max-width: 480px;
    border: 1px solid var(--color-border);
    border-radius: var(--radius-md);
    padding: var(--space-4);
    background: var(--color-surface);
  }
  label {
    display: grid;
    gap: 0.3rem;
    font-size: 0.9rem;
  }
  .form :global(button) {
    justify-self: start;
  }
  .preview-grid {
    display: grid;
    gap: var(--space-4);
    max-width: 480px;
    min-width: 480px;
    min-height: 300px;
  }
  .gallery-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(80px, 1fr));
    gap: var(--space-2);
  }
  .gallery-grid img {
    width: 100%;
    aspect-ratio: 1;
    object-fit: cover;
    border-radius: var(--radius-sm);
    border: 1px solid var(--color-border);
  }
  .same-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }
</style>
