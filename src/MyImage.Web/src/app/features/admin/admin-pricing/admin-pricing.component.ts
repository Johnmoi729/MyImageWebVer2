// src/app/features/admin/admin-pricing/admin-pricing.component.ts
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AdminService } from '../../../core/services/admin.service';

@Component({
  selector: 'app-admin-pricing',
  template: `
    <div class="pricing-container">
      <div class="pricing-header">
        <h2>Price Management</h2>
        <button mat-raised-button color="primary" (click)="toggleAddForm()">
          <mat-icon>add</mat-icon>
          Add New Size
        </button>
      </div>

      <!-- Add New Size Form -->
      <mat-card class="add-form-card" *ngIf="showAddForm">
        <mat-card-header>
          <mat-card-title>Add New Print Size</mat-card-title>
        </mat-card-header>

        <mat-card-content>
          <form [formGroup]="addSizeForm" class="add-size-form">
            <div class="form-row">
              <mat-form-field appearance="outline" class="size-code-field">
                <mat-label>Size Code</mat-label>
                <input matInput formControlName="sizeCode" placeholder="e.g., 8x10">
                <mat-error>Size code is required</mat-error>
              </mat-form-field>

              <mat-form-field appearance="outline" class="display-name-field">
                <mat-label>Display Name</mat-label>
                <input matInput formControlName="displayName" placeholder="e.g., Large 8×10">
                <mat-error>Display name is required</mat-error>
              </mat-form-field>
            </div>

            <div class="form-row">
              <mat-form-field appearance="outline" class="dimension-field">
                <mat-label>Width (inches)</mat-label>
                <input matInput type="number" formControlName="width" step="0.1">
                <mat-error>Width is required</mat-error>
              </mat-form-field>

              <mat-form-field appearance="outline" class="dimension-field">
                <mat-label>Height (inches)</mat-label>
                <input matInput type="number" formControlName="height" step="0.1">
                <mat-error>Height is required</mat-error>
              </mat-form-field>

              <mat-form-field appearance="outline" class="price-field">
                <mat-label>Price ($)</mat-label>
                <input matInput type="number" formControlName="price" step="0.01">
                <mat-error>Price is required</mat-error>
              </mat-form-field>
            </div>

            <div class="form-row">
              <mat-form-field appearance="outline" class="pixel-field">
                <mat-label>Min Width (pixels)</mat-label>
                <input matInput type="number" formControlName="minWidth">
                <mat-error>Minimum width is required</mat-error>
              </mat-form-field>

              <mat-form-field appearance="outline" class="pixel-field">
                <mat-label>Min Height (pixels)</mat-label>
                <input matInput type="number" formControlName="minHeight">
                <mat-error>Minimum height is required</mat-error>
              </mat-form-field>

              <mat-form-field appearance="outline" class="pixel-field">
                <mat-label>Recommended Width (pixels)</mat-label>
                <input matInput type="number" formControlName="recommendedWidth">
                <mat-error>Recommended width is required</mat-error>
              </mat-form-field>

              <mat-form-field appearance="outline" class="pixel-field">
                <mat-label>Recommended Height (pixels)</mat-label>
                <input matInput type="number" formControlName="recommendedHeight">
                <mat-error>Recommended height is required</mat-error>
              </mat-form-field>
            </div>
          </form>
        </mat-card-content>

        <mat-card-actions>
          <button mat-button (click)="cancelAdd()">Cancel</button>
          <button mat-raised-button color="primary"
                  [disabled]="addSizeForm.invalid || isAdding"
                  (click)="addNewSize()">
            <mat-spinner diameter="20" *ngIf="isAdding"></mat-spinner>
            <span *ngIf="!isAdding">Add Size</span>
          </button>
        </mat-card-actions>
      </mat-card>

      <!-- Existing Sizes Table -->
      <mat-card class="sizes-table-card">
        <mat-card-header>
          <mat-card-title>Current Print Sizes</mat-card-title>
        </mat-card-header>

        <mat-card-content>
          <div class="table-container">
            <table mat-table [dataSource]="printSizes" class="sizes-table">
              <!-- Size Code Column -->
              <ng-container matColumnDef="sizeCode">
                <th mat-header-cell *matHeaderCellDef>Size Code</th>
                <td mat-cell *matCellDef="let size">
                  <span class="size-code">{{ size.sizeCode }}</span>
                </td>
              </ng-container>

              <!-- Display Name Column -->
              <ng-container matColumnDef="displayName">
                <th mat-header-cell *matHeaderCellDef>Display Name</th>
                <td mat-cell *matCellDef="let size">
                  {{ size.displayName }}
                </td>
              </ng-container>

              <!-- Dimensions Column -->
              <ng-container matColumnDef="dimensions">
                <th mat-header-cell *matHeaderCellDef>Dimensions</th>
                <td mat-cell *matCellDef="let size">
                  {{ size.width }}" × {{ size.height }}"
                </td>
              </ng-container>

              <!-- Price Column -->
              <ng-container matColumnDef="price">
                <th mat-header-cell *matHeaderCellDef>Price</th>
                <td mat-cell *matCellDef="let size">
                  <div class="price-edit" *ngIf="editingPrice === size.id; else priceDisplay">
                    <mat-form-field appearance="outline" class="price-input">
                      <input matInput type="number" [(ngModel)]="tempPrice" step="0.01">
                    </mat-form-field>
                    <button mat-icon-button color="primary" (click)="savePrice(size)">
                      <mat-icon>check</mat-icon>
                    </button>
                    <button mat-icon-button (click)="cancelPriceEdit()">
                      <mat-icon>close</mat-icon>
                    </button>
                  </div>
                  <ng-template #priceDisplay>
                    <span class="price-value" (click)="startPriceEdit(size)">
                      \${{ size.price.toFixed(2) }}
                      <mat-icon class="edit-icon">edit</mat-icon>
                    </span>
                  </ng-template>
                </td>
              </ng-container>

              <!-- Quality Requirements Column -->
              <ng-container matColumnDef="quality">
                <th mat-header-cell *matHeaderCellDef>Quality Requirements</th>
                <td mat-cell *matCellDef="let size">
                  <div class="quality-info">
                    <div class="quality-row">
                      <span class="quality-label">Min:</span>
                      <span>{{ size.minWidth }} × {{ size.minHeight }}px</span>
                    </div>
                    <div class="quality-row">
                      <span class="quality-label">Recommended:</span>
                      <span>{{ size.recommendedWidth }} × {{ size.recommendedHeight }}px</span>
                    </div>
                  </div>
                </td>
              </ng-container>

              <!-- Status Column -->
              <ng-container matColumnDef="status">
                <th mat-header-cell *matHeaderCellDef>Status</th>
                <td mat-cell *matCellDef="let size">
                  <mat-chip [class]="size.isActive ? 'status-active' : 'status-inactive'"
                           (click)="toggleStatus(size)">
                    {{ size.isActive ? 'Active' : 'Inactive' }}
                  </mat-chip>
                </td>
              </ng-container>

              <!-- Actions Column -->
              <ng-container matColumnDef="actions">
                <th mat-header-cell *matHeaderCellDef>Actions</th>
                <td mat-cell *matCellDef="let size">
                  <button mat-icon-button (click)="editSize(size)" matTooltip="Edit Size">
                    <mat-icon>edit</mat-icon>
                  </button>
                  <button mat-icon-button (click)="toggleStatus(size)"
                          [matTooltip]="size.isActive ? 'Deactivate' : 'Activate'">
                    <mat-icon>{{ size.isActive ? 'visibility_off' : 'visibility' }}</mat-icon>
                  </button>
                </td>
              </ng-container>

              <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
              <tr mat-row *matRowDef="let row; columns: displayedColumns;" class="size-row"></tr>
            </table>
          </div>
        </mat-card-content>
      </mat-card>

      <!-- Statistics -->
      <mat-card class="stats-card">
        <mat-card-header>
          <mat-card-title>Pricing Statistics</mat-card-title>
        </mat-card-header>

        <mat-card-content>
          <div class="stats-grid">
            <div class="stat-item">
              <span class="stat-label">Total Sizes:</span>
              <span class="stat-value">{{ printSizes.length }}</span>
            </div>
            <div class="stat-item">
              <span class="stat-label">Active Sizes:</span>
              <span class="stat-value">{{ getActiveSizes().length }}</span>
            </div>
            <div class="stat-item">
              <span class="stat-label">Price Range:</span>
              <span class="stat-value">\${{ getMinPrice().toFixed(2) }} - \${{ getMaxPrice().toFixed(2) }}</span>
            </div>
            <div class="stat-item">
              <span class="stat-label">Average Price:</span>
              <span class="stat-value">\${{ getAveragePrice().toFixed(2) }}</span>
            </div>
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .pricing-container {
      max-width: 1400px;
      margin: 0 auto;
      padding: 20px;
    }

    .pricing-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
    }

    .add-form-card {
      margin-bottom: 20px;
    }

    .add-size-form {
      margin-top: 16px;
    }

    .form-row {
      display: flex;
      gap: 16px;
      margin-bottom: 16px;
      flex-wrap: wrap;
    }

    .size-code-field {
      flex: 0 0 120px;
    }

    .display-name-field {
      flex: 1;
      min-width: 200px;
    }

    .dimension-field, .price-field {
      flex: 0 0 120px;
    }

    .pixel-field {
      flex: 0 0 150px;
    }

    .sizes-table-card {
      margin-bottom: 20px;
      overflow: hidden;
    }

    .table-container {
      overflow-x: auto;
    }

    .sizes-table {
      width: 100%;
      min-width: 1000px;
    }

    .size-row:hover {
      background-color: #f5f5f5;
    }

    .size-code {
      font-weight: 500;
      color: #3f51b5;
      font-family: monospace;
    }

    .price-edit {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .price-input {
      width: 80px;
    }

    .price-value {
      display: flex;
      align-items: center;
      gap: 8px;
      cursor: pointer;
      padding: 4px 8px;
      border-radius: 4px;
      transition: background-color 0.2s;
      font-weight: 500;
      color: #2e7d32;
    }

    .price-value:hover {
      background-color: #f0f0f0;
    }

    .edit-icon {
      font-size: 16px;
      width: 16px;
      height: 16px;
      opacity: 0.5;
    }

    .quality-info {
      font-size: 0.9em;
    }

    .quality-row {
      display: flex;
      gap: 8px;
      margin: 2px 0;
    }

    .quality-label {
      font-weight: 500;
      color: #666;
      min-width: 80px;
    }

    .status-active {
      background-color: #e8f5e8;
      color: #2e7d32;
      cursor: pointer;
    }

    .status-inactive {
      background-color: #ffebee;
      color: #c62828;
      cursor: pointer;
    }

    .stats-card {
      margin-top: 20px;
    }

    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 20px;
    }

    .stat-item {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 12px;
      background: #f5f5f5;
      border-radius: 8px;
    }

    .stat-label {
      font-weight: 500;
      color: #666;
    }

    .stat-value {
      font-weight: 600;
      color: #3f51b5;
    }

    @media (max-width: 768px) {
      .form-row {
        flex-direction: column;
      }

      .dimension-field, .price-field, .pixel-field {
        flex: 1;
      }

      .pricing-header {
        flex-direction: column;
        gap: 16px;
        align-items: stretch;
      }
    }
  `]
})
export class AdminPricingComponent implements OnInit {
  printSizes: any[] = [];
  showAddForm = false;
  addSizeForm: FormGroup;
  editingPrice: string | null = null;
  tempPrice: number = 0;
  isAdding = false;

