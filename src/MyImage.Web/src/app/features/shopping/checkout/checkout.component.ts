// src/MyImage.Web/src/app/features/shopping/checkout/checkout.component.ts
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { CartService } from '../../../core/services/cart.service';
import { OrderService } from '../../../core/services/order.service';
import { Cart } from '../../../shared/models/cart.models';

// Define interfaces locally to resolve import errors
interface ShippingAddress {
  fullName: string;
  streetLine1: string;
  streetLine2?: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  phone: string;
}

@Component({
  selector: 'app-checkout',
  standalone: false,
  template: `
    <div class="checkout-container">
      <h2>Checkout</h2>

      <!-- Cart Empty State -->
      <div class="empty-cart" *ngIf="!cart || cart.items.length === 0">
        <mat-icon class="empty-icon">shopping_cart</mat-icon>
        <h3>Your cart is empty</h3>
        <p>Add some photos to your cart before checking out</p>
        <button mat-raised-button color="primary" routerLink="/photos">
          Browse Photos
        </button>
      </div>

      <!-- Checkout Process -->
      <div class="checkout-layout" *ngIf="cart && cart.items.length > 0">
        <!-- Checkout Form -->
        <div class="checkout-form">
          <mat-stepper [linear]="true" #stepper>
            <!-- Shipping Address Step -->
            <mat-step [stepControl]="shippingForm" label="Shipping Address">
              <form [formGroup]="shippingForm" class="step-form">
                <div class="form-row">
                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>Full Name</mat-label>
                    <input matInput formControlName="fullName" required>
                    <mat-error *ngIf="shippingForm.get('fullName')?.hasError('required')">
                      Full name is required
                    </mat-error>
                  </mat-form-field>
                </div>

                <div class="form-row">
                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>Street Address</mat-label>
                    <input matInput formControlName="streetLine1" required>
                    <mat-error *ngIf="shippingForm.get('streetLine1')?.hasError('required')">
                      Street address is required
                    </mat-error>
                  </mat-form-field>
                </div>

                <div class="form-row">
                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>Apartment, suite, etc. (optional)</mat-label>
                    <input matInput formControlName="streetLine2">
                  </mat-form-field>
                </div>

                <div class="form-row">
                  <mat-form-field appearance="outline" class="half-width">
                    <mat-label>City</mat-label>
                    <input matInput formControlName="city" required>
                    <mat-error *ngIf="shippingForm.get('city')?.hasError('required')">
                      City is required
                    </mat-error>
                  </mat-form-field>

                  <mat-form-field appearance="outline" class="quarter-width">
                    <mat-label>State</mat-label>
                    <mat-select formControlName="state" required>
                      <mat-option value="MA">Massachusetts</mat-option>
                      <mat-option value="NH">New Hampshire</mat-option>
                      <mat-option value="NY">New York</mat-option>
                      <mat-option value="CT">Connecticut</mat-option>
                      <mat-option value="RI">Rhode Island</mat-option>
                      <mat-option value="VT">Vermont</mat-option>
                    </mat-select>
                    <mat-error *ngIf="shippingForm.get('state')?.hasError('required')">
                      State is required
                    </mat-error>
                  </mat-form-field>

                  <mat-form-field appearance="outline" class="quarter-width">
                    <mat-label>ZIP Code</mat-label>
                    <input matInput formControlName="postalCode" required
                           pattern="[0-9]{5}(-[0-9]{4})?">
                    <mat-error *ngIf="shippingForm.get('postalCode')?.hasError('required')">
                      ZIP code is required
                    </mat-error>
                    <mat-error *ngIf="shippingForm.get('postalCode')?.hasError('pattern')">
                      Invalid ZIP code format
                    </mat-error>
                  </mat-form-field>
                </div>

                <div class="form-row">
                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>Phone Number</mat-label>
                    <input matInput formControlName="phone" type="tel" required
                           pattern="[\+]?[1-9][\d]{0,15}">
                    <mat-error *ngIf="shippingForm.get('phone')?.hasError('required')">
                      Phone number is required
                    </mat-error>
                    <mat-error *ngIf="shippingForm.get('phone')?.hasError('pattern')">
                      Invalid phone number format
                    </mat-error>
                  </mat-form-field>
                </div>

                <div class="step-actions">
                  <button mat-raised-button color="primary" matStepperNext
                          [disabled]="shippingForm.invalid"
                          (click)="calculateFinalTotal()">
                    Continue to Payment
                  </button>
                </div>
              </form>
            </mat-step>

            <!-- Payment Method Step -->
            <mat-step [stepControl]="paymentForm" label="Payment Method">
              <form [formGroup]="paymentForm" class="step-form">
                <div class="payment-methods">
                  <mat-radio-group formControlName="paymentMethod" class="payment-options">
                    <mat-radio-button value="credit_card" class="payment-option">
                      <div class="payment-info">
                        <div class="payment-title">
                          <mat-icon>credit_card</mat-icon>
                          Credit Card
                        </div>
                        <p>Secure online payment</p>
                      </div>
                    </mat-radio-button>

                    <mat-radio-button value="branch_payment" class="payment-option">
                      <div class="payment-info">
                        <div class="payment-title">
                          <mat-icon>store</mat-icon>
                          Pay at Branch
                        </div>
                        <p>Pay in person at our physical location</p>
                      </div>
                    </mat-radio-button>
                  </mat-radio-group>
                </div>

                <!-- Credit Card Fields -->
                <div class="credit-card-section"
                     *ngIf="paymentForm.get('paymentMethod')?.value === 'credit_card'">
                  <h4>Credit Card Information</h4>
                  <div class="form-row">
                    <mat-form-field appearance="outline" class="full-width">
                      <mat-label>Cardholder Name</mat-label>
                      <input matInput formControlName="cardholderName">
                      <mat-error>Cardholder name is required</mat-error>
                    </mat-form-field>
                  </div>

                  <div class="form-row">
                    <mat-form-field appearance="outline" class="full-width">
                      <mat-label>Card Number</mat-label>
                      <input matInput formControlName="cardNumber"
                             placeholder="1234 5678 9012 3456"
                             maxlength="19"
                             (input)="formatCardNumber($event)">
                      <mat-error>Valid card number is required</mat-error>
                    </mat-form-field>
                  </div>

                  <div class="form-row">
                    <mat-form-field appearance="outline" class="half-width">
                      <mat-label>Expiry Date</mat-label>
                      <input matInput formControlName="expiryDate"
                             placeholder="MM/YY"
                             maxlength="5"
                             (input)="formatExpiryDate($event)">
                      <mat-error>Valid expiry date is required</mat-error>
                    </mat-form-field>

                    <mat-form-field appearance="outline" class="half-width">
                      <mat-label>CVV</mat-label>
                      <input matInput formControlName="cvv"
                             type="password"
                             maxlength="4"
                             pattern="[0-9]{3,4}">
                      <mat-error>CVV is required</mat-error>
                    </mat-form-field>
                  </div>
                </div>

                <!-- Branch Payment Info -->
                <div class="branch-payment-section"
                     *ngIf="paymentForm.get('paymentMethod')?.value === 'branch_payment'">
                  <h4>Branch Payment</h4>
                  <p>You can pay for your order at any of our branch locations:</p>
                  <ul class="branch-list">
                    <li><strong>Boston Downtown</strong><br>123 Main St, Boston, MA 02101<br>Phone: (617) 555-0100</li>
                    <li><strong>Cambridge Center</strong><br>456 Tech Blvd, Cambridge, MA 02139<br>Phone: (617) 555-0200</li>
                  </ul>
                  <p><strong>Reference Number:</strong> Will be provided after order confirmation</p>
                </div>

                <div class="step-actions">
                  <button mat-button matStepperPrevious>Back</button>
                  <button mat-raised-button color="primary"
                          [disabled]="!isFormValid() || isProcessing"
                          (click)="placeOrder()">
                    <mat-spinner diameter="20" *ngIf="isProcessing"></mat-spinner>
                    <span *ngIf="!isProcessing">Place Order - {{ finalTotal | currency:'USD':'symbol':'1.2-2' }}</span>
                  </button>
                </div>
              </form>
            </mat-step>
          </mat-stepper>
        </div>

        <!-- Order Summary -->
        <div class="order-summary">
          <mat-card>
            <mat-card-header>
              <mat-card-title>Order Summary</mat-card-title>
            </mat-card-header>

            <mat-card-content>
              <div class="summary-item" *ngFor="let item of cart.items">
                <div class="item-info">
                  <span class="item-name">{{ item.photoFilename }}</span>
                  <span class="item-details">{{ getTotalPrintsForItem(item) }} prints</span>
                </div>
                <span class="item-total">{{ item.photoTotal | currency:'USD':'symbol':'1.2-2' }}</span>
              </div>

              <mat-divider></mat-divider>

              <div class="summary-row">
                <span>Subtotal:</span>
                <span>{{ cart.summary.subtotal | currency:'USD':'symbol':'1.2-2' }}</span>
              </div>

              <div class="summary-row">
                <span>Tax:</span>
                <span>{{ finalTax | currency:'USD':'symbol':'1.2-2' }}</span>
              </div>

              <div class="summary-row total-row">
                <span>Total:</span>
                <span>{{ finalTotal | currency:'USD':'symbol':'1.2-2' }}</span>
              </div>
            </mat-card-content>
          </mat-card>
        </div>
      </div>

      <!-- Loading State -->
      <div class="loading-state" *ngIf="isLoadingCart">
        <mat-spinner diameter="40"></mat-spinner>
        <p>Loading cart...</p>
      </div>
    </div>
  `,
  styles: [`
    .checkout-container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 20px;
    }

    .checkout-layout {
      display: grid;
      grid-template-columns: 2fr 1fr;
      gap: 40px;
      margin-top: 20px;
    }

    .step-form {
      padding: 20px 0;
    }

    .form-row {
      display: flex;
      gap: 16px;
      margin-bottom: 16px;
    }

    .full-width {
      width: 100%;
    }

    .half-width {
      flex: 1;
    }

    .quarter-width {
      flex: 0.5;
    }

    .payment-options {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .payment-option {
      padding: 16px;
      border: 1px solid #ddd;
      border-radius: 8px;
      width: 100%;
    }

    .payment-option.mat-radio-checked {
      border-color: #3f51b5;
      background-color: #f5f5ff;
    }

    .payment-info {
      margin-left: 8px;
    }

    .payment-title {
      display: flex;
      align-items: center;
      gap: 8px;
      font-weight: 500;
      margin-bottom: 4px;
    }

    .credit-card-section, .branch-payment-section {
      margin-top: 20px;
      padding: 20px;
      background: #fafafa;
      border-radius: 8px;
    }

    .branch-list {
      margin: 12px 0;
      padding-left: 20px;
    }

    .branch-list li {
      margin-bottom: 12px;
    }

    .step-actions {
      display: flex;
      justify-content: flex-end;
      gap: 16px;
      margin-top: 20px;
    }

    .order-summary {
      position: sticky;
      top: 20px;
      height: fit-content;
    }

    .summary-item {
      display: flex;
      justify-content: space-between;
      margin-bottom: 12px;
    }

    .item-info {
      display: flex;
      flex-direction: column;
    }

    .item-name {
      font-weight: 500;
    }

    .item-details {
      color: #666;
      font-size: 0.9em;
    }

    .summary-row {
      display: flex;
      justify-content: space-between;
      margin: 8px 0;
    }

    .total-row {
      font-size: 1.2em;
      font-weight: 600;
      color: #3f51b5;
      margin-top: 16px;
      padding-top: 16px;
      border-top: 1px solid #eee;
    }

    .empty-cart, .loading-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 60px 20px;
      text-align: center;
    }

    .empty-icon {
      font-size: 64px;
      width: 64px;
      height: 64px;
      color: #ccc;
      margin-bottom: 16px;
    }

    @media (max-width: 768px) {
      .checkout-layout {
        grid-template-columns: 1fr;
        gap: 20px;
      }

      .form-row {
        flex-direction: column;
      }

      .quarter-width, .half-width {
        width: 100%;
      }

      .order-summary {
        position: static;
      }
    }
  `]
})
export class CheckoutComponent implements OnInit {
  cart: Cart | null = null;
  shippingForm: FormGroup;
  paymentForm: FormGroup;
  isProcessing = false;
  isLoadingCart = false;
  finalTax = 0;
  finalTotal = 0;

