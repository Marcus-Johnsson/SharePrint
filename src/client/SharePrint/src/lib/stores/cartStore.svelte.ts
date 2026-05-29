import type { ListingSummary } from "$lib/services/listingService";

type CartItem = {
  product: ListingSummary;
  amount: number;
};

class Cart {
  products = $state<CartItem[]>([]);

  addListing(newListing: ListingSummary) {
    const idx = this.products.findIndex(p => p.product.id === newListing.id);
    if (idx !== -1) {
      this.products[idx].amount++;
    } else {
      this.products.push({ product: newListing, amount: 1 });
    }
  }

  remove(id: string) {
    this.products = this.products.filter(p => p.product.id !== id);
  }

  clear() {
    this.products = [];
  }

  reduceAmount(id: string) {
    const idx = this.products.findIndex(p => p.product.id === id);
    if (idx === -1) {
      console.log('Found no listing with id: ', { id });
      return;
    }
    this.products[idx].amount--;
    if (this.products[idx].amount <= 0) {
      this.remove(id);
    }
  }

  setAmount(id: string, newAmount: number) {
    const idx = this.products.findIndex(p => p.product.id === id);
    if (idx === -1) {
      console.log('Found no listing with id: ', { id });
      return;
    }
    if (newAmount <= 0) {
      this.remove(id);
      return;
    }
    this.products[idx].amount = newAmount;
  }
  get total() {
    return this.products.reduce(
      (sum, p) => sum + p.product.price * p.amount,
      0
    );
  }
}

export const cart = new Cart();