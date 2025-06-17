import { Component, OnInit } from '@angular/core';
import { PageEvent } from '@angular/material/paginator';
import { MatSnackBar } from '@angular/material/snack-bar';
import { OrderService } from '../../../core/services/order.service';
import { Order } from '../../../shared/models/order.models';

@Component({
  selector: 'app-order-list',
  template: `
    <div class="orders-container">
      <div class="orders-header">
        <h2>My Orders</h2>
        <button mat-raised-button color="primary" routerLink="/photos">
          <mat-icon>add</mat-icon>
          New Order
        </button>
      </div>

      <!-- Orders Table -->
      <mat-card *ngIf="orders.length > 0" class="orders-table-card">
        <div class="table-container">
          <table mat-table [dataSource]="orders" class="orders-table">
            <!-- Order Number Column -->
            <ng-container matColumnDef="orderNumber">
              <th mat-header-cell *matHeaderCellDef>Order #</th>
              <td mat-cell *matCellDef="let order">
                <a [routerLink]="['/orders', order.orderId]" class="order-link">
                  {{ order.orderNumber }}
                </a>
              </td>
            </ng-container>

            <!-- Date Column -->
            <ng-container matColumnDef="orderDate">
              <th mat-header-cell *matHeaderCellDef>Date</th>
              <td mat-cell *matCellDef="let order">
                {{ formatDate(order.orderDate) }}
              </td>
            </ng-container>

            <!-- Status Column -->
            <ng-container matColumnDef="status">
              <th mat-header-cell *matHeaderCellDef>Status</th>
              <td mat-cell *matCellDef="let order">
                <mat-chip [class]="getStatusClass(order.status)">
                  {{ getStatusLabel(order.status) }}
                </mat-chip>
              </td>
            </ng-container>

            <!-- Items Column -->
            <ng-container matColumnDef="items">
              <th mat-header-cell *matHeaderCellDef>Items</th>
              <td mat-cell *matCellDef="let order">
                {{ order.photoCount }} photos, {{ order.printCount }} prints
              </td>
            </ng-container>

            <!-- Payment Column -->
            <ng-container matColumnDef="paymentMethod">
              <th mat-header-cell *matHeaderCellDef>Payment</th>
              <td mat-cell *matCellDef="let order">
                <div class="payment-info">
                  <mat-icon class="payment-icon">
                    {{ getPaymentIcon(order.paymentMethod) }}
                  </mat-icon>
                  {{ getPaymentLabel(order.paymentMethod) }}
                </div>
              </td>
            </ng-container>

            <!-- Total Column -->
            <ng-container matColumnDef="totalAmount">
              <th mat-header-cell *matHeaderCellDef>Total</th>
              <td mat-cell *matCellDef="let order">
                <span class="order-total">\${{ order.totalAmount.toFixed(2) }}</span>
              </td>
            </ng-container>

            <!-- Actions Column -->
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef>Actions</th>
              <td mat-cell *matCellDef="let order">
                <button mat-icon-button [routerLink]="['/orders', order.orderId]"
                        matTooltip="View Details">
                  <mat-icon>visibility</mat-icon>
                </button>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns;"
                class="order-row"
                [routerLink]="['/orders', row.orderId]"></tr>
          </table>
        </div>

        <!-- Pagination -->
        <mat-paginator
          *ngIf="totalOrders > pageSize"
          [length]="totalOrders"
          [pageSize]="pageSize"
          [pageSizeOptions]="[10, 20, 50]"
          (page)="onPageChange($event)"
          showFirstLastButtons>
        </mat-paginator>
      </mat-card>

      <!-- Empty State -->
      <div class="empty-state" *ngIf="orders.length === 0 && !isLoading">
        <mat-icon class="empty-icon">receipt</mat-icon>
        <h3>No Orders Yet</h3>
        <p>Start by uploading photos and placing your first order</p>
        <button mat-raised-button color="primary" routerLink="/photos">
          <mat-icon>photo_library</mat-icon>
          Browse Photos
        </button>
      </div>

      <!-- Loading -->
      <div class="loading-container" *ngIf="isLoading">
        <mat-spinner></mat-spinner>
        <p>Loading orders...</p>
      </div>
    </div>
  `,
  styles: [`
    .orders-container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 20px;
    }

    .orders-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
    }

    .orders-table-card {
      overflow: hidden;
    }

    .table-container {
      overflow-x: auto;
    }

    .orders-table {
      width: 100%;
      min-width: 800px;
    }

    .order-row {
      cursor: pointer;
      transition: background-color 0.2s ease;
    }

    .order-row:hover {
      background-color: #f5f5f5;
    }

    .order-link {
      color: #3f51b5;
      text-decoration: none;
      font-weight: 500;
    }

    .order-link:hover {
      text-decoration: underline;
    }

    .payment-info {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .payment-icon {
      font-size: 18px;
      width: 18px;
      height: 18px;
    }

    .order-total {
      font-weight: 500;
      color: #2e7d32;
    }

    /* Status chip styles */
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

    .empty-state {
      text-align: center;
      padding: 60px 20px;
    }

    .empty-icon {
      font-size: 64px;
      width: 64px;
      height: 64px;
      color: #ccc;
      margin-bottom: 16px;
    }

    .loading-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 60px 20px;
    }

    .loading-container p {
      margin-top: 16px;
      color: #666;
    }

    @media (max-width: 768px) {
      .orders-header {
        flex-direction: column;
        gap: 16px;
        align-items: stretch;
      }

      .orders-table {
        min-width: 600px;
      }
    }
  `]
})
export class OrderListComponent implements OnInit {
  orders: Order[] = [];
  totalOrders = 0;
  pageSize = 10;
  currentPage = 1;
  isLoading = false;

  displayedColumns = [
    'orderNumber',
    'orderDate',
    'status',
    'items',
    'paymentMethod',
    'totalAmount',
    'actions'
  ];

  constructor(
    private orderService: OrderService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadOrders();
  }

  loadOrders(): void {
    this.isLoading = true;

    this.orderService.getOrders(this.currentPage, this.pageSize).subscribe({
      next: (response) => {
        this.isLoading = false;
        if (response.success) {
          this.orders = response.data.items;
          this.totalOrders = response.data.totalCount;
        }
      },
      error: (error) => {
        this.isLoading = false;
        this.snackBar.open('Failed to load orders', 'Close', { duration: 3000 });
      }
    });
  }

  onPageChange(event: PageEvent): void {
    this.currentPage = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadOrders();
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString();
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
