import { Component, Inject, OnInit } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';

import { CartService } from '../../../core/services/cart.service';
import { PrintSizeService } from '../../../core/services/print-size.service';
import { PrintSelection } from '../../../shared/models/cart.models';
import { Photo } from '../../../shared/models/photo.models';
import { PrintSize } from '../../../shared/models/print-size.models';

/**
 * Component for selecting print sizes and quantities for a specific photo.
 * This dialog allows users to choose multiple print sizes for a single photo,
 * calculate totals in real-time, and add selections to their shopping cart.
 *
 * Key Features:
 * - Multi-size selection per photo (implements Requirement 2)
 * - Real-time price calculation
 * - Quality indicators based on photo resolution
 * - Form validation and error handling
 */
@Component({
  selector: 'app-print-selector',
  // Modern Angular standalone component
  standalone: false,
  template: `
    <div class="selector-container">
      <div class="selector-header">
        <h3>Select Print Sizes</h3>
        <button mat-icon-button (click)="close()">
          <mat-icon>close</mat-icon>
        </button>
      </div>

      <div class="photo-preview">
        <img [src]="photo.thumbnailUrl" [alt]="photo.filename" class="preview-thumb">
        <div class="photo-info">
          <h4>{{ photo.filename }}</h4>
          <p>{{ photo.dimensions.width }} × {{ photo.dimensions.height }} pixels</p>
        </div>
      </div>

      <form [formGroup]="printForm" class="print-form">
        <div class="size-selections" formArrayName="selections">
          <!-- Use ng-container to handle both *ngFor and *ngIf without conflict -->
          <ng-container *ngFor="let selection of selectionsArray.controls; let i = index; trackBy: trackByIndex">
            <div class="size-item"
                 [formGroupName]="i"
                 *ngIf="printSizes[i]">

              <div class="size-header">
                <mat-checkbox formControlName="selected"
                             (change)="onSizeToggle(i, $event.checked)">
                  <div class="size-info">
                    <!-- Safe to use direct access since we check existence with *ngIf -->
                    <span class="size-name">{{ printSizes[i].displayName }}</span>
                    <span class="size-dimensions">
                      {{ printSizes[i].width }}" × {{ printSizes[i].height }}"
                    </span>
                  </div>
                </mat-checkbox>

                <div class="size-price">
                  {{ printSizes[i].price | currency:'USD':'symbol':'1.2-2' }} each
                </div>
              </div>

              <div class="quantity-section" *ngIf="selection.get('selected')?.value">
                <mat-form-field appearance="outline" class="quantity-field">
                  <mat-label>Quantity</mat-label>
                  <input matInput type="number"
                         formControlName="quantity"
                         min="1" max="100"
                         (input)="calculateTotal()">
                  <mat-error *ngIf="selection.get('quantity')?.errors?.['min']">
                    Minimum quantity is 1
                  </mat-error>
                  <mat-error *ngIf="selection.get('quantity')?.errors?.['max']">
                    Maximum quantity is 100
                  </mat-error>
                </mat-form-field>

                <div class="line-total">
                  Total: {{ getLineTotal(i) | currency:'USD':'symbol':'1.2-2' }}
                </div>
              </div>

              <div class="quality-indicator" *ngIf="selection.get('selected')?.value">
                <div class="quality-rating" [class]="getQualityClass(i)">
                  <mat-icon>{{ getQualityIcon(i) }}</mat-icon>
                  <span>{{ getQualityText(i) }}</span>
                </div>
              </div>
            </div>
          </ng-container>
        </div>

        <div class="total-section" *ngIf="getTotalCost() > 0">
          <div class="total-row">
            <span class="total-label">Total Cost:</span>
            <span class="total-amount">{{ getTotalCost() | currency:'USD':'symbol':'1.2-2' }}</span>
          </div>
        </div>

        <div class="form-actions">
          <button mat-button (click)="close()">Cancel</button>
          <button mat-raised-button color="primary"
                  [disabled]="!hasValidSelections() || isAdding"
                  (click)="addToCart()">
            <mat-spinner diameter="20" *ngIf="isAdding"></mat-spinner>
            <span *ngIf="!isAdding">Add to Cart</span>
          </button>
        </div>
      </form>
    </div>
  `,
  styles: [`
    .selector-container {
      width: 600px;
      max-height: 80vh;
      display: flex;
      flex-direction: column;
    }

    .selector-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 16px 24px;
      border-bottom: 1px solid #eee;
    }

    .photo-preview {
      display: flex;
      align-items: center;
      padding: 16px 24px;
      gap: 16px;
      background: #fafafa;
    }

    .preview-thumb {
      width: 80px;
      height: 80px;
      object-fit: cover;
      border-radius: 4px;
    }

    .photo-info h4 {
      margin: 0 0 4px 0;
      font-weight: 500;
    }

    .photo-info p {
      margin: 0;
      color: #666;
      font-size: 0.9em;
    }

    .print-form {
      flex: 1;
      overflow-y: auto;
      padding: 0 24px;
    }

    .size-selections {
      max-height: 400px;
      overflow-y: auto;
    }

    .size-item {
      border: 1px solid #eee;
      border-radius: 8px;
      margin: 16px 0;
      padding: 16px;
    }

    .size-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 12px;
    }

    .size-info {
      display: flex;
      flex-direction: column;
    }

    .size-name {
      font-weight: 500;
    }

    .size-dimensions {
      color: #666;
      font-size: 0.9em;
    }

    .size-price {
      font-weight: 500;
      color: #3f51b5;
    }

    .quantity-section {
      display: flex;
      align-items: center;
      gap: 16px;
      margin: 12px 0;
    }

    .quantity-field {
      width: 120px;
    }

    .line-total {
      font-weight: 500;
      color: #2e7d32;
    }

    .quality-indicator {
      margin-top: 8px;
    }

    .quality-rating {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 4px 8px;
      border-radius: 4px;
      font-size: 0.9em;
    }

    .quality-rating.excellent {
      background: #e8f5e8;
      color: #2e7d32;
    }

    .quality-rating.good {
      background: #fff3e0;
      color: #f57c00;
    }

    .quality-rating.fair {
      background: #ffebee;
      color: #c62828;
    }

    .total-section {
      border-top: 1px solid #eee;
      padding: 16px 0;
      margin-top: 16px;
    }

    .total-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      font-size: 1.1em;
    }

    .total-label {
      font-weight: 500;
    }

    .total-amount {
      font-weight: 600;
      color: #3f51b5;
      font-size: 1.2em;
    }

    .form-actions {
      display: flex;
      justify-content: flex-end;
      gap: 16px;
      padding: 16px 0;
      border-top: 1px solid #eee;
    }
  `]
})
export class PrintSelectorComponent implements OnInit {
  printForm: FormGroup;
  printSizes: PrintSize[] = [];
  isAdding = false;

