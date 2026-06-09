<script lang="ts">
import { onMount, onDestroy } from 'svelte';
import {
    listingService,
    type ListingDetail,
    type GalleryItem,
    type PendingPicture,

    type DescriptionPicutre

} from '$lib/services/listingService';
import ListingCard from '$lib/components/ListingCard.svelte';

let {
    listingId,
    onSaved
}: {
    listingId: string | null;
    onSaved?: (id: string | null) => void;
} = $props();

let title = $state('');
let price = $state<number>();
let description = $state('');
let downloadAble = $state(false);
let printAble = $state(false);
let createdAt = $state('');
let lastUpdate = $state('');
let status = $state< 'Active' | 'Unlisted' | 'New'>('New'); // idea is New is only when creating, hence we can hide field when creating
let actionState = $state< 'delete' | 'loading' | ''>('');
let thumbnailUrl = $state<string | null>(null);
let thumbnailFile = $state<File | null>(null);
let thumbnailPreview = $derived(
    thumbnailFile ? URL.createObjectURL(thumbnailFile) : thumbnailUrl
);

let file = $state<File | null>(null);

let gallery = $state<GalleryItem[]>([]);

let loading = $state(false);

const MAX_WORDS = 30;
const MAX_CHARS = 200;

let preViewDescription = $derived.by(() => {
    const desc = description ?? '';
    const words = desc.trim().split(/\s+/);
    let out = words.length > MAX_WORDS
        ? words.slice(0, MAX_WORDS).join(' ')
        : desc;
    if (out.length > MAX_CHARS) out = out.slice(0, MAX_CHARS).trimEnd();
    return out.length < desc.length ? out + '…' : out;
});
let oldStatus = $state<'Active' | 'Unlisted'>('Active');

const isEdit = $derived(listingId !== null);

onMount(async () => {
    if (!listingId) return;
    try {
        const listing = await listingService.detail(listingId) as ListingDetail;
        title = listing.title ?? '';
        description = listing.description ?? '';
        price = listing.price;
        thumbnailUrl = listing.marketPictureLocation;
        downloadAble = listing.downloadAble;
        printAble = listing.printAble;
        status = listing.status;
        oldStatus = listing.status;
        gallery = listing.descriptionPictures.map(p => ({ kind: 'saved', data: p }));
        createdAt = (listing.createdAt);
        lastUpdate = (listing.updatedAt);
    } catch (err) {
        console.error('Error fetching listing details:', err);
    }
});

onDestroy(() => {
    for (const item of gallery) {
        if (item.kind === 'pending') URL.revokeObjectURL(item.data.previewUrl);
    }
});

function pickThumbnail(e: Event) {
    const input = e.target as HTMLInputElement;
    thumbnailFile = input.files?.[0] ?? null;
}

function addGalleryFiles(e: Event) {
    const input = e.target as HTMLInputElement;
    if (!input.files) return;
    const additions: GalleryItem[] = Array.from(input.files).map(f => ({
        kind: 'pending',
        data: { file: f, previewUrl: URL.createObjectURL(f) }
    }));
    gallery = [...gallery, ...additions];
    input.value = '';
}

function removeGalleryAt(index: number) {
    const item = gallery[index];
    if (item.kind === 'pending') URL.revokeObjectURL(item.data.previewUrl);
    gallery = gallery.filter((_, i) => i !== index);
}

async function deleteListing() {
    if (!listingId) return;
    loading = true;
    actionState = 'delete';
    try {
        await listingService.delete(listingId);
        onSaved?.(null);
    } catch (err) {
        console.error('Delete failed:', err);
    } finally {
        loading = false;
        actionState = '';
    }
}

async function submit() {
    if (loading) return;
    loading = true;
    try {
        if (isEdit) {
            await submitEdit(listingId!);
        } else {
            await submitCreate();
        }
    } catch (err) {
        console.error('Submit failed:', err);
    } finally {
        loading = false;
    }
}

async function submitCreate() {
    if (!thumbnailFile || !file || price === undefined) {
        throw new Error('Missing required fields');
    }
    const form = new FormData();
    form.append('title', title);
    form.append('description', description);
    form.append('price', String(price));
    form.append('thumbnail', thumbnailFile);
    form.append('file', file);
    form.append('downloadAble', String(downloadAble));
    form.append('printAble', String(printAble));
    for (const item of gallery) {
        if (item.kind === 'pending') form.append('galleryImages', item.data.file);
    }
    const created = await listingService.create(form);
    const newId = created && 'id' in created ? (created as ListingDetail).id : null;
    onSaved?.(newId);
}

async function submitEdit(id: string) {
    if (price === undefined) throw new Error('Price required');

    const keptGalleryIds = gallery
        .filter((g): g is { kind: 'saved', data: DescriptionPicutre } => g.kind === 'saved')
        .map(g => g.data.id);

    const newGalleryImages = gallery
        .filter((g): g is { kind: 'pending', data: PendingPicture } => g.kind === 'pending')
        .map(g => g.data.file);

    await listingService.update(id, {
        title,
        description,
        price,
        downloadAble,
        printAble,
        thumbnail: thumbnailFile ?? undefined,
        keptGalleryIds,
        newGalleryImages,
    });
    
    if (oldStatus !== status && (status === 'Active' || status === 'Unlisted')) {
        const response = await listingService.status(id, status);
        if (response && typeof response === 'object' && 'code' in response) {
            console.error('Failed to update status:', response);
        } else {
            oldStatus = status;
        }
    }

    thumbnailFile = null;
    gallery = gallery.filter(g => g.kind === 'saved');
    onSaved?.(id);
}

