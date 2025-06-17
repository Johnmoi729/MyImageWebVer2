import { Component, Inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AdminService } from '../../../core/services/admin.service';

@Component({
  selector: 'app-order-status-dialog',
  template: `
    <div class="dialog-container">
      <h2 mat-dialog-title>Update Order Status</h2>

      <mat-dialog-content>
        <div class="order-info">
          <h4>Order {{ data.order.orderNumber }}</h4>
          <p>Customer: {{ data.order.customerName }}</p>
          <p>Current Status:
            <mat-chip [class]="getStatusClass(data.order.status)">
              {{ getStatusLabel(data.order.status) }}
            </mat-chip>
          </p>
        </div>

        <form [formGroup]="statusForm" class="status-form">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>New Status</mat-label>
            <mat-select formControlName="status">
              <mat-option value="pending">Pending Payment</mat-option>
              <mat-option value="payment_verified">Payment Verified</mat-option>
              <mat-option value="processing">Processing</mat-option>
              <mat-option value="printed">Printed</mat-option>
              <mat-option value="shipped">Shipped</mat-option>
              <mat-option value="completed">Completed</mat-option>
            </mat-select>
            <mat-error>Please select a status</mat-error>
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Notes (optional)</mat-label>
            <textarea matInput formControlName="notes" rows="3"
                      placeholder="Add any notes about this status change..."></textarea>
          </mat-form-field>
        </form>
      </mat-dialog-content>

      <mat-dialog-actions align="end">
        <button mat-button (click)="cancel()">Cancel</button>
        <button mat-raised-button color="primary"
                [disabled]="statusForm.invalid || isUpdating"
                (click)="updateStatus()">
          <mat-spinner diameter="20" *ngIf="isUpdating"></mat-spinner>
          <span *ngIf="!isUpdating">Update Status</span>
        </button>
      </mat-dialog-actions>
    </div>
  `,
  styles: [`
    .dialog-container {
      min-width: 450px;
    }

    .order-info {
      margin-bottom: 20px;
      padding: 16px;
      background: #f5f5f5;
      border-radius: 8px;
    }

    .order-info h4 {
      margin: 0 0 8px 0;
      color: #3f51b5;
    }

    .order-info p {
      margin: 4px 0;
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .status-form {
      margin-top: 16px;
    }

    .full-width {
      width: 100%;
      margin-bottom: 16px;
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
export class OrderStatusDialogComponent {
  statusForm: FormGroup;
  isUpdating = false;

  constructor(
    @Inject(MAT_DIALOG_DATA) public data: any,
    private dialogRef: MatDialogRef<OrderStatusDialogComponent>,
    private fb: FormBuilder,
    private adminService: AdminService,
    private snackBar: MatSnackBar
  ) {
    this.statusForm = this.fb.group({
      status: [data.order.status, Validators.required],
      notes: ['']
    });
  }

  updateStatus(): void {
    if (this.statusForm.valid) {
      this.isUpdating = true;

      const { status, notes } = this.statusForm.value;

      this.adminService.updateOrderStatus(this.data.order.orderId, status, notes).subscribe({
        next: (response) => {
          this.isUpdating = false;
          if (response.success) {
            this.snackBar.open('Order status updated successfully', 'Close', { duration: 3000 });
            this.dialogRef.close(true);
          }
        },
        error: (error) => {
          this.isUpdating = false;
          this.snackBar.open('Failed to update order status', 'Close', { duration: 3000 });
        }
      });
    }
  }

  cancel(): void {
    this.dialogRef.close(false);
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
}