  constructor(
    private fb: FormBuilder,
    private cartService: CartService,
    private orderService: OrderService,
    private router: Router,
    private snackBar: MatSnackBar
  ) {
    this.shippingForm = this.fb.group({
      fullName: ['', [Validators.required]],
      streetLine1: ['', [Validators.required]],
      streetLine2: [''],
      city: ['', [Validators.required]],
      state: ['', [Validators.required]],
      postalCode: ['', [Validators.required, Validators.pattern(/^[0-9]{5}(-[0-9]{4})?$/)]],
      phone: ['', [Validators.required, Validators.pattern(/^[\+]?[1-9][\d]{0,15}$/)]]
    });

    this.paymentForm = this.fb.group({
      paymentMethod: ['credit_card', [Validators.required]],
      cardholderName: [''],
      cardNumber: [''],
      expiryDate: [''],
      cvv: ['']
    });
  }

  ngOnInit(): void {
    console.log('Checkout - Component initialized');
    this.loadCart();
    this.setupPaymentValidation();
  }

  private loadCart(): void {
    this.isLoadingCart = true;

    this.cartService.cart$.subscribe({
      next: (cart) => {
        console.log('Checkout - Cart loaded:', cart);
        this.cart = cart;
        this.isLoadingCart = false;

        if (cart && cart.items.length > 0) {
          this.finalTax = cart.summary.tax;
          this.finalTotal = cart.summary.total;
        } else {
          console.log('Checkout - Cart is empty, user should not be here');
        }
      },
      error: (error) => {
        console.error('Checkout - Error loading cart:', error);
        this.isLoadingCart = false;
        this.snackBar.open('Failed to load cart', 'Close', { duration: 3000 });
      }
    });

    // Also trigger a fresh cart load
    this.cartService.loadCart().subscribe();
  }

