import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { Photo } from '../../../shared/models/photo.models';

@Component({
  selector: 'app-photo-preview',
  template: `
    <div class="preview-container">
      <div class="preview-header">
        <h3>{{ photo.filename }}</h3>
        <button mat-icon-button (click)="close()" class="close-button">
          <mat-icon>close</mat-icon>
        </button>
      </div>

      <div class="preview-content">
        <img [src]="photo.downloadUrl"
             [alt]="photo.filename"
             class="preview-image"
             loading="lazy">
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
  `]
})
export class PhotoPreviewComponent {
  constructor(
    @Inject(MAT_DIALOG_DATA) public photo: Photo,
    private dialogRef: MatDialogRef<PhotoPreviewComponent>
  ) {}

  close(): void {
    this.dialogRef.close();
  }

  selectForPrinting(): void {
    this.dialogRef.close({ action: 'addToCart', photo: this.photo });
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
