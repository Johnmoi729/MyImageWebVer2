import { Component, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute } from '@angular/router';
import { OrderService } from '../../../core/services/order.service';

@Component({
  selector: 'app-order-detail',
  template: `
    <div class="order-detail-container" *ngIf="order">
      <!-- Order Header -->
      <div class="order-header">
        <div class="header-info">
          <h2>Order {{ order.orderNumber }}</h2>
          <mat-chip [class]="getStatusClass(order.status)">
            {{ getStatusLabel(order.status) }}
          </mat-chip>
        </div>
        <button mat-button routerLink="/orders">
          <mat-icon>arrow_back</mat-icon>
          Back to Orders
        </button>
      </div>

      <div class="order-content">
        <!-- Order Info -->
        <mat-card class="order-info-card">
          <mat-card-header>
            <mat-card-title>Order Information</mat-card-title>
          </mat-card-header>

          <mat-card-content>
            <div class="info-grid">
              <div class="info-item">
                <span class="label">Order Date:</span>
                <span>{{ formatDateTime(order.orderDate) }}</span>
              </div>

              <div class="info-item">
                <span class="label">Payment Method:</span>
                <div class="payment-info">
                  <mat-icon>{{ getPaymentIcon(order.paymentMethod) }}</mat-icon>
                  {{ getPaymentLabel(order.paymentMethod) }}
                </div>
              </div>

              <div class="info-item">
                <span class="label">Total Amount:</span>
                <span class="amount">\${{ order.totalAmount.toFixed(2) }}</span>
              </div>

              <div class="info-item" *ngIf="order.trackingNumber">
                <span class="label">Tracking Number:</span>
                <span class="tracking">{{ order.trackingNumber }}</span>
              </div>
            </div>
          </mat-card-content>
        </mat-card>

        <!-- Order Items -->
        <mat-card class="order-items-card">
          <mat-card-header>
            <mat-card-title>Order Items</mat-card-title>
            <mat-card-subtitle>
              {{ order.photoCount }} photos, {{ order.printCount }} prints
            </mat-card-subtitle>
          </mat-card-header>

          <mat-card-content>
            <div class="items-list" *ngIf="order.items">
              <mat-expansion-panel *ngFor="let item of order.items" class="item-panel">
                <mat-expansion-panel-header>
                  <mat-panel-title>
                    <div class="item-summary">
                      <img [src]="item.photoThumbnailUrl"
                           [alt]="item.photoFilename"
                           class="item-thumbnail">
                      <div class="item-info">
                        <span class="item-name">{{ item.photoFilename }}</span>
                        <span class="item-subtitle">
                          {{ item.printSelections.length }} size(s) - \${{ item.photoTotal.toFixed(2) }}
                        </span>
                      </div>
                    </div>
                  </mat-panel-title>
                </mat-expansion-panel-header>

                <div class="item-details">
                  <h4>Print Selections:</h4>
                  <div class="selections-list">
                    <div class="selection-item" *ngFor="let selection of item.printSelections">
                      <div class="selection-info">
                        <span class="size-name">{{ selection.sizeName }}</span>
                        <span class="quantity">Quantity: {{ selection.quantity }}</span>
                      </div>
                      <div class="selection-pricing">
                        <span class="unit-price">\${{ selection.unitPrice.toFixed(2) }} each</span>
                        <span class="line-total">\${{ selection.lineTotal.toFixed(2) }}</span>
                      </div>
                    </div>
                  </div>
                </div>
              </mat-expansion-panel>
            </div>
          </mat-card-content>
        </mat-card>

        <!-- Shipping Information -->
        <mat-card class="shipping-card" *ngIf="order.shippingAddress">
          <mat-card-header>
            <mat-card-title>Shipping Address</mat-card-title>
          </mat-card-header>

          <mat-card-content>
            <div class="address-info">
              <p class="recipient-name">{{ order.shippingAddress.fullName }}</p>
              <p>{{ order.shippingAddress.streetLine1 }}</p>
              <p *ngIf="order.shippingAddress.streetLine2">{{ order.shippingAddress.streetLine2 }}</p>
              <p>
                {{ order.shippingAddress.city }}, {{ order.shippingAddress.state }}
                {{ order.shippingAddress.postalCode }}
              </p>
              <p>{{ order.shippingAddress.country }}</p>
              <p class="phone">Phone: {{ order.shippingAddress.phone }}</p>
            </div>
          </mat-card-content>
        </mat-card>

        <!-- Order Timeline -->
        <mat-card class="timeline-card" *ngIf="order.timeline">
          <mat-card-header>
            <mat-card-title>Order Timeline</mat-card-title>
          </mat-card-header>

          <mat-card-content>
            <div class="timeline">
              <div class="timeline-item" *ngFor="let event of order.timeline">
                <div class="timeline-marker"></div>
                <div class="timeline-content">
                  <h4>{{ event.title }}</h4>
                  <p>{{ event.description }}</p>
                  <span class="timeline-date">{{ formatDateTime(event.date) }}</span>
                </div>
              </div>
            </div>
          </mat-card-content>
        </mat-card>

        <!-- Pricing Breakdown -->
        <mat-card class="pricing-card">
          <mat-card-header>
            <mat-card-title>Pricing Breakdown</mat-card-title>
          </mat-card-header>

          <mat-card-content>
            <div class="pricing-details">
              <div class="pricing-row">
                <span>Subtotal:</span>
                <span>\${{ order.pricing?.subtotal.toFixed(2) }}</span>
              </div>

              <div class="pricing-row">
                <span>Tax ({{ (order.pricing?.taxRate * 100).toFixed(2) }}%):</span>
                <span>\${{ order.pricing?.taxAmount.toFixed(2) }}</span>
              </div>

              <mat-divider></mat-divider>

              <div class="pricing-row total-row">
                <span>Total:</span>
                <span>\${{ order.pricing?.total.toFixed(2) }}</span>
              </div>
            </div>
          </mat-card-content>
        </mat-card>
      </div>
    </div>

    <!-- Loading -->
    <div class="loading-container" *ngIf="!order && isLoading">
      <mat-spinner></mat-spinner>
      <p>Loading order details...</p>
    </div>

    <!-- Error State -->
    <div class="error-state" *ngIf="!order && !isLoading">
      <mat-icon class="error-icon">error</mat-icon>
      <h3>Order Not Found</h3>
      <p>The order you're looking for doesn't exist or you don't have access to it.</p>
      <button mat-raised-button color="primary" routerLink="/orders">
        Back to Orders
      </button>
    </div>
  `,
  styles: [`
    .order-detail-container {
      max-width: 1000px;
      margin: 0 auto;
      padding: 20px;
    }

    .order-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 30px;
    }

    .header-info {
      display: flex;
      align-items: center;
      gap: 16px;
    }

    .order-content {
      display: flex;
      flex-direction: column;
      gap: 20px;
    }

    .info-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 16px;
    }

    .info-item {
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .label {
      font-weight: 500;
      color: #666;
    }

    .payment-info {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .amount {
      font-size: 1.2em;
      font-weight: 600;
      color: #2e7d32;
    }

    .tracking {
      font-family: monospace;
      background: #f5f5f5;
      padding: 4px 8px;
      border-radius: 4px;
    }

    .item-panel {
      margin-bottom: 8px;
    }

    .item-summary {
      display: flex;
      align-items: center;
      gap: 12px;
      width: 100%;
    }

    .item-thumbnail {
      width: 50px;
      height: 50px;
      object-fit: cover;
      border-radius: 4px;
    }

    .item-info {
      display: flex;
      flex-direction: column;
    }

    .item-name {
      font-weight: 500;
    }

    .item-subtitle {
      color: #666;
      font-size: 0.9em;
    }

    .item-details {
      padding: 16px 0;
    }

    .selections-list {
      margin-top: 8px;
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
    }

    .quantity {
      color: #666;
      font-size: 0.9em;
    }

    .selection-pricing {
      display: flex;
      flex-direction: column;
      align-items: flex-end;
    }

    .unit-price {
      color: #666;
      font-size: 0.9em;
    }

    .line-total {
      font-weight: 500;
      color: #2e7d32;
    }

    .address-info p {
      margin: 4px 0;
    }

    .recipient-name {
      font-weight: 500;
      font-size: 1.1em;
    }

    .phone {
      color: #666;
      margin-top: 8px;
    }

    .timeline {
      position: relative;
      padding-left: 30px;
    }

    .timeline-item {
      position: relative;
      margin-bottom: 20px;
    }

    .timeline-marker {
      position: absolute;
      left: -30px;
      top: 0;
      width: 12px;
      height: 12px;
      background: #3f51b5;
      border-radius: 50%;
      border: 3px solid #fff;
      box-shadow: 0 0 0 2px #3f51b5;
    }

    .timeline-content h4 {
      margin: 0 0 4px 0;
      font-weight: 500;
    }

    .timeline-content p {
      margin: 0 0 8px 0;
      color: #666;
    }

    .timeline-date {
      color: #999;
      font-size: 0.9em;
    }

    .pricing-details {
      min-width: 300px;
    }

    .pricing-row {
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

    /* Status chip styles - same as order list */
    .status-pending {
      background-color: #fff3e0;
      color: #e65100;
    }

    .status-verified {
      background-color: #e3f2fd;
      color: #1565c0;
    }

    .status-processing {
      background-color: #f3e5f5;
      color: #7b1fa2;
    }

    .status-printed {
      background-color: #e8f5e8;
      color: #2e7d32;
    }

    .status-shipped {
      background-color: #e1f5fe;
      color: #0277bd;
    }

    .status-completed {
      background-color: #e8f5e8;
      color: #1b5e20;
    }

    .loading-container, .error-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 60px 20px;
      text-align: center;
    }

    .error-icon {
      font-size: 64px;
      width: 64px;
      height: 64px;
      color: #f44336;
      margin-bottom: 16px;
    }

    @media (max-width: 768px) {
      .order-header {
        flex-direction: column;
        gap: 16px;
        align-items: stretch;
      }

      .info-grid {
        grid-template-columns: 1fr;
      }

      .item-summary {
        flex-direction: column;
        align-items: flex-start;
      }
    }
  `]
})
export class OrderDetailComponent implements OnInit {
  order: any = null;
  isLoading = false;

