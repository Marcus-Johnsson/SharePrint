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
    status: 'Active' | 'Unlisted',
    downloadAble: boolean,
    printAble: boolean,
    createdAt: string,
    updatedAt: string
};

export type ListingUpdate = {
    title: string,
    description: string,
    price: number,
    downloadAble: boolean,
    printAble: boolean,
    thumbnail?: File,           // omit = keep
    keptGalleryIds: string[],   // existing IDs user did NOT remove
    newGalleryImages: File[]    // uploads
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

    update: (id: string, payload: ListingUpdate) => {
        const form = new FormData();
        form.append('title', payload.title);
        form.append('description', payload.description);
        form.append('price', String(payload.price));
        form.append('downloadAble', String(payload.downloadAble));
        form.append('printAble', String(payload.printAble));
        if (payload.thumbnail) form.append('thumbnail', payload.thumbnail);
        for (const gid of payload.keptGalleryIds) form.append('keptGalleryIds', gid);
        for (const f of payload.newGalleryImages) form.append('newGalleryImages', f);
        return api.upload<ListingDetail>(`listings/${id}`, form, 'PUT');
},

    status: (id: string, status: 'Active' | 'Unlisted') =>
        api.post<unknown, { status: string }>(`listings/${id}/status`, { status }),
};
