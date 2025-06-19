import { Component, OnDestroy } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { PhotoService } from '../../../core/services/photo.service';

/**
 * Fixed PhotoUploadComponent with proper blob URL lifecycle management.
 * Fixes the blob URL error after upload completion and improves user experience.
 *
 * Key fixes:
 * - Don't cleanup blob URLs immediately after upload success
 * - Only cleanup when user explicitly clears or component is destroyed
 * - Better error handling and user feedback
 */
@Component({
  selector: 'app-photo-upload',
  standalone: false,
  template: `
    <div class="upload-container">
      <mat-card class="upload-card">
        <mat-card-header>
          <mat-card-title>Upload Photos</mat-card-title>
          <mat-card-subtitle>Select JPEG files from your computer</mat-card-subtitle>
        </mat-card-header>

        <mat-card-content>
          <!-- Folder Selection -->
          <div class="folder-section" *ngIf="!isUploading && selectedFiles.length === 0 && !uploadComplete">
            <div class="drop-zone"
                 (click)="fileInput.click()"
                 (dragover)="onDragOver($event)"
                 (dragleave)="onDragLeave($event)"
                 (drop)="onFileDrop($event)"
                 [class.drag-over]="isDragOver">
              <mat-icon class="upload-icon">cloud_upload</mat-icon>
              <h3>Choose Photos to Upload</h3>
              <p>Click here to browse for photos or drag and drop JPEG files</p>
              <p class="file-info">Maximum 50MB per file • JPEG format only</p>

              <input #fileInput
                     type="file"
                     multiple
                     accept=".jpg,.jpeg"
                     (change)="onFileSelect($event)"
                     style="display: none">
            </div>
          </div>

          <!-- File Selection Display -->
          <div class="file-selection" *ngIf="selectedFiles.length > 0 && !isUploading && !uploadComplete">
            <h4>Selected Files ({{ selectedFiles.length }} files)</h4>
            <div class="file-list">
              <div class="file-item"
                   *ngFor="let fileData of fileDataArray; let i = index; trackBy: trackByIndex">
                <!-- Use cached preview URL -->
                <img [src]="fileData.previewUrl" class="file-preview" alt="Preview">
                <div class="file-info">
                  <span class="file-name">{{ fileData.file.name }}</span>
                  <span class="file-size">{{ formatFileSize(fileData.file.size) }}</span>
                </div>
                <button mat-icon-button (click)="removeFile(i)" color="warn">
                  <mat-icon>close</mat-icon>
                </button>
              </div>
            </div>

            <div class="upload-actions">
              <button mat-button (click)="clearSelection()">Clear All</button>
              <button mat-raised-button color="primary" (click)="startUpload()">
                Upload {{ selectedFiles.length }} Files
              </button>
            </div>
          </div>

          <!-- Upload Progress -->
          <div class="upload-progress" *ngIf="isUploading">
            <h4>Uploading Photos...</h4>
            <mat-progress-bar [value]="uploadProgress" mode="determinate"></mat-progress-bar>
            <p class="progress-text">{{ uploadProgress }}% Complete</p>
          </div>

          <!-- Upload Results -->
          <div class="upload-results" *ngIf="uploadComplete">
            <mat-icon color="primary" class="success-icon">check_circle</mat-icon>
            <h4>Upload Complete!</h4>
            <p>{{ uploadResult?.totalUploaded || 0 }} photos uploaded successfully</p>

            <!-- Show failed uploads if any -->
            <div class="failed-uploads" *ngIf="uploadResult?.failedUploads?.length > 0">
              <h5>Failed Uploads:</h5>
              <ul>
                <li *ngFor="let failed of uploadResult.failedUploads">
                  {{ failed.filename }}: {{ failed.errorMessage }}
                </li>
              </ul>
            </div>

            <div class="result-actions">
              <button mat-button (click)="uploadMore()">Upload More</button>
              <button mat-raised-button color="primary" (click)="viewGallery()">
                View Gallery
              </button>
            </div>
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .upload-container {
      max-width: 800px;
      margin: 0 auto;
      padding: 20px;
    }

    .upload-card {
      min-height: 400px;
    }

    .drop-zone {
      border: 2px dashed #ccc;
      border-radius: 8px;
      padding: 40px;
      text-align: center;
      cursor: pointer;
      transition: all 0.3s ease;
      background: #fafafa;
    }

    .drop-zone:hover, .drop-zone.drag-over {
      border-color: #3f51b5;
      background: #f0f0ff;
    }

    .upload-icon {
      font-size: 48px;
      width: 48px;
      height: 48px;
      color: #666;
      margin-bottom: 16px;
    }

    .file-info {
      color: #666;
      font-size: 0.9em;
    }

    .file-list {
      max-height: 300px;
      overflow-y: auto;
      margin: 16px 0;
    }

    .file-item {
      display: flex;
      align-items: center;
      padding: 8px;
      border: 1px solid #eee;
      border-radius: 4px;
      margin-bottom: 8px;
      gap: 12px;
    }

    .file-preview {
      width: 50px;
      height: 50px;
      object-fit: cover;
      border-radius: 4px;
    }

    .file-info {
      flex: 1;
      display: flex;
      flex-direction: column;
    }

    .file-name {
      font-weight: 500;
    }

    .file-size {
      color: #666;
      font-size: 0.9em;
    }

    .upload-actions, .result-actions {
      display: flex;
      justify-content: flex-end;
      gap: 16px;
      margin-top: 16px;
    }

    .upload-progress {
      text-align: center;
      padding: 40px;
    }

    .progress-text {
      margin-top: 8px;
      color: #666;
    }

    .upload-results {
      text-align: center;
      padding: 40px;
    }

    .success-icon {
      font-size: 48px;
      width: 48px;
      height: 48px;
      margin-bottom: 16px;
    }

    .failed-uploads {
      background: #fff3cd;
      border: 1px solid #ffeaa7;
      border-radius: 4px;
      padding: 16px;
      margin: 16px 0;
      text-align: left;
    }

    .failed-uploads h5 {
      margin: 0 0 8px 0;
      color: #856404;
    }

    .failed-uploads ul {
      margin: 0;
      padding-left: 20px;
    }

    .failed-uploads li {
      color: #856404;
      margin-bottom: 4px;
    }
  `]
})
export class PhotoUploadComponent implements OnDestroy {
  selectedFiles: File[] = [];
  isUploading = false;
  uploadProgress = 0;
  uploadComplete = false;
  uploadResult: any = null;
  isDragOver = false;