  displayedColumns = [
    'sizeCode',
    'displayName',
    'dimensions',
    'price',
    'quality',
    'status',
    'actions'
  ];

  constructor(
    private fb: FormBuilder,
    private adminService: AdminService,
    private snackBar: MatSnackBar
  ) {
    this.addSizeForm = this.fb.group({
      sizeCode: ['', Validators.required],
      displayName: ['', Validators.required],
      width: ['', [Validators.required, Validators.min(0.1)]],
      height: ['', [Validators.required, Validators.min(0.1)]],
      price: ['', [Validators.required, Validators.min(0.01)]],
      minWidth: ['', [Validators.required, Validators.min(1)]],
      minHeight: ['', [Validators.required, Validators.min(1)]],
      recommendedWidth: ['', [Validators.required, Validators.min(1)]],
      recommendedHeight: ['', [Validators.required, Validators.min(1)]]
    });
  }

  ngOnInit(): void {
    this.loadPrintSizes();
  }

  private loadPrintSizes(): void {
    this.adminService.getAllPrintSizes().subscribe({
      next: (response) => {
        if (response.success) {
          this.printSizes = response.data;
        }
      },
      error: () => {
        this.snackBar.open('Failed to load print sizes', 'Close', { duration: 3000 });
      }
    });
  }

