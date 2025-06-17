import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { CartService } from '../../../core/services/cart.service';
import { OrderService } from '../../../core/services/order.service';
import { Cart } from '../../../shared/models/cart.models';

// Fix for missing interfaces - define locally to resolve import error
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

      <div class="checkout-layout" *ngIf="cart">
        <!-- Checkout Form -->
        <div class="checkout-form">
          <mat-stepper [linear]="true" #stepper>
            <!-- Shipping Address Step -->
            <mat-step [stepControl]="shippingForm" label="Shipping Address">
              <form [formGroup]="shippingForm" class="step-form">
                <div class="form-row">
                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>Full Name</mat-label>
                    <input matInput formControlName="fullName">
                    <mat-error>Full name is required</mat-error>
                  </mat-form-field>
                </div>

                <div class="form-row">
                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>Street Address</mat-label>
                    <input matInput formControlName="streetLine1">
                    <mat-error>Street address is required</mat-error>
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
                    <input matInput formControlName="city">
                    <mat-error>City is required</mat-error>
                  </mat-form-field>

                  <mat-form-field appearance="outline" class="quarter-width">
                    <mat-label>State</mat-label>
                    <mat-select formControlName="state">
                      <mat-option value="MA">Massachusetts</mat-option>
                      <mat-option value="NH">New Hampshire</mat-option>
                      <mat-option value="NY">New York</mat-option>
                      <mat-option value="CT">Connecticut</mat-option>
                    </mat-select>
                    <mat-error>State is required</mat-error>
                  </mat-form-field>

                  <mat-form-field appearance="outline" class="quarter-width">
                    <mat-label>ZIP Code</mat-label>
                    <input matInput formControlName="postalCode">
                    <mat-error>ZIP code is required</mat-error>
                  </mat-form-field>
                </div>

                <div class="form-row">
                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>Phone Number</mat-label>
                    <input matInput formControlName="phone" type="tel">
                    <mat-error>Phone number is required</mat-error>
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
                             maxlength="19">
                      <mat-error>Valid card number is required</mat-error>
                    </mat-form-field>
                  </div>

                  <div class="form-row">
                    <mat-form-field appearance="outline" class="half-width">
                      <mat-label>Expiry Date</mat-label>
                      <input matInput formControlName="expiryDate"
                             placeholder="MM/YY"
                             maxlength="5">
                      <mat-error>Valid expiry date is required</mat-error>
                    </mat-form-field>

                    <mat-form-field appearance="outline" class="half-width">
                      <mat-label>CVV</mat-label>
                      <input matInput formControlName="cvv"
                             type="password"
                             maxlength="4">
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
                    <li>Boston Downtown - 123 Main St, Boston, MA 02101</li>
                    <li>Cambridge Center - 456 Tech Blvd, Cambridge, MA 02139</li>
                  </ul>
                  <p><strong>Reference Number:</strong> Will be provided after order confirmation</p>
                </div>

                <div class="step-actions">
                  <button mat-button matStepperPrevious>Back</button>
                  <button mat-raised-button color="primary"
                          [disabled]="paymentForm.invalid || isProcessing"
                          (click)="placeOrder()">
                    <mat-spinner diameter="20" *ngIf="isProcessing"></mat-spinner>
                    <span *ngIf="!isProcessing">Place Order</span>
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
                  <span class="item-details">{{ item.printSelections.length }} size(s)</span>
                </div>
                <span class="item-total">{{ item.photoTotal.toFixed(2) }}</span>
              </div>

              <mat-divider></mat-divider>

              <div class="summary-row">
                <span>Subtotal:</span>
                <span>{{ cart.summary.subtotal.toFixed(2) }}</span>
              </div>

              <div class="summary-row">
                <span>Tax:</span>
                <span>{{ finalTax.toFixed(2) }}</span>
              </div>

              <div class="summary-row total-row">
                <span>Total:</span>
                <span>{{ finalTotal.toFixed(2) }}</span>
              </div>
            </mat-card-content>
          </mat-card>
        </div>
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
    }
  `]
})
export class CheckoutComponent implements OnInit {
  cart: Cart | null = null;
  shippingForm: FormGroup;
  paymentForm: FormGroup;
  isProcessing = false;
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
      fullName: ['', Validators.required],
      streetLine1: ['', Validators.required],
      streetLine2: [''],
      city: ['', Validators.required],
      state: ['', Validators.required],
      postalCode: ['', Validators.required],
      phone: ['', Validators.required]
    });

    this.paymentForm = this.fb.group({
      paymentMethod: ['credit_card', Validators.required],
      cardholderName: [''],
      cardNumber: [''],
      expiryDate: [''],
      cvv: ['']
    });
  }

  ngOnInit(): void {
    this.loadCart();
    this.setupPaymentValidation();
  }

  private loadCart(): void {
    this.cartService.cart$.subscribe(cart => {
      this.cart = cart;
      if (cart) {
        this.finalTax = cart.summary.tax;
        this.finalTotal = cart.summary.total;
      }
    });
  }

  private setupPaymentValidation(): void {
    this.paymentForm.get('paymentMethod')?.valueChanges.subscribe(method => {
      const cardFields = ['cardholderName', 'cardNumber', 'expiryDate', 'cvv'];

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

  calculateFinalTotal(): void {
    const shippingData = this.shippingForm.value;
    if (this.cart && shippingData.state && shippingData.postalCode) {
      this.cartService.calculateTotal(
        this.cart.summary.subtotal,
        shippingData.state,
        shippingData.postalCode
      ).subscribe({
        next: (response) => {
          if (response.success) {
            this.finalTax = response.data.taxAmount;
            this.finalTotal = response.data.total;
          }
        }
      });
    }
  }

  placeOrder(): void {
    if (this.shippingForm.invalid || this.paymentForm.invalid) {
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
      creditCard = {
        encryptedCardNumber: 'encrypted_' + this.paymentForm.get('cardNumber')?.value,
        cardholderName: this.paymentForm.get('cardholderName')?.value,
        expiryMonth: this.paymentForm.get('expiryDate')?.value?.split('/')[0],
        expiryYear: '20' + this.paymentForm.get('expiryDate')?.value?.split('/')[1],
        encryptedCvv: 'encrypted_' + this.paymentForm.get('cvv')?.value
      };
    }

    this.orderService.createOrder(shippingAddress, paymentMethod, creditCard).subscribe({
      next: (response) => {
        this.isProcessing = false;
        if (response.success) {
          this.snackBar.open('Order placed successfully!', 'Close', { duration: 5000 });
          this.router.navigate(['/orders', response.data.orderId]);
        }
      },
      error: (error) => {
        this.isProcessing = false;
        this.snackBar.open('Failed to place order. Please try again.', 'Close', { duration: 5000 });
      }
    });
  }
}
