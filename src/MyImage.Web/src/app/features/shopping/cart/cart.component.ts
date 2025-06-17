import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';

import { CartService } from '../../../core/services/cart.service';
import { Cart, CartItem } from '../../../shared/models/cart.models';
import { PrintSelectorComponent } from '../../photo/print-selector/print-selector.component';

/**
 * Shopping Cart Component
 *
 * Displays the user's current cart items with detailed print selections,
 * allows editing and removing items, shows order summary with totals,
 * and provides checkout navigation.
 *
 * Key Features:
 * - Display cart items with photo previews and print selections
 * - Edit print selections (opens print selector dialog)
 * - Remove individual items or clear entire cart
 * - Real-time total calculations
 * - Responsive design for mobile devices
 * - Checkout navigation with validation
 */
@Component({
  selector: 'app-cart',
  standalone: false,

  template: `
    <div class="cart-container">
      <div class="cart-header">
        <h2>Shopping Cart</h2>
        <button mat-button color="warn"
                *ngIf="cart && cart.items.length > 0"
                (click)="clearCart()">
          <mat-icon>clear_all</mat-icon>
          Clear Cart
        </button>
      </div>

      <!-- Cart Items -->
      <div class="cart-content" *ngIf="cart && cart.items.length > 0">
        <mat-card class="cart-item" *ngFor="let item of cart.items; trackBy: trackByItemId">
          <div class="item-layout">
            <!-- Photo Preview -->
            <div class="photo-section">
              <img [src]="item.photoThumbnailUrl"
                   [alt]="item.photoFilename"
                   class="photo-thumbnail"
                   loading="lazy">
              <div class="photo-info">
                <h4>{{ item.photoFilename }}</h4>
                <p>{{ item.photoDimensions.width }} × {{ item.photoDimensions.height }}</p>
              </div>
            </div>

            <!-- Print Selections -->
            <div class="selections-section">
              <h5>Print Selections</h5>
              <div class="selection-list">
                <div class="selection-item" *ngFor="let selection of item.printSelections; trackBy: trackBySelectionSize">
                  <div class="selection-info">
                    <span class="size-name">{{ selection.sizeName }}</span>
                    <span class="quantity">
                      {{ selection.quantity }} × {{ selection.unitPrice | currency:'USD':'symbol':'1.2-2' }}
                    </span>
                  </div>
                  <div class="selection-total">
                    {{ selection.lineTotal | currency:'USD':'symbol':'1.2-2' }}
                  </div>
                </div>
              </div>

              <div class="photo-total">
                <strong>Photo Total: {{ item.photoTotal | currency:'USD':'symbol':'1.2-2' }}</strong>
              </div>
            </div>

            <!-- Actions -->
            <div class="actions-section">
              <button mat-icon-button color="primary"
                      (click)="editItem(item)"
                      matTooltip="Edit Selections"
                      [disabled]="isLoading">
                <mat-icon>edit</mat-icon>
              </button>
              <button mat-icon-button color="warn"
                      (click)="removeItem(item)"
                      matTooltip="Remove from Cart"
                      [disabled]="isLoading">
                <mat-icon>delete</mat-icon>
              </button>
            </div>
          </div>
        </mat-card>

        <!-- Cart Summary -->
        <mat-card class="cart-summary">
          <mat-card-content>
            <h3>Order Summary</h3>
            <div class="summary-row">
              <span>{{ cart.summary.totalPhotos }} Photos</span>
              <span>{{ cart.summary.totalPrints }} Prints</span>
            </div>
            <div class="summary-row">
              <span>Subtotal:</span>
              <span>{{ cart.summary.subtotal | currency:'USD':'symbol':'1.2-2' }}</span>
            </div>
            <div class="summary-row">
              <span>Estimated Tax:</span>
              <span>{{ cart.summary.tax | currency:'USD':'symbol':'1.2-2' }}</span>
            </div>
            <mat-divider></mat-divider>
            <div class="summary-row total-row">
              <span>Total:</span>
              <span>{{ cart.summary.total | currency:'USD':'symbol':'1.2-2' }}</span>
            </div>
          </mat-card-content>

          <mat-card-actions>
            <button mat-button routerLink="/photos">Continue Shopping</button>
            <button mat-raised-button color="primary"
                    (click)="proceedToCheckout()"
                    [disabled]="cart.items.length === 0 || isLoading">
              Checkout
            </button>
          </mat-card-actions>
        </mat-card>
      </div>

      <!-- Empty Cart -->
      <div class="empty-cart" *ngIf="!cart || cart.items.length === 0">
        <mat-icon class="empty-icon">shopping_cart</mat-icon>
        <h3>Your cart is empty</h3>
        <p>Add some photos to start printing</p>
        <button mat-raised-button color="primary" routerLink="/photos">
          <mat-icon>photo_library</mat-icon>
          Browse Photos
        </button>
      </div>

      <!-- Loading Indicator -->
      <div class="loading-indicator" *ngIf="isLoading && (!cart || cart.items.length === 0)">
        <mat-icon>refresh</mat-icon>
        <p>Loading cart...</p>
      </div>
    </div>
  `,
  styles: [`
    .cart-container {
      max-width: 1000px;
      margin: 0 auto;
      padding: 20px;
    }

    .cart-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
    }

    .cart-header h2 {
      margin: 0;
      color: #333;
    }

    .cart-content {
      display: flex;
      flex-direction: column;
      gap: 20px;
    }

    .cart-item {
      padding: 20px;
      transition: box-shadow 0.2s ease;
    }

    .cart-item:hover {
      box-shadow: 0 4px 8px rgba(0,0,0,0.1);
    }

    .item-layout {
      display: grid;
      grid-template-columns: 200px 1fr auto;
      gap: 20px;
      align-items: start;
    }

    .photo-section {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 8px;
    }

    .photo-thumbnail {
      width: 120px;
      height: 120px;
      object-fit: cover;
      border-radius: 8px;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }

    .photo-info {
      text-align: center;
    }

    .photo-info h4 {
      margin: 0;
      font-size: 0.9em;
      font-weight: 500;
      color: #333;
    }

    .photo-info p {
      margin: 4px 0 0 0;
      color: #666;
      font-size: 0.8em;
    }

    .selections-section {
      flex: 1;
    }

    .selections-section h5 {
      margin: 0 0 12px 0;
      color: #666;
      font-weight: 500;
      font-size: 1em;
    }

    .selection-list {
      margin-bottom: 16px;
    }

    .selection-item {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 8px 0;
      border-bottom: 1px solid #f0f0f0;
    }

    .selection-item:last-child {
      border-bottom: none;
    }

    .selection-info {
      display: flex;
      flex-direction: column;
    }

    .size-name {
      font-weight: 500;
      color: #333;
    }

    .quantity {
      color: #666;
      font-size: 0.9em;
    }

    .selection-total {
      font-weight: 500;
      color: #2e7d32;
    }

    .photo-total {
      text-align: right;
      padding-top: 8px;
      border-top: 2px solid #e0e0e0;
    }

    .actions-section {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .cart-summary {
      position: sticky;
      bottom: 20px;
      background: white;
      box-shadow: 0 -2px 8px rgba(0,0,0,0.1);
    }

    .cart-summary h3 {
      margin: 0 0 16px 0;
      color: #333;
    }

    .summary-row {
      display: flex;
      justify-content: space-between;
      margin-bottom: 8px;
      color: #666;
    }

    .total-row {
      font-size: 1.2em;
      font-weight: 600;
      color: #3f51b5 !important;
      margin-top: 8px;
      padding-top: 8px;
    }

    .empty-cart {
      text-align: center;
      padding: 60px 20px;
      color: #666;
    }

    .empty-icon {
      font-size: 64px;
      width: 64px;
      height: 64px;
      color: #ccc;
      margin-bottom: 16px;
    }

    .empty-cart h3 {
      margin: 16px 0 8px 0;
      color: #666;
    }

    .empty-cart p {
      margin-bottom: 24px;
      color: #999;
    }

    .loading-indicator {
      text-align: center;
      padding: 40px;
      color: #666;
    }

    .loading-indicator mat-icon {
      animation: spin 1s linear infinite;
      font-size: 32px;
      width: 32px;
      height: 32px;
      margin-bottom: 12px;
    }

    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }

    @media (max-width: 768px) {
      .cart-container {
        padding: 16px;
      }

      .item-layout {
        grid-template-columns: 1fr;
        gap: 16px;
      }

      .photo-section {
        flex-direction: row;
        justify-content: flex-start;
        align-items: center;
        text-align: left;
      }

      .photo-thumbnail {
        width: 80px;
        height: 80px;
      }

      .photo-info {
        text-align: left;
        margin-left: 12px;
      }

      .actions-section {
        flex-direction: row;
        justify-content: flex-end;
        gap: 12px;
      }

      .cart-summary {
        position: static;
        margin-top: 20px;
      }
    }

    @media (max-width: 480px) {
      .cart-header {
        flex-direction: column;
        align-items: flex-start;
        gap: 12px;
      }

      .selection-item {
        flex-direction: column;
        align-items: flex-start;
        gap: 4px;
      }

      .selection-total {
        align-self: flex-end;
      }
    }
  `]
})
export class CartComponent implements OnInit {
  cart: Cart | null = null;
  isLoading = false;

