import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { PageEvent } from '@angular/material/paginator';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AdminService } from '../../../core/services/admin.service';
import { OrderStatusDialogComponent } from '../order-status-dialog/order-status-dialog.component';

@Component({
  selector: 'app-admin-orders',
  standalone: false,
  template: `
    <div class="admin-orders-container">
      <div class="orders-header">
        <h2>Order Management</h2>

        <!-- Status Filter -->
        <mat-form-field appearance="outline" class="status-filter">
          <mat-label>Filter by Status</mat-label>
          <mat-select [(value)]="selectedStatus" (selectionChange)="onStatusFilter()">
            <mat-option value="">All Orders</mat-option>
            <mat-option value="pending">Pending Payment</mat-option>
            <mat-option value="payment_verified">Payment Verified</mat-option>
            <mat-option value="processing">Processing</mat-option>
            <mat-option value="printed">Printed</mat-option>
            <mat-option value="shipped">Shipped</mat-option>
            <mat-option value="completed">Completed</mat-option>
          </mat-select>
        </mat-form-field>
      </div>

      <!-- Orders Table -->
      <mat-card class="orders-table-card">
        <div class="table-container">
          <table mat-table [dataSource]="orders" class="orders-table">
            <!-- Order Number Column -->
            <ng-container matColumnDef="orderNumber">
              <th mat-header-cell *matHeaderCellDef>Order #</th>
              <td mat-cell *matCellDef="let order">
                <span class="order-number">{{ order.orderNumber }}</span>
              </td>
            </ng-container>

            <!-- Customer Column -->
            <ng-container matColumnDef="customer">
              <th mat-header-cell *matHeaderCellDef>Customer</th>
              <td mat-cell *matCellDef="let order">
                <div class="customer-info">
                  <span class="customer-name">{{ order.customerName }}</span>
                  <span class="customer-email">{{ order.customerEmail }}</span>
                </div>
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

            <!-- Payment Column -->
            <ng-container matColumnDef="paymentMethod">
              <th mat-header-cell *matHeaderCellDef>Payment</th>
              <td mat-cell *matCellDef="let order">
                <div class="payment-info">
                  <mat-icon>{{ getPaymentIcon(order.paymentMethod) }}</mat-icon>
                  {{ getPaymentLabel(order.paymentMethod) }}
                </div>
              </td>
            </ng-container>

            <!-- Items Column -->
            <ng-container matColumnDef="items">
              <th mat-header-cell *matHeaderCellDef>Items</th>
              <td mat-cell *matCellDef="let order">
                {{ order.photoCount }} photos<br>
                {{ order.printCount }} prints
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
                <div class="action-buttons">
                  <button mat-icon-button (click)="updateOrderStatus(order)"
                          matTooltip="Update Status">
                    <mat-icon>edit</mat-icon>
                  </button>

                  <button mat-icon-button (click)="viewOrderDetails(order)"
                          matTooltip="View Details">
                    <mat-icon>visibility</mat-icon>
                  </button>

                  <button mat-icon-button
                          *ngIf="order.status === 'shipped'"
                          (click)="completeOrder(order)"
                          color="primary"
                          matTooltip="Mark as Completed">
                    <mat-icon>check_circle</mat-icon>
                  </button>
                </div>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns;" class="order-row"></tr>
          </table>
        </div>

        <!-- Pagination -->
        <mat-paginator
          [length]="totalOrders"
          [pageSize]="pageSize"
          [pageSizeOptions]="[10, 25, 50]"
          (page)="onPageChange($event)"
          showFirstLastButtons>
        </mat-paginator>
      </mat-card>
    </div>
  `,
  styles: [`
    .admin-orders-container {
      max-width: 1400px;
      margin: 0 auto;
      padding: 20px;
    }

    .orders-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
    }

    .status-filter {
      min-width: 200px;
    }

    .orders-table-card {
      overflow: hidden;
    }

    .table-container {
      overflow-x: auto;
    }

    .orders-table {
      width: 100%;
      min-width: 1000px;
    }

    .order-row:hover {
      background-color: #f5f5f5;
    }

    .order-number {
      font-weight: 500;
      color: #3f51b5;
    }

    .customer-info {
      display: flex;
      flex-direction: column;
    }

    .customer-name {
      font-weight: 500;
    }

    .customer-email {
      color: #666;
      font-size: 0.9em;
    }

    .payment-info {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .order-total {
      font-weight: 500;
      color: #2e7d32;
    }

    .action-buttons {
      display: flex;
      gap: 4px;
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
  `]
})
export class AdminOrdersComponent implements OnInit {
  orders: any[] = [];
  totalOrders = 0;
  pageSize = 25;
  currentPage = 1;
  selectedStatus = '';

  displayedColumns = [
    'orderNumber',
    'customer',
    'orderDate',
    'status',
    'paymentMethod',
    'items',
    'totalAmount',
    'actions'
  ];

  constructor(
    private adminService: AdminService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadOrders();
  }

  loadOrders(): void {
    this.adminService.getOrders(this.selectedStatus, this.currentPage, this.pageSize).subscribe({
      next: (response) => {
        if (response.success) {
          this.orders = response.data.items;
          this.totalOrders = response.data.totalCount;
        }
      },
      error: (error) => {
        this.snackBar.open('Failed to load orders', 'Close', { duration: 3000 });
      }
    });
  }

  onStatusFilter(): void {
    this.currentPage = 1;
    this.loadOrders();
  }

  onPageChange(event: PageEvent): void {
    this.currentPage = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadOrders();
  }

  updateOrderStatus(order: any): void {
    const dialogRef = this.dialog.open(OrderStatusDialogComponent, {
      width: '500px',
      data: { order }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadOrders();
      }
    });
  }

  viewOrderDetails(order: any): void {
    // Navigate to order details or open in dialog
  }

  completeOrder(order: any): void {
    if (confirm(`Mark order ${order.orderNumber} as completed? This will delete all associated photos.`)) {
      const completionData = {
        shippingDate: new Date().toISOString(),
        notes: 'Order completed and photos scheduled for deletion'
      };

      this.adminService.completeOrder(order.orderId, completionData).subscribe({
        next: (response) => {
          if (response.success) {
            this.snackBar.open('Order completed successfully', 'Close', { duration: 3000 });
            this.loadOrders();
          }
        },
        error: (error) => {
          this.snackBar.open('Failed to complete order', 'Close', { duration: 3000 });
        }
      });
    }
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
