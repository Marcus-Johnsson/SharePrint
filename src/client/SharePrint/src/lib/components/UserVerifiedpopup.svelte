<script lang="ts">
    import { onMount, onDestroy } from 'svelte';
    import { loadStripe, type Stripe, type StripeCardElement } from '@stripe/stripe-js';
    import { PUBLIC_STRIPE_KEY, PUBLIC_API_URL } from '$env/static/public';
    import { auth, isSeller, ui } from '$lib/services/auth.svelte';

    let stripe: Stripe | null = null;
    let card: StripeCardElement | null = null;
    let cardMount = $state<HTMLDivElement | undefined>();

    let status = $state<'idle' | 'loading' | 'success' | 'error'>('idle');
    let errorMsg = $state<string | null>(null);

    // Reactive: hide popup if user already seller or not logged in.
    const visible = $derived(
        ui.showVerifyPopup && auth.isAuthenticated === true && !isSeller()
    );

    // Mount Stripe Element only when popup actually shown.
    $effect(() => {
        if (!visible || !cardMount) return;

        let active = true;
        (async () => {
            stripe = await loadStripe(PUBLIC_STRIPE_KEY);
            if (!stripe || !active) return;
            const elements = stripe.elements();
            card = elements.create('card', {
                style: {
                    base: { fontSize: '16px', color: '#1a1a1a',
                        '::placeholder': { color: '#9ca3af' } },
                    invalid: { color: '#dc2626' }
                }
            });
            card.mount(cardMount);
            card.on('change', (e) => { errorMsg = e.error?.message ?? null; });
        })();

        return () => {
            active = false;
            card?.destroy();
            card = null;
        };
    });

    function close() {
        ui.showVerifyPopup = false;
        status = 'idle';
        errorMsg = null;
    }

    async function submit() {
        if (!stripe || !card) return;
        status = 'loading';
        errorMsg = null;

        try {
            // 1. Backend creates SetupIntent
            const intentRes = await fetch(`${PUBLIC_API_URL}/seller/apply/setup-intent`, {
                method: 'POST',
                credentials: 'include'
            });
            if (!intentRes.ok) {
                status = 'error';
                errorMsg = 'Kunde inte starta kortverifiering.';
                return;
            }
            const { clientSecret } = await intentRes.json();

            // 2. Stripe.js validates the card (PCI stays in iframe).
            const { setupIntent, error } = await stripe.confirmCardSetup(clientSecret, {
                payment_method: { card }
            });
            if (error || setupIntent?.status !== 'succeeded') {
                status = 'error';
                errorMsg = error?.message ?? 'Kortet kunde inte verifieras.';
                return;
            }

            // 3. Backend grants Seller role.
            const confirmRes = await fetch(`${PUBLIC_API_URL}/seller/apply/confirm`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'include',
                body: JSON.stringify({ setupIntentId: setupIntent.id })
            });
            if (!confirmRes.ok) {
                status = 'error';
                errorMsg = 'Behörighet kunde inte tilldelas.';
                return;
            }

            // Optimistic role update so isSeller() flips immediately.
            if (!auth.Roles.includes('Seller')) auth.Roles = [...auth.Roles, 'Seller'];

            status = 'success';
            setTimeout(close, 1200);
        } catch (err) {
            status = 'error';
            errorMsg = 'Något gick fel. Försök igen.';
            console.error(err);
        }
    }
</script>

{#if visible}
    <div class="backdrop" onclick={close} role="presentation"></div>
    <div class="popup" role="dialog" aria-modal="true" aria-labelledby="verify-title">
        <header>
            <h2 id="verify-title">Bli säljare</h2>
            <button class="close" onclick={close} aria-label="Stäng">&times;</button>
        </header>

        <p class="lead">Verifiera ett kort för att aktivera säljarbehörighet. Inga pengar dras.</p>

        <form onsubmit={(e) => { e.preventDefault(); submit(); }}>
            <label>
                Kortuppgifter
                <div bind:this={cardMount} class="card-input"></div>
            </label>

            {#if errorMsg}<p class="err">{errorMsg}</p>{/if}
            {#if status === 'success'}<p class="ok">Säljarbehörighet beviljad!</p>{/if}

            <div class="actions">
                <button type="button" class="ghost" onclick={close}>Avbryt</button>
                <button type="submit" class="primary" disabled={status === 'loading' || status === 'success'}>
                    {status === 'loading' ? 'Verifierar...' : 'Verifiera kort'}
                </button>
            </div>
        </form>
    </div>
{/if}

<style>
    .backdrop {
        position: fixed; inset: 0;
        background: rgba(0, 0, 0, 0.5);
        z-index: 100;
    }
    .popup {
        position: fixed;
        top: 50%; left: 50%;
        transform: translate(-50%, -50%);
        background: white;
        border-radius: 12px;
        padding: 1.5rem;
        width: min(90vw, 440px);
        box-shadow: 0 20px 60px rgba(0, 0, 0, 0.25);
        z-index: 101;
        display: grid;
        gap: 1rem;
    }
    header {
        display: flex;
        align-items: center;
        justify-content: space-between;
    }
    h2 { margin: 0; font-size: 1.25rem; }
    .close {
        background: none;
        border: none;
        font-size: 1.5rem;
        cursor: pointer;
        line-height: 1;
    }
    .lead { margin: 0; color: #555; font-size: 0.9rem; }
    label { display: grid; gap: 0.4rem; font-size: 0.9rem; }
    .card-input {
        padding: 0.75rem;
        border: 1px solid #d1d5db;
        border-radius: 6px;
        background: white;
    }
    .err { color: #dc2626; font-size: 0.85rem; margin: 0; }
    .ok { color: #16a34a; font-size: 0.85rem; margin: 0; }
    .actions {
        display: flex;
        gap: 0.75rem;
        justify-content: flex-end;
        margin-top: 0.5rem;
    }
    .primary {
        background: var(--color-accent, #2563eb);
        color: white;
        border: none;
        padding: 0.6rem 1.2rem;
        border-radius: 6px;
        cursor: pointer;
    }
    .primary:disabled { opacity: 0.6; cursor: not-allowed; }
    .ghost {
        background: transparent;
        border: 1px solid #d1d5db;
        padding: 0.6rem 1.2rem;
        border-radius: 6px;
        cursor: pointer;
    }
</style>