  constructor(
    private route: ActivatedRoute,
    private orderService: OrderService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      if (params['id']) {
        this.loadOrderDetail(params['id']);
      }
    });
  }

  private loadOrderDetail(orderId: string): void {
    this.isLoading = true;

    this.orderService.getOrderDetails(orderId).subscribe({
      next: (response) => {
        this.isLoading = false;
        if (response.success) {
          this.order = response.data;
        }
      },
      error: (error) => {
        this.isLoading = false;
        this.snackBar.open('Failed to load order details', 'Close', { duration: 3000 });
      }
    });
  }

  formatDateTime(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
  }

  getStatusLabel(status: string): string {
    const statusLabels: { [key: string]: string } = {
      'pending': 'Pending Payment',
      'payment_verified': 'Payment Verified',
      'processing': 'Processing',
      'printed': 'Printed',
      'shipped': 'Shipped',
      'completed': 'Completed'
    };
    return statusLabels[status] || status;
  }

  getStatusClass(status: string): string {
    return `status-${status.replace('_', '-')}`;
  }

  getPaymentIcon(paymentMethod: string): string {
    return paymentMethod === 'credit_card' ? 'credit_card' : 'store';
  }

  getPaymentLabel(paymentMethod: string): string {
    return paymentMethod === 'credit_card' ? 'Credit Card' : 'Branch Payment';
  }
}