  private setupPaymentValidation(): void {
    this.paymentForm.get('paymentMethod')?.valueChanges.subscribe(method => {
      const cardFields = ['cardholderName', 'cardNumber', 'expiryDate', 'cvv'];

      console.log('Checkout - Payment method changed to:', method);

      if (method === 'credit_card') {
        cardFields.forEach(field => {
          this.paymentForm.get(field)?.setValidators([Validators.required]);
        });
      } else {
        cardFields.forEach(field => {
          this.paymentForm.get(field)?.clearValidators();
        });
      }

      cardFields.forEach(field => {
        this.paymentForm.get(field)?.updateValueAndValidity();
      });
    });
  }

  isFormValid(): boolean {
    const shippingValid = this.shippingForm.valid;
    const paymentValid = this.paymentForm.valid;

    console.log('Checkout - Form validation:', {
      shipping: shippingValid,
      payment: paymentValid,
      overall: shippingValid && paymentValid
    });

    return shippingValid && paymentValid;
  }

  calculateFinalTotal(): void {
    const shippingData = this.shippingForm.value;
    console.log('Checkout - Calculating final total with shipping data:', shippingData);

    if (this.cart && shippingData.state && shippingData.postalCode) {
      this.cartService.calculateTotal(
        this.cart.summary.subtotal,
        shippingData.state,
        shippingData.postalCode
      ).subscribe({
        next: (response) => {
          console.log('Checkout - Tax calculation response:', response);
          if (response.success && response.data) {
            this.finalTax = response.data.taxAmount;
            this.finalTotal = response.data.total;
          }
        },
        error: (error) => {
          console.error('Checkout - Error calculating tax:', error);
          this.snackBar.open('Failed to calculate tax', 'Close', { duration: 3000 });
        }
      });
    }
  }

