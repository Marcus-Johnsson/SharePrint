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

export type ListinCreation = {
    title: string,
    description: string,
    price: number,
    marketPicture: File,
    thumbnail: FileList,
    file: File,
    downloadAble: boolean,
    printAble: boolean
};

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

    detail: (id: string) =>
        api.get<ListingDetail>(`listings/${id}`),

    create: (form: FormData) =>
        api.upload('listings', form, 'POST'),

    replaceThumbnail: (id: string, file: File) => {
        const form = new FormData();
        form.append('thumbnail', file);
        return api.upload<ListingDetail>(`listings/${id}/thumbnail`, form, 'PUT');
    },

    addGalleryImage: (id: string, file: File) => {
        const form = new FormData();
        form.append('image', file);
        return api.upload<ListingDetail>(`listings/${id}/gallery`, form, 'POST');
    },

    deleteGalleryImage: (id: string, imageId: string) =>
        api.delete<ListingDetail>(`listings/${id}/gallery/${imageId}`),

    patch: (id: string, body: { title: string; description: string; price: number }) =>
        api.patch<unknown, typeof body>(`listings/${id}`, body),

    unlist: (id: string) =>
        api.post<unknown, undefined>(`listings/${id}/unlist`, undefined),
};
