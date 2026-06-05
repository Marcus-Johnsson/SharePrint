<script lang="ts">
  import { onMount } from 'svelte';
  import { cart } from '$lib/stores/cartStore.svelte';
  import { loadStripe, type Stripe, type StripeElements } from '@stripe/stripe-js';
  import { PUBLIC_STRIPE_KEY } from '$env/static/public';
  import { auth } from '$lib/stores/auth.svelte';


  let stripe: Stripe | null = null;
  let elements: StripeElements | null = null;
  let message = $state('');
  let processing = $state(false);

  onMount(async () => {
    if (cart.products.length === 0) return;
    if (!cart.allOptionsChosen) {
      message = 'Välj Nedladdning eller Utskrift för varje produkt innan du går vidare till kassan.';
      return;
    }

    // 1. ask backend to create a PaymentIntent for current cart
    const res = await fetch('/api/checkout/create-intent', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({
        items: cart.products.map(p => ({
          id: p.product.id,
          option: p.selectedOption
        }))
      })
    });
    if (!res.ok) {
      const body = await res.text();
      console.error('create-intent failed', res.status, body);
      message = `Kunde inte starta kassan (${res.status}): ${body || res.statusText}`;
      return;
    }
    const { clientSecret } = await res.json();

    // 2. load Stripe + mount Payment Element
    stripe = await loadStripe(PUBLIC_STRIPE_KEY);
    if (!stripe) return;
    elements = stripe.elements({ clientSecret });
    elements.create('payment').mount('#payment-element');
  });

  async function handleSubmit(e: SubmitEvent) {
    e.preventDefault();
    if (!stripe || !elements) return;
    processing = true;
    const { error } = await stripe.confirmPayment({
      elements,
      confirmParams: { return_url: `${window.location.origin}/checkout/success` }
    });
    if (error) message = error.message ?? 'Betalning misslyckades';
    processing = false;
  }
</script>

<h1>Kassa</h1>

<div class="cart-preview" aria-label="Ordersammanfattning">
    <div class="items">
        {#if cart.products.length === 0}
            <p class="empty">Din varukorg är tom.</p>
        {:else}
            {#each cart.products as cartItem (cartItem.product.id)}
                <article class="item" title={cartItem.product.title}>
                    <span class="title">
                        {cartItem.product.title}
                        <small class="path">({cartItem.selectedOption ?? '— ej valt —'})</small>
                    </span>
                    <span class="line-total">{cart.unitPrice(cartItem).toFixed(2)} SEK</span>
                </article>
            {/each}
        {/if}
    </div>
    <form id="payment-form" onsubmit={handleSubmit}>
      <div id="payment-element">
        <!--Stripe.js injicerar betalningselementet-->
      </div>

      <footer class="foot">
        <div class="total">
          <span>Totalt</span>
          <strong>{cart.total.toFixed(2)} SEK</strong>
        </div>

        {#if auth.isAuthenticated}
          <button id="submit" class="buy-btn" disabled={processing || cart.products.length === 0}>
            <span id="button-text">{processing ? 'Behandlar…' : 'Köp'}</span>
          </button>
        {:else}
          <div class="auth-msg">
            <p class="message">Du behöver <a href="/login">logga in</a> för att avsluta köpet.</p>
            <p class="message">Har du inget konto? Registera <a href="/register">här!</a></p>
          </div>
        {/if}
      </footer>
    </form>
</div>


<style>
.cart-preview {
    width: 420px;
    max-height: calc(100vh - 90px);
    display: flex;
    flex-direction: column;
    background: var(--color-surface);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-md);
    box-shadow: var(--shadow-menu), 0 12px 32px rgba(37, 52, 63, 0.03);
    overflow: hidden;
    animation: slide-in 180ms ease-out;
}

#payment-form {
    flex: 1;
    display: flex;
    flex-direction: column;
    min-height: 0;
}

#payment-element {
    padding: var(--space-3) var(--space-4);
    overflow-y: auto;
}

@keyframes slide-in {
    from { opacity: 0; transform: translateY(-6px) translateX(8px); }
    to   { opacity: 1; transform: translateY(0) translateX(0); }
}

.items {
    flex: 1;
    overflow-y: auto;
    padding: var(--space-2) var(--space-3);
    display: flex;
    flex-direction: column;
}

.empty {
    margin: 0;
    padding: var(--space-4) 0;
    text-align: center;
    color: var(--color-muted);
}

.item {
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto;
    align-items: center;
    column-gap: var(--space-3);
    padding: var(--space-2) var(--space-1);
    border-bottom: 1px solid var(--color-border);
    font-size: 0.85rem;
}
.path {
    margin-left: var(--space-1);
    color: var(--color-muted);
    font-weight: 400;
    font-size: 0.75rem;
    text-transform: capitalize;
}
.item:last-child {
    border-bottom: none;
}
.item:hover {
    background: var(--color-bg);
}

.title {
    font-weight: 600;
    color: var(--color-text);
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    min-width: 0;
}

.line-total {
    font-weight: 600;
    color: var(--color-text);
    font-variant-numeric: tabular-nums;
    white-space: nowrap;
    min-width: 64px;
    text-align: right;
    font-size: 0.8rem;
}

.foot {
    margin-top: auto;
    position: sticky;
    bottom: 0;
    border-top: 1px solid var(--color-border);
    padding: var(--space-3) var(--space-4);
    background: var(--color-bg);
    display: flex;
    flex-direction: column;
    gap: var(--space-3);
    z-index: 1;
}
.total {
    display: flex;
    justify-content: space-between;
    align-items: baseline;
    font-size: 0.95rem;
}
.total strong {
    font-size: 1.05rem;
    color: var(--color-text);
}
.message {
    font-size: 0.9rem;
    color: var(--color-muted);
    margin: 0;
}
.auth-msg {
    display: flex;
    flex-direction: column;
    gap: var(--space-1);
}

.buy-btn {
    width: 100%;
    padding: 0.85rem 1rem;
    font-size: 1rem;
    font-weight: 600;
    color: white;
    background: var(--color-accent, #2563eb);
    border: none;
    border-radius: var(--radius-md);
    cursor: pointer;
    transition: background 120ms ease, transform 80ms ease;
}
.buy-btn:hover:not(:disabled) {
    background: var(--color-accent-hover, #1d4ed8);
}
.buy-btn:active:not(:disabled) {
    transform: translateY(1px);
}
.buy-btn:disabled {
    opacity: 0.55;
    cursor: not-allowed;
}
</style>
