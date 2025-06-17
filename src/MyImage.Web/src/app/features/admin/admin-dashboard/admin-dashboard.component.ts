import { Component, OnInit } from '@angular/core';
import { AdminService } from '../../../core/services/admin.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: false,
  template: `
    <div class="dashboard-container">
      <h2>Admin Dashboard</h2>

      <!-- Stats Cards -->
      <div class="stats-grid" *ngIf="stats">
        <mat-card class="stat-card">
          <mat-card-content>
            <div class="stat-content">
              <div class="stat-number">{{ stats.pendingOrders }}</div>
              <div class="stat-label">Pending Orders</div>
            </div>
            <mat-icon class="stat-icon pending">hourglass_empty</mat-icon>
          </mat-card-content>
        </mat-card>

        <mat-card class="stat-card">
          <mat-card-content>
            <div class="stat-content">
              <div class="stat-number">{{ stats.processingOrders }}</div>
              <div class="stat-label">Processing</div>
            </div>
            <mat-icon class="stat-icon processing">sync</mat-icon>
          </mat-card-content>
        </mat-card>

        <mat-card class="stat-card">
          <mat-card-content>
            <div class="stat-content">
              <div class="stat-number">{{ stats.completedToday }}</div>
              <div class="stat-label">Completed Today</div>
            </div>
            <mat-icon class="stat-icon completed">check_circle</mat-icon>
          </mat-card-content>
        </mat-card>

        <mat-card class="stat-card">
          <mat-card-content>
            <div class="stat-content">
              <div class="stat-number">\${{ stats.totalRevenue?.toFixed(2) }}</div>
              <div class="stat-label">Total Revenue</div>
            </div>
            <mat-icon class="stat-icon revenue">attach_money</mat-icon>
          </mat-card-content>
        </mat-card>
      </div>

      <!-- Quick Actions -->
      <div class="quick-actions">
        <h3>Quick Actions</h3>
        <div class="actions-grid">
          <button mat-raised-button color="primary" routerLink="/admin/orders">
            <mat-icon>assignment</mat-icon>
            Manage Orders
          </button>

          <button mat-raised-button color="accent" routerLink="/admin/pricing">
            <mat-icon>price_change</mat-icon>
            Update Pricing
          </button>

          <button mat-stroked-button routerLink="/photos">
            <mat-icon>photo_library</mat-icon>
            View as Customer
          </button>
        </div>
      </div>

      <!-- Recent Activity -->
      <mat-card class="recent-activity" *ngIf="recentOrders">
        <mat-card-header>
          <mat-card-title>Recent Orders</mat-card-title>
          <mat-card-subtitle>Orders requiring attention</mat-card-subtitle>
        </mat-card-header>

        <mat-card-content>
          <div class="order-item" *ngFor="let order of recentOrders">
            <div class="order-info">
              <span class="order-number">{{ order.orderNumber }}</span>
              <span class="order-customer">{{ order.customerName }}</span>
            </div>
            <mat-chip [class]="getStatusClass(order.status)">
              {{ getStatusLabel(order.status) }}
            </mat-chip>
            <span class="order-amount">\${{ order.totalAmount.toFixed(2) }}</span>
          </div>

          <div class="view-all-action">
            <button mat-button routerLink="/admin/orders">View All Orders</button>
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .dashboard-container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 20px;
    }

    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 20px;
      margin: 20px 0 40px 0;
    }

    .stat-card {
      padding: 0;
    }

    .stat-card mat-card-content {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 24px;
    }

    .stat-content {
      display: flex;
      flex-direction: column;
    }

    .stat-number {
      font-size: 2em;
      font-weight: 600;
      line-height: 1;
    }

    .stat-label {
      color: #666;
      margin-top: 8px;
    }

    .stat-icon {
      font-size: 32px;
      width: 32px;
      height: 32px;
      opacity: 0.7;
    }

    .stat-icon.pending {
      color: #ff9800;
    }

    .stat-icon.processing {
      color: #2196f3;
    }

    .stat-icon.completed {
      color: #4caf50;
    }

    .stat-icon.revenue {
      color: #9c27b0;
    }

    .quick-actions {
      margin: 40px 0;
    }

    .actions-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 16px;
      margin-top: 16px;
    }

    .actions-grid button {
      height: 60px;
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .recent-activity {
      margin-top: 40px;
    }

    .order-item {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 12px 0;
      border-bottom: 1px solid #f0f0f0;
    }

    .order-item:last-child {
      border-bottom: none;
    }

    .order-info {
      display: flex;
      flex-direction: column;
      flex: 1;
    }

    .order-number {
      font-weight: 500;
    }

    .order-customer {
      color: #666;
      font-size: 0.9em;
    }

    .order-amount {
      font-weight: 500;
      color: #2e7d32;
      margin-left: 16px;
    }

    .view-all-action {
      text-align: center;
      margin-top: 16px;
      padding-top: 16px;
      border-top: 1px solid #f0f0f0;
    }

    /* Status styles */
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
  `]
})
export class AdminDashboardComponent implements OnInit {
  stats: any = null;
  recentOrders: any[] = [];

  constructor(private adminService: AdminService) {}

  ngOnInit(): void {
    this.loadDashboardData();
  }

  private loadDashboardData(): void {
    this.adminService.getDashboardStats().subscribe({
      next: (response) => {
        if (response.success) {
          this.stats = response.data;
        }
      }
    });

    // Load recent orders that need attention
    this.adminService.getOrders('pending', 1, 5).subscribe({
      next: (response) => {
        if (response.success) {
          this.recentOrders = response.data.items;
        }
      }
    });
  }

  getStatusLabel(status: string): string {
    const statusLabels: { [key: string]: string } = {
      'pending': 'Pending Payment',
      'payment_verified': 'Payment Verified',
      'processing': 'Processing'
    };
    return statusLabels[status] || status;
  }

  getStatusClass(status: string): string {
    return `status-${status.replace('_', '-')}`;
  }
}