  constructor(
    @Inject(MAT_DIALOG_DATA) public photo: Photo, // Photo data passed from parent component
    private dialogRef: MatDialogRef<PrintSelectorComponent>, // Dialog reference for closing
    private fb: FormBuilder, // For building reactive forms
    private printSizeService: PrintSizeService, // Service to fetch available print sizes
    private cartService: CartService, // Service to add items to cart
    private snackBar: MatSnackBar // For user feedback messages
  ) {
    // Initialize reactive form with FormArray for multiple size selections
    this.printForm = this.fb.group({
      selections: this.fb.array([])
    });
  }

  /**
   * Getter for the FormArray containing print size selections.
   * This allows easy access to the selections array in the template.
   */
  get selectionsArray(): FormArray {
    return this.printForm.get('selections') as FormArray;
  }

  ngOnInit(): void {
    this.loadPrintSizes();
  }

  /**
   * TrackBy function for ngFor to improve performance and prevent unnecessary re-renders.
   * This helps Angular track which items have changed when the array is updated.
   */
  trackByIndex(index: number, item: any): number {
    return index;
  }

  /**
   * Loads available print sizes from the API and initializes the form.
   * This implements the API call to /api/print-sizes endpoint.
   */
  private loadPrintSizes(): void {
    this.printSizeService.getPrintSizes().subscribe({
      next: (response) => {
        if (response.success) {
          // Filter to only show active print sizes
          this.printSizes = response.data.filter((size: PrintSize) => size.isActive);
          this.initializeForm();
        }
      },
      error: (error) => {
        console.error('Error loading print sizes:', error);
        this.snackBar.open('Failed to load print sizes', 'Close', { duration: 3000 });
      }
    });
  }

  /**
   * Initializes the reactive form with FormGroups for each print size.
   * Creates a FormGroup for each size containing selection state, quantity, and pricing info.
   * Ensures the form array length matches the printSizes array length for consistency.
   */
  private initializeForm(): void {
    const selectionsArray = this.fb.array(
      this.printSizes.map(size => this.fb.group({
        selected: [false], // Whether this size is selected
        quantity: [1, [Validators.min(1), Validators.max(100)]], // Quantity with validation
        sizeCode: [size.sizeCode], // Size identifier
        unitPrice: [size.price] // Price per unit
      }))
    );

    this.printForm.setControl('selections', selectionsArray);
  }

  /**
   * Handles toggling of size selection checkbox.
   * When unchecked, resets quantity to 1 and recalculates totals.
   */
  onSizeToggle(index: number, selected: boolean): void {
    if (!selected) {
      // Reset quantity to 1 when unchecking
      this.selectionsArray.at(index).get('quantity')?.setValue(1);
    }
    this.calculateTotal();
  }

