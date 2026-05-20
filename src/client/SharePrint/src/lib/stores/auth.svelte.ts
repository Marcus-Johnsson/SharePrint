

export const auth = $state<{ 
    isAuthenticated: boolean;
     user:
        {
            id: string;
            email: string;
            displayname: string;
            roles: string[]
        } | null }>({
    isAuthenticated: false,
    user: null
});