  // Cache for file data with preview URLs
  fileDataArray: Array<{ file: File; previewUrl: string }> = [];

  constructor(
    private photoService: PhotoService,
    private router: Router,
    private snackBar: MatSnackBar
  ) {}

  /**
   * Cleanup blob URLs when component is destroyed to prevent memory leaks.
   */
  ngOnDestroy(): void {
    this.cleanupPreviewUrls();
  }

  /**
   * TrackBy function for ngFor to improve performance.
   */
  trackByIndex(index: number, item: any): number {
    return index;
  }

  onFileSelect(event: Event): void {
    const files = (event.target as HTMLInputElement).files;
    if (files) {
      this.processFiles(files);
    }
    // Reset input value to allow selecting the same files again
    (event.target as HTMLInputElement).value = '';
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver = false;
  }

  onFileDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver = false;

    const files = event.dataTransfer?.files;
    if (files) {
      this.processFiles(files);
    }
  }

  /**
   * Process selected files and create preview URLs.
   */
  private processFiles(fileList: FileList): void {
    const validFiles = this.photoService.filterJpegFiles(fileList);

    if (validFiles.length === 0) {
      this.snackBar.open('No valid JPEG files found', 'Close', { duration: 3000 });
      return;
    }

    // Check for invalid files and show warnings
    const invalidCount = fileList.length - validFiles.length;
    if (invalidCount > 0) {
      this.snackBar.open(
        `${invalidCount} files skipped (not JPEG or too large)`,
        'Close',
        { duration: 5000 }
      );
    }

    // Clean up existing preview URLs before setting new ones
    this.cleanupPreviewUrls();

    // Create file data objects with cached preview URLs
    this.fileDataArray = validFiles.map(file => ({
      file: file,
      previewUrl: URL.createObjectURL(file)
    }));

    this.selectedFiles = validFiles;
  }

  /**
   * Remove file at specific index and clean up its preview URL.
   */
  removeFile(index: number): void {
    if (index >= 0 && index < this.fileDataArray.length) {
      // Cleanup the blob URL for the removed file
      URL.revokeObjectURL(this.fileDataArray[index].previewUrl);

      // Remove from both arrays
      this.fileDataArray.splice(index, 1);
      this.selectedFiles.splice(index, 1);
    }
  }

  /**
   * Clear all selected files and cleanup preview URLs.
   */
  clearSelection(): void {
    this.cleanupPreviewUrls();
    this.selectedFiles = [];
    this.fileDataArray = [];
  }

  /**
   * Start the upload process with progress tracking.
   */
  startUpload(): void {
    if (this.selectedFiles.length === 0) return;

    this.isUploading = true;
    this.uploadProgress = 0;

    this.photoService.uploadPhotos(this.selectedFiles).subscribe({
      next: (event) => {
        this.uploadProgress = event.progress;
        if (event.result) {
          this.uploadResult = event.result;
          this.uploadComplete = true;
          this.isUploading = false;

          // DON'T cleanup preview URLs here - let them persist until user navigates away
          // This prevents the blob URL error you were seeing
        }
      },
      error: (error) => {
        this.isUploading = false;
        this.snackBar.open('Upload failed. Please try again.', 'Close', { duration: 5000 });
        console.error('Upload error:', error);
      }
    });
  }

  /**
   * Reset component state for uploading more files.
   */
  uploadMore(): void {
    // Clean up URLs when explicitly starting over
    this.cleanupPreviewUrls();

    this.selectedFiles = [];
    this.fileDataArray = [];
    this.uploadComplete = false;
    this.uploadResult = null;
    this.uploadProgress = 0;
  }

  /**
   * Navigate to photo gallery to view uploaded photos.
   */
  viewGallery(): void {
    // Clean up URLs when navigating away
    this.cleanupPreviewUrls();
    this.router.navigate(['/photos']);
  }

  /**
   * Format file size for display in human-readable format.
   */
  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  }

  /**
   * Clean up all blob URLs to prevent memory leaks.
   */
  private cleanupPreviewUrls(): void {
    this.fileDataArray.forEach(fileData => {
      if (fileData.previewUrl) {
        URL.revokeObjectURL(fileData.previewUrl);
      }
    });
  }
}