  toggleAddForm(): void {
    this.showAddForm = !this.showAddForm;
    if (this.showAddForm) {
      this.addSizeForm.reset();
    }
  }

  addNewSize(): void {
    if (this.addSizeForm.valid) {
      this.isAdding = true;

      this.adminService.addPrintSize(this.addSizeForm.value).subscribe({
        next: (response) => {
          this.isAdding = false;
          if (response.success) {
            this.snackBar.open('Print size added successfully', 'Close', { duration: 3000 });
            this.showAddForm = false;
            this.loadPrintSizes();
          }
        },
        error: () => {
          this.isAdding = false;
          this.snackBar.open('Failed to add print size', 'Close', { duration: 3000 });
        }
      });
    }
  }

  cancelAdd(): void {
    this.showAddForm = false;
    this.addSizeForm.reset();
  }

  startPriceEdit(size: any): void {
    this.editingPrice = size.id;
    this.tempPrice = size.price;
  }

  savePrice(size: any): void {
    if (this.tempPrice > 0) {
      this.adminService.updatePrintSize(size.id, {
        price: this.tempPrice,
        isActive: size.isActive
      }).subscribe({
        next: (response) => {
          if (response.success) {
            this.snackBar.open('Price updated successfully', 'Close', { duration: 3000 });
            this.cancelPriceEdit();
            this.loadPrintSizes();
          }
        },
        error: () => {
          this.snackBar.open('Failed to update price', 'Close', { duration: 3000 });
        }
      });
    }
  }

