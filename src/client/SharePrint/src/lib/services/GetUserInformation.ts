import { api, type AppError } from '$lib/services/apiService';
import { auth } from './auth.svelte';

export interface MeResponse {
    email: string;
    displayName: string;
    roles: string[];
}

function isAppError(x: unknown): x is AppError {
    return !!x && typeof x === 'object' && 'code' in x;
}


export async function GetUserInformation(): Promise<MeResponse | null> {
    const res = await api.get<MeResponse>('auth/me');

    if (isAppError(res)) {
        if (res.code !== 'AUTH_ERROR') console.error(res);
        return null; // apiService already flipped auth.isAuthenticated on 401
    }
    if (!res) return null;

    auth.isAuthenticated = true;
    auth.Email = res.email;
    auth.Username = res.displayName;
    auth.Roles = res.roles ?? [];
    return res;
}