  constructor(
    private cartService: CartService, // Service for cart operations
    private router: Router, // For navigation to checkout
    private snackBar: MatSnackBar, // For user feedback messages
    private dialog: MatDialog // For opening edit dialog
  ) {}

  ngOnInit(): void {
    this.loadCart();
    this.subscribeToCartUpdates();
  }

  /**
   * Loads the current cart data from the service.
   * Calls the cart service API endpoint to get updated cart information.
   */
  loadCart(): void {
    this.isLoading = true;
    this.cartService.loadCart().subscribe({
      next: (response) => {
        this.isLoading = false;
        if (response.success) {
          this.cart = response.data;
        }
      },
      error: (error) => {
        this.isLoading = false;
        console.error('Error loading cart:', error);
        this.snackBar.open('Failed to load cart', 'Close', { duration: 3000 });
      }
    });
  }

  /**
   * Subscribes to cart updates from the service.
   * This ensures the component stays in sync with cart changes from other components.
   */
  private subscribeToCartUpdates(): void {
    this.cartService.cart$.subscribe(cart => {
      this.cart = cart;
    });
  }

  /**
   * Opens the print selector dialog to edit an existing cart item.
   * Allows users to modify print size selections for a photo already in cart.
   */
  editItem(item: CartItem): void {
    if (this.isLoading) return;

    // Create a photo object from cart item data for the dialog
    const photoData = {
      id: item.photoId,
      filename: item.photoFilename,
      thumbnailUrl: item.photoThumbnailUrl,
      dimensions: item.photoDimensions
    };

    const dialogRef = this.dialog.open(PrintSelectorComponent, {
      width: '600px',
      data: photoData,
      disableClose: true
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result && result.action === 'added') {
        // Remove old item and add new selections
        this.removeItem(item, false).then(() => {
          this.loadCart(); // Refresh cart after edit
        });
      }
    });
  }

  /**
   * Removes an item from the cart with confirmation.
   * Calls the cart service API to remove the specified item.
   */
  removeItem(item: CartItem, showConfirmation: boolean = true): Promise<void> {
    return new Promise((resolve, reject) => {
      if (showConfirmation && !confirm('Remove this photo from your cart?')) {
        resolve();
        return;
      }

      this.isLoading = true;
      this.cartService.removeFromCart(item.id).subscribe({
        next: (response) => {
          this.isLoading = false;
          if (response.success) {
            this.snackBar.open('Item removed from cart', 'Close', { duration: 3000 });
            this.loadCart();
            resolve();
          } else {
            reject('Failed to remove item');
          }
        },
        error: (error) => {
          this.isLoading = false;
          console.error('Error removing item:', error);
          this.snackBar.open('Failed to remove item', 'Close', { duration: 3000 });
          reject(error);
        }
      });
    });
  }

  /**
   * Clears all items from the cart with confirmation.
   * Calls the cart service API to empty the entire cart.
   */
  clearCart(): void {
    if (!confirm('Clear all items from your cart?')) return;

    this.isLoading = true;
    this.cartService.clearCart().subscribe({
      next: (response) => {
        this.isLoading = false;
        if (response.success) {
          this.snackBar.open('Cart cleared', 'Close', { duration: 3000 });
          this.cart = null;
        }
      },
      error: (error) => {
        this.isLoading = false;
        console.error('Error clearing cart:', error);
        this.snackBar.open('Failed to clear cart', 'Close', { duration: 3000 });
      }
    });
  }

  /**
   * Navigates to the checkout page.
   * Validates that cart has items before proceeding.
   */
  proceedToCheckout(): void {
    if (!this.cart || this.cart.items.length === 0) {
      this.snackBar.open('Your cart is empty', 'Close', { duration: 3000 });
      return;
    }

    this.router.navigate(['/checkout']);
  }

  /**
   * TrackBy function for cart items to improve performance.
   * Helps Angular track items in ngFor loops efficiently.
   */
  trackByItemId(index: number, item: CartItem): string {
    return item.id;
  }

  /**
   * TrackBy function for print selections within items.
   * Improves performance for nested ngFor loops.
   */
  trackBySelectionSize(index: number, selection: any): string {
    return selection.sizeCode;
  }
}
