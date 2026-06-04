import { api } from './apiService';

export type ListingSummary = {
    id: string,
    title: string,
    descriptionPreview: string,
    price: number,
    marketPictureLocation: string,
    sellerUsername: string,
    downloadAble: boolean,
    printAble: boolean
};

export type DescriptionPicutre = {
    id: string,
    url: string
};

export type PendingPicture = {
    file: File,
    previewUrl: string
};

export type GalleryItem =
    | { kind: 'saved', data: DescriptionPicutre }
    | { kind: 'pending', data: PendingPicture };

export type ListingDetail = {
    id: string,
    title: string,
    description: string,
    price: number,
    marketPictureLocation: string,
    descriptionPictures: DescriptionPicutre[],
    sellerUsername: string,
    status: string,
    downloadAble: boolean,
    printAble: boolean
};

export type ListingUpdate = {
    title?: string,
    description?: string,
    price?: number,
    downloadAble?: boolean,
    printAble?: boolean,
    thumbnail?: File,
    galleryImages?: File[],
    removedGalleryIds?: string[]
};

export type ListingPage = {
    items: ListingSummary[];
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
    hasNextPage: boolean;
    hasPreviousPage: boolean;
};

export const listingService = {
    catalog: (page: number, pageSize: number) =>
    api.get<ListingPage>(`listings/${page}/${pageSize}`),

    userCatalog: (page: number, pageSize: number) =>
    api.get<ListingPage>(`listings/user/${page}/${pageSize}`),

    detail: (id: string) =>
        api.get<ListingDetail>(`listings/${id}`),

    create: (form: FormData) =>
        api.upload<ListingDetail>('listings', form, 'POST'),

    update: (id: string, patch: ListingUpdate) => {
        const form = new FormData();
        if (patch.title        !== undefined) form.append('title', patch.title);
        if (patch.description  !== undefined) form.append('description', patch.description);
        if (patch.price        !== undefined) form.append('price', String(patch.price));
        if (patch.downloadAble !== undefined) form.append('downloadAble', String(patch.downloadAble));
        if (patch.printAble    !== undefined) form.append('printAble', String(patch.printAble));
        if (patch.thumbnail)                  form.append('thumbnail', patch.thumbnail);
        for (const f of patch.galleryImages    ?? []) form.append('galleryImages', f);
        for (const id of patch.removedGalleryIds ?? []) form.append('removedGalleryIds', id);
        return api.upload<ListingDetail>(`listings/${id}`, form, 'PUT');
    },

    unlist: (id: string) =>
        api.post<unknown, undefined>(`listings/${id}/unlist`, undefined),
};
