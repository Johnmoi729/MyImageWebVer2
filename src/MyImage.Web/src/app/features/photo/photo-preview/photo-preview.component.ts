// src/MyImage.Web/src/app/features/photo/photo-preview/photo-preview.component.ts
import { Component, Inject, OnDestroy, OnInit } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { Subject, takeUntil } from 'rxjs';
import { PhotoService } from '../../../core/services/photo.service';
import { Photo } from '../../../shared/models/photo.models';

@Component({
  selector: 'app-photo-preview',
  standalone: false,
  template: `
    <div class="preview-container">
      <div class="preview-header">
        <h3>{{ photo.filename }}</h3>
        <button mat-icon-button (click)="close()" class="close-button">
          <mat-icon>close</mat-icon>
        </button>
      </div>

      <div class="preview-content">
        <!-- FIXED: Better loading and error state handling -->
        <div *ngIf="imageLoading" class="loading-state">
          <mat-spinner diameter="40"></mat-spinner>
          <p>Loading image...</p>
        </div>

        <img *ngIf="imageUrl && !imageError && !imageLoading"
             [src]="imageUrl"
             [alt]="photo.filename"
             class="preview-image"
             (load)="onImageLoad()"
             (error)="onImageError()"
             loading="lazy">

        <div *ngIf="imageError" class="error-state">
          <mat-icon>broken_image</mat-icon>
          <p>Unable to load image</p>
          <button mat-button (click)="retryImageLoad()" color="primary">
            <mat-icon>refresh</mat-icon>
            Retry
          </button>
        </div>
      </div>

      <div class="preview-info">
        <div class="info-row">
          <span class="label">Dimensions:</span>
          <span>{{ photo.dimensions.width }} × {{ photo.dimensions.height }} pixels</span>
        </div>
        <div class="info-row">
          <span class="label">File Size:</span>
          <span>{{ formatFileSize(photo.fileSize) }}</span>
        </div>
        <div class="info-row">
          <span class="label">Uploaded:</span>
          <span>{{ formatDate(photo.uploadedAt) }}</span>
        </div>
        <div class="info-row" *ngIf="photo.sourceFolder">
          <span class="label">Source:</span>
          <span>{{ photo.sourceFolder }}</span>
        </div>
      </div>

      <div class="preview-actions">
        <button mat-button (click)="close()">Close</button>
        <button mat-raised-button color="primary"
                [disabled]="photo.isOrdered"
                (click)="selectForPrinting()">
          <mat-icon>shopping_cart</mat-icon>
          Add to Cart
        </button>
      </div>
    </div>
  `,
  styles: [`
    .preview-container {
      display: flex;
      flex-direction: column;
      max-height: 90vh;
      width: 100%;
      max-width: 800px;
    }

    .preview-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 16px 24px;
      border-bottom: 1px solid #eee;
    }

    .preview-header h3 {
      margin: 0;
      font-weight: 500;
    }

    .preview-content {
      flex: 1;
      display: flex;
      justify-content: center;
      align-items: center;
      padding: 20px;
      overflow: hidden;
      min-height: 300px;
    }

    .loading-state, .error-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      color: #666;
      text-align: center;
      gap: 12px;
    }

    .error-state mat-icon {
      font-size: 48px;
      width: 48px;
      height: 48px;
      color: #ccc;
    }

    .preview-image {
      max-width: 100%;
      max-height: 60vh;
      object-fit: contain;
      border-radius: 4px;
      box-shadow: 0 2px 8px rgba(0,0,0,0.1);
    }

    .preview-info {
      padding: 16px 24px;
      border-top: 1px solid #eee;
      background: #fafafa;
    }

    .info-row {
      display: flex;
      justify-content: space-between;
      margin-bottom: 8px;
    }

    .info-row:last-child {
      margin-bottom: 0;
    }

    .label {
      font-weight: 500;
      color: #666;
    }

    .preview-actions {
      display: flex;
      justify-content: flex-end;
      gap: 16px;
      padding: 16px 24px;
      border-top: 1px solid #eee;
    }

    @media (max-width: 768px) {
      .preview-container {
        max-width: 100%;
        max-height: 100vh;
      }

      .preview-content {
        padding: 12px;
      }

      .preview-image {
        max-height: 50vh;
      }

      .info-row {
        flex-direction: column;
        gap: 4px;
      }
    }
  `]
})
export class PhotoPreviewComponent implements OnInit, OnDestroy {
  // FIXED: Better state management and cleanup
  private destroy$ = new Subject<void>();