let previewListing = $derived({
    id: listingId ?? 'preview',
    title: title || '(Titel)',
    descriptionPreview: preViewDescription || '(Beskrivning)',
    price: Number(price) || 0,
    marketPictureLocation: thumbnailPreview ?? '',
    sellerUsername: 'Du',
    downloadAble,
    printAble
});
</script>

<div class="same-row">
    <h1>{isEdit ? 'Redigera annons' : 'Skapa annons'}</h1>
    <h1>Förhandsvisning</h1>
</div>

<div class="same-row">
    <form class="form" onsubmit={(e) => { e.preventDefault(); submit(); }}>
        <label>
            <span class="required">Titel</span>
            <input type="text" bind:value={title} />
        </label>

        <label>
            <span class="required">Pris</span>
            <input type="number" bind:value={price} min="0" step="0.01" />
        </label>

        <label class="description">
            <span class="required">Beskrivning</span>
            <textarea bind:value={description} rows="4"></textarea>
        </label>

        {#if !isEdit}
            <label>
                <span class="required">Fil</span>
                <input type="file" onchange={(e) => {
                    const i = e.target as HTMLInputElement;
                    file = i.files?.[0] ?? null;
                }} />
            </label>
        {/if}

        <label>
            <span class="required">Visningsbild</span>
            <input type="file" accept="image/*" onchange={pickThumbnail} />
            {#if thumbnailPreview}
                <img class="thumb-preview" src={thumbnailPreview} alt="Visningsbild" />
            {/if}
        </label>

        <div class="gallery-field">
            <span class="required">Galleribilder ({gallery.length}/5)</span>
            <input type="file" accept="image/*" multiple onchange={addGalleryFiles} />
            {#if gallery.length}
                <div class="gallery-grid">
                    {#each gallery as item, i (item.kind === 'saved' ? item.data.id : item.data.previewUrl)}
                        <div class="gallery-item">
                            <img
                                src={item.kind === 'saved' ? item.data.url : item.data.previewUrl}
                                alt="Galleribild {i + 1}"
                            />
                            <button type="button" class="remove" onclick={() => removeGalleryAt(i)}>×</button>
                        </div>
                    {/each}
                </div>
            {/if}
        </div>

        <label class="checkbox-row">
            <input type="checkbox" bind:checked={downloadAble} />
            Nedladdningsbar
        </label>
        <label class="checkbox-row">
            <input type="checkbox" bind:checked={printAble} />
            Utskriftsbar
        </label>

        {#if isEdit && status !== 'New'}
            <label>
                <span>Gjordes: {createdAt}</span>
                <span>Senaste ändringen: {lastUpdate}</span>
                <span>Status</span>
                <select bind:value={status}>
                    <option value="Active">Aktiv</option>
                    <option value="Unlisted">Avlistad</option>
                </select>
            </label>
        {/if}
        <div>
            <button type="submit" class="primary" disabled={loading}>
                {loading ? 'Sparar…' : isEdit ? 'Spara' : 'Publicera'}
            </button>
            {#if isEdit}
                <button type="button" class="primary" disabled={loading} onclick={deleteListing}>
                    {actionState == 'delete' ? 'Tar bort...' : 'Ta bort'}
                </button>
            {/if}
        </div>
    </form>

    <div class="preview-grid">
        <ListingCard listing={previewListing} showBuy={false} />
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
    .checkbox-row {
        grid-template-columns: auto 1fr;
        align-items: center;
    }
    .form button[type='submit'] {
        justify-self: start;
    }
    .thumb-preview {
        width: 120px;
        aspect-ratio: 1;
        object-fit: cover;
        border-radius: var(--radius-sm);
        border: 1px solid var(--color-border);
    }
    .gallery-field {
        display: grid;
        gap: 0.3rem;
    }
    .gallery-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(80px, 1fr));
        gap: var(--space-2);
    }
    .gallery-item {
        position: relative;
    }
    .gallery-item img {
        width: 100%;
        aspect-ratio: 1;
        object-fit: cover;
        border-radius: var(--radius-sm);
        border: 1px solid var(--color-border);
    }
    .remove {
        position: absolute;
        top: 2px;
        right: 2px;
        width: 22px;
        height: 22px;
        border-radius: 50%;
        border: none;
        background: rgba(0, 0, 0, 0.6);
        color: white;
        cursor: pointer;
        line-height: 1;
    }
    .preview-grid {
        display: grid;
        gap: var(--space-4);
        max-width: 480px;
        min-width: 480px;
        min-height: 300px;
    }
    .same-row {
        display: flex;
        justify-content: space-between;
        align-items: center;
    }
</style>
