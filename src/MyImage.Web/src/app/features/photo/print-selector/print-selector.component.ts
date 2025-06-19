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
 * FIXED PrintSelectorComponent with comprehensive photo display and URL handling.
 * Resolves issues with photo thumbnails not displaying and improves error handling.
 *
 * Key fixes:
 * - Enhanced URL handling for photo thumbnails and downloads
 * - Better error handling for image loading failures
 * - Improved photo data validation and fallback mechanisms
 * - Enhanced debugging capabilities
 * - More robust API integration
 */
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
        <!-- Photo with comprehensive error handling -->
        <div class="photo-container">
          <img [src]="getPhotoThumbnailUrl()"
               [alt]="photo.filename"
               class="preview-thumb"
               (error)="onImageError($event)"
               (load)="onImageLoad()"
               [style.display]="imageLoading ? 'none' : 'block'">

          <!-- Loading spinner for image -->
          <div *ngIf="imageLoading" class="image-loading">
            <mat-spinner diameter="40"></mat-spinner>
            <p>Loading image...</p>
          </div>

          <!-- Fallback when image fails -->
          <div *ngIf="imageError" class="image-error">
            <mat-icon>broken_image</mat-icon>
            <span>Image not available</span>
            <small *ngIf="showDebugInfo">URL: {{ getPhotoThumbnailUrl() }}</small>
          </div>
        </div>

        <div class="photo-info">
          <h4>{{ photo.filename }}</h4>
          <p>{{ photo.dimensions.width }} × {{ photo.dimensions.height }} pixels</p>
          <p class="photo-id" *ngIf="showDebugInfo">Photo ID: {{ photo.id }}</p>
          <p class="photo-urls" *ngIf="showDebugInfo">
            <small>Thumbnail: {{ photo.thumbnailUrl }}</small><br>
            <small>Download: {{ photo.downloadUrl }}</small>
          </p>
        </div>
      </div>

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

      <!-- Loading state for print sizes -->
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

    .image-error small {
      font-size: 0.7em;
      color: #999;
      word-break: break-all;
      margin-top: 4px;
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

    .photo-id, .photo-urls {
      font-size: 0.8em !important;
      color: #999 !important;
    }

    .photo-urls small {
      font-size: 0.7em;
      color: #999;
      word-break: break-all;
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
export class PrintSelectorComponent implements OnInit {
  printForm: FormGroup;
  printSizes: PrintSize[] = [];
  isAdding = false;

  // Image state management
  imageLoading = true;
  imageError = false;

  // Loading states
  loadingPrintSizes = true;
  errorLoadingPrintSizes = false;

  // Debug flag - set to true to enable detailed debugging
  showDebugInfo = true; // FIXED: Enable debug info during integration

  constructor(
    @Inject(MAT_DIALOG_DATA) public photo: Photo,
    private dialogRef: MatDialogRef<PrintSelectorComponent>,
    private fb: FormBuilder,
    private printSizeService: PrintSizeService,
    private cartService: CartService,
    private snackBar: MatSnackBar
  ) {
    this.printForm = this.fb.group({
      selections: this.fb.array([])
    });
  }

  get selectionsArray(): FormArray {
    return this.printForm.get('selections') as FormArray;
  }

  ngOnInit(): void {
    console.log('PrintSelector - Photo data received:', this.photo);
    this.validatePhotoData();
    this.loadPrintSizes();
  }

  /**
   * FIXED: Validate photo data to ensure all required properties exist.
   */
  private validatePhotoData(): void {
    if (!this.photo) {
      console.error('PrintSelector - No photo data provided');
      this.snackBar.open('Invalid photo data', 'Close', { duration: 3000 });
      this.close();
      return;
    }

    const requiredFields = ['id', 'filename', 'dimensions'];
    const missingFields = requiredFields.filter(field => !this.photo[field as keyof Photo]);

    if (missingFields.length > 0) {
      console.error('PrintSelector - Missing required photo fields:', missingFields);
      this.snackBar.open('Incomplete photo data', 'Close', { duration: 3000 });
    }

    if (!this.photo.thumbnailUrl && !this.photo.downloadUrl) {
      console.warn('PrintSelector - No image URLs available for photo');
    }
  }

  trackByIndex(index: number, item: any): number {
    return index;
  }

  /**
   * FIXED: Get the proper thumbnail URL for the photo with enhanced error handling.
   * Now properly handles different URL formats and provides detailed debugging.
   */
  getPhotoThumbnailUrl(): string {
    if (!this.photo) {
      console.error('PrintSelector - No photo data provided');
      return '';
    }

    if (!this.photo.thumbnailUrl) {
      console.warn('PrintSelector - No thumbnail URL found for photo:', this.photo);
      // Try to use download URL as fallback
      if (this.photo.downloadUrl) {
        console.log('PrintSelector - Using download URL as fallback');
        return this.ensureAbsoluteUrl(this.photo.downloadUrl);
      }
      return '';
    }

    const finalUrl = this.ensureAbsoluteUrl(this.photo.thumbnailUrl);
    console.log('PrintSelector - Final thumbnail URL:', finalUrl);
    return finalUrl;
  }

  /**
   * FIXED: Helper method to ensure URLs are absolute with comprehensive handling.
   */
  private ensureAbsoluteUrl(url: string): string {
    if (!url) {
      return '';
    }

    // If URL is already absolute, return as-is
    if (url.startsWith('http://') || url.startsWith('https://')) {
      return url;
    }

    // If URL is relative, make it absolute
    if (url.startsWith('/')) {
      const baseUrl = `${window.location.protocol}//${window.location.host}`;
      return `${baseUrl}${url}`;
    }

    // If URL doesn't start with /, assume it's relative to API base
    const baseUrl = `${window.location.protocol}//${window.location.host}`;
    return `${baseUrl}/${url}`;
  }

  /**
   * Handle image load success.
   */
  onImageLoad(): void {
    this.imageLoading = false;
    this.imageError = false;
    console.log('PrintSelector - Image loaded successfully');
  }

  /**
   * FIXED: Enhanced image error handling with fallback attempts.
   */
  onImageError(event: any): void {
    console.error('PrintSelector - Image failed to load:', event);
    console.error('PrintSelector - Failed URL:', this.getPhotoThumbnailUrl());

    const img = event.target as HTMLImageElement;

    // Try download URL as fallback if we were using thumbnail URL
    if (this.photo.downloadUrl && img.src !== this.ensureAbsoluteUrl(this.photo.downloadUrl)) {
      console.log('PrintSelector - Trying download URL as fallback');
      img.src = this.ensureAbsoluteUrl(this.photo.downloadUrl);
      return; // Don't set error state yet, give fallback a chance
    }

    // All attempts failed
    this.imageLoading = false;
    this.imageError = true;
    console.error('PrintSelector - All image loading attempts failed');
  }

  /**
   * FIXED: Load print sizes with enhanced error handling and retry logic.
   */
  loadPrintSizes(): void {
    this.loadingPrintSizes = true;
    this.errorLoadingPrintSizes = false;

    console.log('PrintSelector - Loading print sizes...');

    this.printSizeService.getPrintSizes().subscribe({
      next: (response) => {
        this.loadingPrintSizes = false;
        console.log('PrintSelector - Print sizes API response:', response);

        if (response.success && response.data) {
          this.printSizes = response.data.filter((size: PrintSize) => size.isActive);
          this.initializeForm();
          console.log('PrintSelector - Loaded print sizes:', this.printSizes.length);
        } else {
          this.errorLoadingPrintSizes = true;
          console.error('PrintSelector - API returned success=false:', response.message);
          this.snackBar.open('Failed to load print sizes: ' + response.message, 'Close', { duration: 5000 });
        }
      },
      error: (error) => {
        this.loadingPrintSizes = false;
        this.errorLoadingPrintSizes = true;
        console.error('PrintSelector - Error loading print sizes:', error);

        // Provide detailed error information
        let errorMessage = 'Failed to load print sizes';
        if (error.status === 404) {
          errorMessage += ' - API endpoint not found (404)';
        } else if (error.status === 0) {
          errorMessage += ' - Network error or CORS issue';
        } else if (error.status >= 500) {
          errorMessage += ' - Server error';
        }

        this.snackBar.open(errorMessage, 'Close', { duration: 5000 });
      }
    });
  }

  private initializeForm(): void {
    const selectionsArray = this.fb.array(
      this.printSizes.map(size => this.fb.group({
        selected: [false],
        quantity: [1, [Validators.min(1), Validators.max(100)]],
        sizeCode: [size.sizeCode],
        unitPrice: [size.price]
      }))
    );

    this.printForm.setControl('selections', selectionsArray);
  }

  onSizeToggle(index: number, selected: boolean): void {
    if (!selected) {
      this.selectionsArray.at(index).get('quantity')?.setValue(1);
    }
    this.calculateTotal();
  }

  calculateTotal(): void {
    // Trigger change detection
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

  hasValidSelections(): boolean {
    return this.selectionsArray.controls.some(control =>
      control.get('selected')?.value &&
      (control.get('quantity')?.value || 0) > 0
    );
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

  /**
   * FIXED: Enhanced add to cart with comprehensive error handling and debugging.
   */
  addToCart(): void {
    if (!this.hasValidSelections()) return;

    this.isAdding = true;

    const printSelections: PrintSelection[] = [];

    this.selectionsArray.controls.forEach((control, index) => {
      if (control.get('selected')?.value) {
        const printSize = this.printSizes[index];
        const quantity = control.get('quantity')?.value || 0;

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
          console.error('PrintSelector - Add to cart failed:', response.message);
          this.snackBar.open('Failed to add to cart: ' + response.message, 'Close', { duration: 5000 });
        }
      },
      error: (error) => {
        this.isAdding = false;
        console.error('PrintSelector - Error adding to cart:', error);

        let errorMessage = 'Failed to add to cart';
        if (error.status === 401) {
          errorMessage += ' - Please log in again';
        } else if (error.status === 404) {
          errorMessage += ' - Photo or cart service not found';
        } else if (error.status >= 500) {
          errorMessage += ' - Server error';
        }

        this.snackBar.open(errorMessage, 'Close', { duration: 5000 });
      }
    });
  }

  close(): void {
    this.dialogRef.close();
  }
}
