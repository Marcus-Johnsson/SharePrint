import { browser } from "$app/environment";
import { goto } from "$app/navigation";
import type { ListingDetail } from "./listingService";
import { PUBLIC_API_URL } from "$env/static/public";



class AuthState {
	isAuthenticated = $state<boolean | null>(null);
    Username = $state<string| null>(null);
    Email = $state<string | null>(null);
    Roles = $state<string[]>([]);
    Cart = $state<Cart | []>([]);

};

class Cart {
    listingDetail: ListingDetail[] = [];
}

export const auth = new AuthState();
export const cart = new Cart();
export const isSeller = () => auth.Roles.includes('Seller');
export const isAdmin = () => auth.Roles.includes('Admin'); // no plan for admin parts but created for future ref




function saveSession(session: Partial<AuthState>) {
    if(browser) {
        const existingSessions = getStoredSession();
        const updatedSeesion = { ...existingSessions, ...session};
        localStorage.setItem('user_session', JSON.stringify(updatedSeesion));
    }
}

export function getStoredSession(): AuthState {
    if(browser) {
        try{
            const stored = localStorage.getItem('user_session');
            if(stored) {
                return JSON.parse(stored);
            }
        }
        catch (error){
            console.error('Error geting localstorage session: ', error)
        }
    }
    return {
        isAuthenticated: false,
        Username: null,
        Email: null,
        Roles: [],
        Cart: {
            listingDetail: []
        }
    };
}

function clearSession() {
    if(browser) {
        localStorage.removeItem('user_session');
    }
}

function clearStore() {
    auth.isAuthenticated = false,
    auth.Username = null,
    auth.Email = null,
    auth.Cart = [];
}

function emptyCart() {
    cart.listingDetail = [];
}

export const logout = async () => {
    try {
        const response = await fetch(`${PUBLIC_API_URL}/auth/logout`, {
            method: 'POST',
            credentials: 'include'
        }); // path is most likley wrong 

        if(response.ok) {
            clearSession();
            clearStore();

            await goto('/login', { replaceState: true });
        }else {
			console.error('Logout failed');
		}
    }
    catch (err) {
		console.error('Error during logout:', err);
	}
};

export const login = async (payload: {email: string, password: string} ) => {
    try {
        const response = await fetch(`${PUBLIC_API_URL}/auth/login?useCookies=true`, {
			method: 'POST',
			headers: {
				'Content-Type': 'application/json'
			},
			body: JSON.stringify( payload),
			credentials: 'include'
		});

        if(response.ok) {
            const data: { email: string; userName: string } = await response.json();
            auth.isAuthenticated = true;
            auth.Email = data.email;
            auth.Username = data.userName;

            saveSession({
                isAuthenticated: true,
                Email: data.email,
                Username: data.userName,
            });

            await goto('/', { replaceState: true });
        } else {
            console.error('Login failed');
        }
    }
    catch (err) {
        console.error('Error during login:', err);
    }
};

export const register = async (payload: { email: string; username: string; password: string }) => {
    try{
        const response = await fetch(`${PUBLIC_API_URL}/auth/register`, {
		method: 'POST',
		headers: {
			'Content-Type': 'application/json'
		},
		body: JSON.stringify(payload),
		credentials: 'include'
    	});
        if(response.ok) {
            //Autolog in since we dont have email confirmation, changed towards V1. Create better catches for error based on response status
            try {
                login(payload)
            }
            catch (error) {
                console.log('Error while loggin in: ', error)
            }
        }
    }
    catch (error) {
        console.log('Error while register account in: ', error)
    }
}

export const changePassword = async (oldPassword: string, newPassword: string) => {
    try {
        const respone = await fetch(`${PUBLIC_API_URL}/auth/changepassword`, {
            headers: {
			'Content-Type': 'application/json'
		},
		body: JSON.stringify({ oldPassword, newPassword }),
		credentials: 'include'
    });

    if(respone.ok) {
        return  { success: true }
        }
        else {
           let errorData: { title?: string } | null = null;

			const contentType = respone.headers.get('content-type') ?? '';
			if (contentType.includes('application/json')) {
				errorData = await respone.json();
			}

			let errorMessage = 'Kunde inte ändra lösenordet';
			if (respone.status === 401) {
				errorMessage = 'Du är inte inloggad';
			} else if (respone.status === 400) {
				errorMessage = errorData?.title || 'Ogiltigt lösenord';
			} else if (respone.status >= 500) {
				errorMessage = 'Serverfel. Försök igen om en stund';
			} else if (errorData?.title) {
				errorMessage = errorData.title;
			}
			return { success: false, error: errorMessage };
		}
    }
    catch (error) {
        return { success: false, message: error }
    }
}


export const applyToSellerRole = async () => {
    // stripe logic unknow for now
}

export const resetPassword = async () => {
    // no backend yet and email service

}

export const forgotPassword = async () => {
    // no backend yet and email service
}