  /**
   * Triggers recalculation of totals.
   * This method can be expanded to perform additional calculations if needed.
   */
  calculateTotal(): void {
    // The totals are calculated by getters, this just ensures change detection runs
    // Could be expanded for more complex tax calculations or bulk discounts
  }

  /**
   * Calculates the total cost for a specific print size selection.
   * Multiplies quantity by unit price only if the size is selected.
   * Returns 0 if the print size doesn't exist at the given index.
   */
  getLineTotal(index: number): number {
    const selection = this.selectionsArray.at(index);
    const printSize = this.printSizes[index];

    if (!selection || !printSize) {
      return 0;
    }

    const selected = selection.get('selected')?.value;
    const quantity = selection.get('quantity')?.value || 0;
    const unitPrice = selection.get('unitPrice')?.value || printSize.price;

    return selected ? quantity * unitPrice : 0;
  }

  /**
   * Calculates the total cost for all selected print sizes.
   * Sums up all line totals across all selections.
   */
  getTotalCost(): number {
    return this.selectionsArray.controls.reduce((total, control, index) => {
      return total + this.getLineTotal(index);
    }, 0);
  }

  /**
   * Checks if there are valid selections to enable the Add to Cart button.
   * Requires at least one size to be selected with a quantity greater than 0.
   */
  hasValidSelections(): boolean {
    return this.selectionsArray.controls.some(control =>
      control.get('selected')?.value &&
      (control.get('quantity')?.value || 0) > 0
    );
  }

  /**
   * Determines print quality CSS class based on photo resolution vs print requirements.
   * Compares photo dimensions with recommended and minimum print size requirements.
   * Returns 'fair' if print size doesn't exist to prevent errors.
   */
  getQualityClass(index: number): string {
    const printSize = this.printSizes[index];
    if (!printSize) return 'fair';

    const photoWidth = this.photo.dimensions.width;
    const photoHeight = this.photo.dimensions.height;

    // Check against recommended resolution for excellent quality
    if (photoWidth >= printSize.recommendedWidth && photoHeight >= printSize.recommendedHeight) {
      return 'excellent';
    }
    // Check against minimum resolution for good quality
    else if (photoWidth >= printSize.minWidth && photoHeight >= printSize.minHeight) {
      return 'good';
    }
    // Below minimum resolution - fair quality with potential pixelation
    else {
      return 'fair';
    }
  }

  /**
   * Returns appropriate icon for quality rating.
   * Maps quality levels to Material Design icons.
   */
  getQualityIcon(index: number): string {
    const quality = this.getQualityClass(index);
    switch (quality) {
      case 'excellent': return 'star';
      case 'good': return 'star_half';
      default: return 'star_border';
    }
  }

  /**
   * Returns descriptive text for quality rating.
   * Provides user-friendly explanation of print quality expectations.
   */
  getQualityText(index: number): string {
    const quality = this.getQualityClass(index);
    switch (quality) {
      case 'excellent': return 'Excellent quality';
      case 'good': return 'Good quality';
      default: return 'Fair quality - may appear pixelated';
    }
  }

  /**
   * Adds selected print sizes to the shopping cart.
   * Builds PrintSelection array and calls cart service API.
   * Implements the multi-size selection requirement.
   */
  addToCart(): void {
    if (!this.hasValidSelections()) return;

    this.isAdding = true;

    // Build array of print selections from form data
    const printSelections: PrintSelection[] = [];

    this.selectionsArray.controls.forEach((control, index) => {
      if (control.get('selected')?.value) {
        const printSize = this.printSizes[index];
        const quantity = control.get('quantity')?.value || 0;

        // Only add if print size exists
        if (printSize) {
          printSelections.push({
            sizeCode: printSize.sizeCode,
            sizeName: printSize.displayName,
            quantity: quantity,
            unitPrice: printSize.price,
            lineTotal: quantity * printSize.price
          });
        }
      }
    });

    // Call cart service to add selections to cart
    this.cartService.addToCart(this.photo.id, printSelections).subscribe({
      next: (response) => {
        this.isAdding = false;
        if (response.success) {
          this.snackBar.open('Added to cart successfully!', 'Close', { duration: 3000 });
          this.dialogRef.close({ action: 'added', selections: printSelections });
        }
      },
      error: (error) => {
        this.isAdding = false;
        console.error('Error adding to cart:', error);
        this.snackBar.open('Failed to add to cart', 'Close', { duration: 3000 });
      }
    });
  }

  /**
   * Closes the dialog without making any changes.
   */
  close(): void {
    this.dialogRef.close();
  }
}
