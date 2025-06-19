// src/MyImage.Web/src/app/features/photo/print-selector/print-selector.component.ts
import { Component, Inject, OnDestroy, OnInit } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';

import { CartService } from '../../../core/services/cart.service';
import { PhotoService } from '../../../core/services/photo.service';
import { PrintSizeService } from '../../../core/services/print-size.service';
import { PrintSelection } from '../../../shared/models/cart.models';
import { Photo } from '../../../shared/models/photo.models';
import { PrintSize } from '../../../shared/models/print-size.models';

@Component({
  selector: 'app-print-selector',
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
        <!-- FIXED: Photo with authenticated image loading -->
        <div class="photo-container">
          <!-- Loading state -->
          <div *ngIf="imageLoading" class="image-loading">
            <mat-spinner diameter="40"></mat-spinner>
            <p>Loading image...</p>
          </div>

          <!-- Image display -->
          <img *ngIf="photoImageUrl && !imageError"
               [src]="photoImageUrl"
               [alt]="photo.filename"
               class="preview-thumb"
               (load)="onImageLoad()"
               (error)="onImageError()">

          <!-- Error state -->
          <div *ngIf="imageError" class="image-error">
            <mat-icon>broken_image</mat-icon>
            <span>Image not available</span>
          </div>
        </div>

        <div class="photo-info">
          <h4>{{ photo.filename }}</h4>
          <p>{{ photo.dimensions.width }} × {{ photo.dimensions.height }} pixels</p>
        </div>
      </div>

      <!-- Print Form - FIXED: Proper FormArray handling -->
      <form [formGroup]="printForm" class="print-form" *ngIf="printSizes.length > 0">
        <div class="size-selections" formArrayName="selections">
          <ng-container *ngFor="let selection of selectionsArray.controls; let i = index; trackBy: trackByIndex">
            <div class="size-item"
                 [formGroupName]="i"
                 *ngIf="printSizes[i]">

              <div class="size-header">
                <mat-checkbox formControlName="selected"
                             (change)="onSizeToggle(i, $event.checked)">
                  <div class="size-info">
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

              <!-- FIXED: Quantity section with proper form field structure -->
              <div class="quantity-section" *ngIf="selection.get('selected')?.value">
                <mat-form-field appearance="outline" class="quantity-field">
                  <mat-label>Quantity</mat-label>
                  <input matInput
                         type="number"
                         formControlName="quantity"
                         min="1"
                         max="100"
                         (input)="calculateTotal()">
                  <mat-error *ngIf="selection.get('quantity')?.hasError('required')">
                    Quantity is required
                  </mat-error>
                  <mat-error *ngIf="selection.get('quantity')?.hasError('min')">
                    Minimum quantity is 1
                  </mat-error>
                  <mat-error *ngIf="selection.get('quantity')?.hasError('max')">
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
          <button mat-raised-button
                  color="primary"
                  [disabled]="!hasValidSelections() || isAdding"
                  (click)="addToCart()">
            <mat-spinner diameter="20" *ngIf="isAdding"></mat-spinner>
            <span *ngIf="!isAdding">Add to Cart ({{ getSelectedCount() }})</span>
          </button>
        </div>
      </form>

      <!-- Loading print sizes -->
      <div *ngIf="loadingPrintSizes" class="loading-print-sizes">
        <mat-spinner diameter="40"></mat-spinner>
        <p>Loading print sizes...</p>
      </div>

      <!-- Error state -->
      <div *ngIf="errorLoadingPrintSizes" class="error-state">
        <mat-icon color="warn">error</mat-icon>
        <p>Failed to load print sizes. Please try again.</p>
        <button mat-button (click)="loadPrintSizes()">Retry</button>
      </div>
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

    .photo-container {
      position: relative;
      width: 80px;
      height: 80px;
      border-radius: 4px;
      overflow: hidden;
      background: #f0f0f0;
      display: flex;
      align-items: center;
      justify-content: center;
      border: 1px solid #ddd;
    }

    .preview-thumb {
      width: 80px;
      height: 80px;
      object-fit: cover;
      border-radius: 4px;
    }

    .image-loading {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      width: 100%;
      height: 100%;
      font-size: 0.8em;
      color: #666;
    }

    .image-error {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      width: 100%;
      height: 100%;
      color: #666;
      font-size: 0.8em;
      text-align: center;
    }

    .image-error mat-icon {
      font-size: 24px;
      width: 24px;
      height: 24px;
      margin-bottom: 4px;
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

    .loading-print-sizes, .error-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 40px;
      text-align: center;
    }

    .loading-print-sizes mat-spinner,
    .error-state mat-icon {
      margin-bottom: 16px;
    }
  `]
})
export class PrintSelectorComponent implements OnInit, OnDestroy {
  printForm: FormGroup;
  printSizes: PrintSize[] = [];
  isAdding = false;

  // FIXED: Image state management with authenticated loading
  imageLoading = true;
  imageError = false;
  photoImageUrl: string | null = null;

  // Loading states
  loadingPrintSizes = true;
  errorLoadingPrintSizes = false;

  constructor(
    @Inject(MAT_DIALOG_DATA) public photo: Photo,
    private dialogRef: MatDialogRef<PrintSelectorComponent>,
    private fb: FormBuilder,
    private printSizeService: PrintSizeService,
    private cartService: CartService,
    private photoService: PhotoService,
    private snackBar: MatSnackBar
  ) {
    // FIXED: Initialize form properly
    this.printForm = this.fb.group({
      selections: this.fb.array([])
    });
  }

  get selectionsArray(): FormArray {
    return this.printForm.get('selections') as FormArray;
  }

  ngOnInit(): void {
    console.log('PrintSelector - Photo data received:', this.photo);
    this.loadPhotoImage();
    this.loadPrintSizes();
  }

  ngOnDestroy(): void {
    // FIXED: Clean up blob URL when component is destroyed
    if (this.photoImageUrl) {
      URL.revokeObjectURL(this.photoImageUrl);
    }
  }

  /**
   * FIXED: Load authenticated photo image with proper error handling.
   */
  private loadPhotoImage(): void {
    this.imageLoading = true;
    this.imageError = false;

    console.log('PrintSelector - Loading authenticated image for photo:', this.photo.id);

    // FIXED: Use photo service with proper error handling
    this.photoService.getThumbnailUrl(this.photo.id).subscribe({
      next: (blobUrl) => {
        this.photoImageUrl = blobUrl;
        this.imageLoading = false;
        console.log('PrintSelector - Image loaded successfully');
      },
      error: (error) => {
        console.error('PrintSelector - Failed to load image:', error);
        this.imageLoading = false;
        this.imageError = true;
      }
    });
  }

  trackByIndex(index: number, item: any): number {
    return index;
  }

  onImageLoad(): void {
    this.imageLoading = false;
    this.imageError = false;
    console.log('PrintSelector - Image element loaded successfully');
  }

  onImageError(): void {
    console.error('PrintSelector - Image element failed to load');
    this.imageError = true;
    this.imageLoading = false;
  }

  loadPrintSizes(): void {
    this.loadingPrintSizes = true;
    this.errorLoadingPrintSizes = false;

    this.printSizeService.getPrintSizes().subscribe({
      next: (response) => {
        this.loadingPrintSizes = false;
        console.log('PrintSelector - Print sizes response:', response);

        if (response.success && response.data) {
          this.printSizes = response.data.filter((size: PrintSize) => size.isActive);
          this.initializeForm();
          console.log('PrintSelector - Loaded print sizes:', this.printSizes.length);
        } else {
          this.errorLoadingPrintSizes = true;
          this.snackBar.open('Failed to load print sizes: ' + response.message, 'Close', { duration: 5000 });
        }
      },
      error: (error) => {
        this.loadingPrintSizes = false;
        this.errorLoadingPrintSizes = true;
        console.error('PrintSelector - Error loading print sizes:', error);

        let errorMessage = 'Failed to load print sizes';
        if (error.status === 404) {
          errorMessage += ' - Print sizes endpoint not found';
        } else if (error.status === 0) {
          errorMessage += ' - Network error';
        } else if (error.status >= 500) {
          errorMessage += ' - Server error';
        }

        this.snackBar.open(errorMessage, 'Close', { duration: 5000 });
      }
    });
  }

  /**
   * FIXED: Initialize form with proper validation
   */
  private initializeForm(): void {
    const selectionsArray = this.fb.array(
      this.printSizes.map(size => this.fb.group({
        selected: [false],
        quantity: [1, [Validators.required, Validators.min(1), Validators.max(100)]],
        sizeCode: [size.sizeCode],
        unitPrice: [size.price]
      }))
    );

    this.printForm.setControl('selections', selectionsArray);
    console.log('PrintSelector - Form initialized with', selectionsArray.length, 'size options');
  }

  onSizeToggle(index: number, selected: boolean): void {
    console.log('PrintSelector - Size toggle:', index, selected);

    if (!selected) {
      // Reset quantity to 1 when unchecked
      this.selectionsArray.at(index).get('quantity')?.setValue(1);
    }
    this.calculateTotal();
  }

  calculateTotal(): void {
    // Triggers change detection for reactive calculations
    console.log('PrintSelector - Total cost:', this.getTotalCost());
  }

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

  getTotalCost(): number {
    return this.selectionsArray.controls.reduce((total, control, index) => {
      return total + this.getLineTotal(index);
    }, 0);
  }

  /**
   * FIXED: Improved validation logic
   */
  hasValidSelections(): boolean {
    const hasSelections = this.selectionsArray.controls.some(control => {
      const selected = control.get('selected')?.value;
      const quantity = control.get('quantity')?.value || 0;
      const quantityValid = control.get('quantity')?.valid;

      return selected && quantity > 0 && quantityValid;
    });

    console.log('PrintSelector - Has valid selections:', hasSelections);
    return hasSelections;
  }

  /**
   * FIXED: Get count of selected items for button text
   */
  getSelectedCount(): number {
    return this.selectionsArray.controls.filter(control =>
      control.get('selected')?.value
    ).length;
  }

  getQualityClass(index: number): string {
    const printSize = this.printSizes[index];
    if (!printSize) return 'fair';

    const photoWidth = this.photo.dimensions.width;
    const photoHeight = this.photo.dimensions.height;

    if (photoWidth >= printSize.recommendedWidth && photoHeight >= printSize.recommendedHeight) {
      return 'excellent';
    } else if (photoWidth >= printSize.minWidth && photoHeight >= printSize.minHeight) {
      return 'good';
    } else {
      return 'fair';
    }
  }

  getQualityIcon(index: number): string {
    const quality = this.getQualityClass(index);
    switch (quality) {
      case 'excellent': return 'star';
      case 'good': return 'star_half';
      default: return 'star_border';
    }
  }

  getQualityText(index: number): string {
    const quality = this.getQualityClass(index);
    switch (quality) {
      case 'excellent': return 'Excellent quality';
      case 'good': return 'Good quality';
      default: return 'Fair quality - may appear pixelated';
    }
  }

  addToCart(): void {
    if (!this.hasValidSelections()) {
      this.snackBar.open('Please select at least one print size', 'Close', { duration: 3000 });
      return;
    }

    this.isAdding = true;

    const printSelections: PrintSelection[] = [];

    this.selectionsArray.controls.forEach((control, index) => {
      if (control.get('selected')?.value && control.valid) {
        const printSize = this.printSizes[index];
        const quantity = control.get('quantity')?.value || 0;

        if (printSize && quantity > 0) {
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

    if (printSelections.length === 0) {
      this.isAdding = false;
      this.snackBar.open('No valid selections found', 'Close', { duration: 3000 });
      return;
    }

    console.log('PrintSelector - Adding to cart:', {
      photoId: this.photo.id,
      printSelections,
      totalCost: this.getTotalCost()
    });

    this.cartService.addToCart(this.photo.id, printSelections).subscribe({
      next: (response) => {
        this.isAdding = false;
        console.log('PrintSelector - Add to cart response:', response);

        if (response.success) {
          this.snackBar.open('Added to cart successfully!', 'Close', { duration: 3000 });
          this.dialogRef.close({ action: 'added', selections: printSelections });
        } else {
          const errorMessage = response.message || 'Failed to add to cart';
          const errors = response.errors?.join(', ') || '';
          this.snackBar.open(`${errorMessage}${errors ? ': ' + errors : ''}`, 'Close', { duration: 5000 });
        }
      },
      error: (error) => {
        this.isAdding = false;
        console.error('PrintSelector - Error adding to cart:', error);

        let errorMessage = 'Failed to add to cart';
        if (error.status === 401) {
          errorMessage += ' - Please log in again';
        } else if (error.status === 404) {
          errorMessage += ' - Service not found';
        } else if (error.status === 400) {
          errorMessage += ' - Invalid request data';
        } else if (error.status >= 500) {
          errorMessage += ' - Server error';
        } else if (error.status === 0) {
          errorMessage += ' - Network error';
        }

        this.snackBar.open(errorMessage, 'Close', { duration: 5000 });
      }
    });
  }

  close(): void {
    this.dialogRef.close();
  }
}