  imageLoading = true;
  imageError = false;
  imageUrl: string | null = null;
  private loadAttempts = 0;
  private readonly maxLoadAttempts = 3;

  constructor(
    @Inject(MAT_DIALOG_DATA) public photo: Photo,
    private dialogRef: MatDialogRef<PhotoPreviewComponent>,
    private photoService: PhotoService
  ) {}

  ngOnInit(): void {
    console.log('PhotoPreview - Loading photo:', this.photo);
    this.loadImage();
    this.subscribeToInvalidation();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();

    // Clean up blob URL
    if (this.imageUrl) {
      URL.revokeObjectURL(this.imageUrl);
    }
  }

  /**
   * FIXED: Subscribe to cache invalidation events
   */
  private subscribeToInvalidation(): void {
    this.photoService.cacheInvalidated$
      .pipe(takeUntil(this.destroy$))
      .subscribe(invalidatedKey => {
        if (invalidatedKey && invalidatedKey.includes(this.photo.id)) {
          console.log('PhotoPreview - Cache invalidated, reloading image');
          this.imageUrl = null;
          this.loadImage();
        }
      });
  }

  /**
   * FIXED: Load image with retry logic and better error handling
   */
  private loadImage(): void {
    if (this.loadAttempts >= this.maxLoadAttempts) {
      console.error('PhotoPreview - Max load attempts reached');
      this.imageError = true;
      this.imageLoading = false;
      return;
    }

    this.imageLoading = true;
    this.imageError = false;
    this.loadAttempts++;

    console.log(`PhotoPreview - Loading image attempt ${this.loadAttempts}/${this.maxLoadAttempts}`);

    this.photoService.getDownloadUrl(this.photo.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (url) => {
          this.imageUrl = url;
          this.imageLoading = false;
          console.log('PhotoPreview - Image loaded successfully:', url);
        },
        error: (error) => {
          console.error('PhotoPreview - Failed to load image:', error);
          this.imageLoading = false;
          this.imageError = true;

          // Auto-retry for network errors
          if (error.status === 0 && this.loadAttempts < this.maxLoadAttempts) {
            console.log('PhotoPreview - Network error, auto-retrying in 2 seconds...');
            setTimeout(() => this.loadImage(), 2000);
          }
        }
      });
  }

  /**
   * Manual retry for image loading
   */
  retryImageLoad(): void {
    this.loadAttempts = 0; // Reset attempt counter
    this.loadImage();
  }

  onImageLoad(): void {
    this.imageLoading = false;
    this.imageError = false;
    this.loadAttempts = 0; // Reset on success
  }

  onImageError(): void {
    console.error('PhotoPreview - Image element failed to load, URL:', this.imageUrl);
    this.imageError = true;
    this.imageLoading = false;

    // Check if URL is still valid
    if (this.imageUrl && !this.photoService.isCacheValid(this.photo.id, 'download')) {
      console.log('PhotoPreview - Cache invalid, attempting reload');
      this.retryImageLoad();
    }
  }

  close(): void {
    this.dialogRef.close();
  }

  /**
   * Pass complete photo data to print selector
   */
  selectForPrinting(): void {
    // Ensure we have complete photo data for the print selector
    const photoData = {
      id: this.photo.id,
      filename: this.photo.filename,
      dimensions: this.photo.dimensions,
      fileSize: this.photo.fileSize,
      uploadedAt: this.photo.uploadedAt,
      thumbnailUrl: this.photo.thumbnailUrl,
      downloadUrl: this.photo.downloadUrl,
      isOrdered: this.photo.isOrdered,
      sourceFolder: this.photo.sourceFolder
    };

    console.log('PhotoPreview - Passing photo data to cart:', photoData);
    this.dialogRef.close({ action: 'addToCart', photo: photoData });
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
  }
}