  cancelPriceEdit(): void {
    this.editingPrice = null;
    this.tempPrice = 0;
  }

  toggleStatus(size: any): void {
    const newStatus = !size.isActive;

    this.adminService.updatePrintSize(size.id, {
      price: size.price,
      isActive: newStatus
    }).subscribe({
      next: (response) => {
        if (response.success) {
          this.snackBar.open(
            `Print size ${newStatus ? 'activated' : 'deactivated'}`,
            'Close',
            { duration: 3000 }
          );
          this.loadPrintSizes();
        }
      },
      error: () => {
        this.snackBar.open('Failed to update status', 'Close', { duration: 3000 });
      }
    });
  }

  editSize(size: any): void {
    // Open edit dialog or form
    // Implementation depends on requirements
    console.log('Edit size:', size);
  }

  getActiveSizes(): any[] {
    return this.printSizes.filter(size => size.isActive);
  }

  getMinPrice(): number {
    const activeSizes = this.getActiveSizes();
    return activeSizes.length > 0 ? Math.min(...activeSizes.map(s => s.price)) : 0;
  }

  getMaxPrice(): number {
    const activeSizes = this.getActiveSizes();
    return activeSizes.length > 0 ? Math.max(...activeSizes.map(s => s.price)) : 0;
  }

  getAveragePrice(): number {
    const activeSizes = this.getActiveSizes();
    if (activeSizes.length === 0) return 0;
    const total = activeSizes.reduce((sum, size) => sum + size.price, 0);
    return total / activeSizes.length;
  }
}
