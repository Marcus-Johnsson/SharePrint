import { auth } from "$lib/stores/auth.svelte";

const apiUrl = import.meta.env.VITE_API_URL;

export interface AppError {
    code: 'AUTH_ERROR' | 'API_ERROR' | 'NETWORK_ERROR' | 'NOT_FOUND';
    message: string;
    status?: number;
    details?: unknown;
}

function authError(res: Response): AppError | null {
    if (res.status === 401) {
        auth.isAuthenticated = false; auth.user = null;
        return {code: 'AUTH_ERROR', message: 'Authentication required.', status: 401};
    }
    return null;
}

export class ApiService {
    private fetch: typeof fetch;
    constructor(fetchFn: typeof fetch = fetch) { this.fetch = fetchFn; }

    private async parse<T>(res: Response): Promise<T | null> {
        if (res.status === 204) return null;
        const t = await res.text();
        if (!t) return null;
        try {
            return JSON.parse(t) as T;
        }
        catch { throw new Error(`Invalid JSON (status ${res.status})`);}
    }
    async get<T>(endpoint: string, params?: Record<string, string | number | undefined>): Promise<T | null | AppError> {
        try{
            let url = `${apiUrl}/${endpoint}`;
            if(params) {
            const q = Object.entries(params).filter(([, v]) => v !== undefined && v !== '')
          .map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(String(v))}`);
            if (q.length) url += `?${q.join('&')}`;
            }
            const res = await this.fetch(url, { credentials: 'include' });
            if (!res.ok) {
                const ae = authError(res); if (ae) return ae;
                if (res.status === 404) return { code: 'NOT_FOUND', message: `Not found: ${url}`, status: 404 };
                return { code: 'API_ERROR', message: `GET ${url} failed`, status: res.status, details: await res.text() };
            }
            return await this.parse<T>(res);
            } catch (e) { return { code: 'NETWORK_ERROR', message: 'Network error', details: e }; }
        }
   
    async post<T,D>(endpoint: string, data: D) { return this.body<T, D>('POST', endpoint, data); }
    async patch<T,D>(endpoint: string, data: D) { return this.body<T, D>('PATCH', endpoint, data); }
    async put<T,D>(endpoint: string, data: D) { return this.body<T, D>('PUT', endpoint, data); }

    private async body<T, D>(method:string, endpoint: string, data: D): Promise<T | null | AppError>{
        try {
            const res = await this.fetch(`${apiUrl}/${endpoint}`, {
                method, headers: { 'Content-Type': 'application/json' },
                credentials: 'include', body: JSON.stringify(data)
            });
            if (!res.ok) {
                const error = authError(res);
                if (error) return error;
                        return { code: 'API_ERROR', message: `${method} ${endpoint} failed`, status: res.status, details: await res.text() };
                }
                return await this.parse<T>(res);
                } catch (e) { return { code: 'NETWORK_ERROR', message: 'Network error', details: e }; }
        }

    async delete<T>(endpoint: string): Promise<T | null | AppError> {
        try {
        const res = await this.fetch(`${apiUrl}/${endpoint}`, { method: 'DELETE', credentials: 'include' });
        if (!res.ok) {
            const ae = authError(res); if (ae) return ae;
            return { code: 'API_ERROR', message: `DELETE ${endpoint} failed`, status: res.status, details: await res.text() };
        }
        return res.status === 204 ? null : await this.parse<T>(res);
        } catch (e) { return { code: 'NETWORK_ERROR', message: 'Network error', details: e }; }
    }

    async postFormData<T>(endpoint: string, form: FormData): Promise<T | null | AppError> {
        try {
        const res = await this.fetch(`${apiUrl}/${endpoint}`, { method: 'POST', credentials: 'include', body: form });
        if (!res.ok) {
            const ae = authError(res); if (ae) return ae;
            return { code: 'API_ERROR', message: `POST ${endpoint} failed`, status: res.status, details: await res.text() };
        }
        return await this.parse<T>(res);
        } catch (e) { return { code: 'NETWORK_ERROR', message: 'Network error', details: e }; }
    }
}