  placeOrder(): void {
    console.log('Checkout - Place order clicked');

    if (!this.isFormValid()) {
      console.error('Checkout - Form is invalid');
      this.snackBar.open('Please complete all required fields', 'Close', { duration: 3000 });
      return;
    }

    if (!this.cart || this.cart.items.length === 0) {
      console.error('Checkout - No cart or empty cart');
      this.snackBar.open('Your cart is empty', 'Close', { duration: 3000 });
      return;
    }

    this.isProcessing = true;

    const shippingAddress: ShippingAddress = {
      ...this.shippingForm.value,
      country: 'USA'
    };

    const paymentMethod = this.paymentForm.get('paymentMethod')?.value;
    let creditCard = null;

    if (paymentMethod === 'credit_card') {
      const cardNumber = this.paymentForm.get('cardNumber')?.value;
      const expiryDate = this.paymentForm.get('expiryDate')?.value;
      const cvv = this.paymentForm.get('cvv')?.value;

      creditCard = {
        encryptedCardNumber: 'encrypted_' + cardNumber,
        cardholderName: this.paymentForm.get('cardholderName')?.value,
        expiryMonth: expiryDate?.split('/')[0] || '',
        expiryYear: '20' + (expiryDate?.split('/')[1] || ''),
        encryptedCvv: 'encrypted_' + cvv
      };
    }

    console.log('Checkout - Creating order with data:', {
      shippingAddress,
      paymentMethod,
      creditCard: creditCard ? { ...creditCard, encryptedCardNumber: '[ENCRYPTED]', encryptedCvv: '[ENCRYPTED]' } : null
    });

    this.orderService.createOrder(shippingAddress, paymentMethod, creditCard).subscribe({
      next: (response) => {
        console.log('Checkout - Order creation response:', response);
        this.isProcessing = false;

        if (response.success && response.data) {
          this.snackBar.open('Order placed successfully!', 'Close', { duration: 5000 });

          // Navigate to order details
          console.log('Checkout - Navigating to order:', response.data.orderId);
          this.router.navigate(['/orders', response.data.orderId]);
        } else {
          console.error('Checkout - Order creation failed:', response);
          this.snackBar.open(response.message || 'Failed to place order', 'Close', { duration: 5000 });
        }
      },
      error: (error) => {
        console.error('Checkout - Order creation error:', error);
        this.isProcessing = false;

        let errorMessage = 'Failed to place order';
        if (error.status === 400) {
          errorMessage += ' - Invalid order data';
        } else if (error.status === 401) {
          errorMessage += ' - Please log in again';
          this.router.navigate(['/auth/login']);
          return;
        } else if (error.status === 404) {
          errorMessage += ' - Service not available';
        } else if (error.status >= 500) {
          errorMessage += ' - Server error';
        }

        this.snackBar.open(errorMessage, 'Close', { duration: 5000 });
      }
    });
  }

  // Helper methods for formatting
  formatCardNumber(event: any): void {
    let value = event.target.value.replace(/\s/g, '');
    let formattedValue = value.replace(/(.{4})/g, '$1 ').trim();
    event.target.value = formattedValue;
    this.paymentForm.get('cardNumber')?.setValue(formattedValue);
  }

  formatExpiryDate(event: any): void {
    let value = event.target.value.replace(/\D/g, '');
    if (value.length >= 2) {
      value = value.substring(0, 2) + '/' + value.substring(2, 4);
    }
    event.target.value = value;
    this.paymentForm.get('expiryDate')?.setValue(value);
  }

  getTotalPrintsForItem(item: any): number {
    return item.printSelections.reduce((total: number, selection: any) => total + selection.quantity, 0);
  }
}
