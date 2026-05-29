<script lang="ts">
  import type { ListinCreation } from "$lib/services/listingService";
  import { listingService } from "$lib/services/listingService";

  let title = $state('');
  let price = $state('');
  let description = $state('');
  let thumbList = $state<FileList>();
  let galleryPictures = $state<FileList>();
  let fileList = $state<FileList>();
  let downloadAble = $state(false);
  let printAble = $state(false);

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

  async function submit() {
    if (!fileList?.[0] || !thumbList?.[0] || !galleryPictures?.length) return;
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

<h1>Create listing</h1>

<form class="form" onsubmit={(e) => e.preventDefault()}>
  <label>
    <span class="required">Title</span>
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

    <label class="preview">
      PreView text
      <textarea  rows="4" disabled>{preViewDescription}</textarea>
    </label>
  </div>

  <label>
          <span class="required">Fil</span>

    <input type="file" bind:files={fileList}/>
      <span class="required">Visnings bild</span>
    <input type="file" bind:files={thumbList}/>
      <span class="required">Galleri bilder</span>
    <input type="file" multiple bind:files={galleryPictures}/>
  </label>
  <button type="submit" class="primary" onclick={submit}>Publish</button>

  <label>
    <input type="checkbox" bind:checked={downloadAble} />
    Download able
  <input type="checkbox" bind:checked={printAble} />
    Print able
  </label>  
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
