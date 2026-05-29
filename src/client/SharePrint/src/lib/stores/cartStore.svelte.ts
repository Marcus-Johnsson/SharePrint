import type { ListingSummary } from "$lib/services/listingService";
import { PUBLIC_PRINT_SURCHARGE } from "$env/static/public";

export type CartOption = 'print' | 'download' | null;

type CartItem = {
  product: ListingSummary;
  selectedOption: CartOption;
};

// Must match server config "Checkout:PrintSurcharge". Falls back to 0 if env missing.
const PRINT_SURCHARGE = Number(PUBLIC_PRINT_SURCHARGE ?? 0) || 0;

function defaultOption(l: ListingSummary): CartOption {
  if (l.downloadAble && !l.printAble) return 'download';
  if (l.printAble && !l.downloadAble) return 'print';
  return null; // both available — user must pick
}

function unitPrice(item: CartItem): number {
  const base = item.product.price;
  return item.selectedOption === 'print' ? base + PRINT_SURCHARGE : base;
}

class Cart {
  products = $state<CartItem[]>([]);

  /** Add a listing. No-op if already in cart (digital goods own-once semantics). */
  addListing(newListing: ListingSummary) {
    const exists = this.products.some(p => p.product.id === newListing.id);
    if (exists) return;
    this.products.push({
      product: newListing,
      selectedOption: defaultOption(newListing)
    });
  }

  remove(id: string) {
    this.products = this.products.filter(p => p.product.id !== id);
  }

  clear() {
    this.products = [];
  }

  setOption(id: string, option: CartOption) {
    const idx = this.products.findIndex(p => p.product.id === id);
    if (idx === -1) return;
    this.products[idx].selectedOption = option;
  }

  unitPrice(item: CartItem): number {
    return unitPrice(item);
  }

  get total() {
    return this.products.reduce((sum, p) => sum + unitPrice(p), 0);
  }

  get allOptionsChosen() {
    return this.products.every(p => p.selectedOption !== null);
  }
}

export const cart = new Cart();
