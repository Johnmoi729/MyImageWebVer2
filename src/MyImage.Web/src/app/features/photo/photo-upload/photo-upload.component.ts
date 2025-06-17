import { Component } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { PhotoService } from '../../../core/services/photo.service';

@Component({
  selector: 'app-photo-upload',
  template: `
    <div class="upload-container">
      <mat-card class="upload-card">
        <mat-card-header>
          <mat-card-title>Upload Photos</mat-card-title>
          <mat-card-subtitle>Select JPEG files from your computer</mat-card-subtitle>
        </mat-card-header>

        <mat-card-content>
          <!-- Folder Selection -->
          <div class="folder-section" *ngIf="!isUploading && selectedFiles.length === 0">
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
          <div class="file-selection" *ngIf="selectedFiles.length > 0 && !isUploading">
            <h4>Selected Files ({{ selectedFiles.length }} files)</h4>
            <div class="file-list">
              <div class="file-item" *ngFor="let file of selectedFiles; let i = index">
                <img [src]="getFilePreview(file)" class="file-preview" alt="Preview">
                <div class="file-info">
                  <span class="file-name">{{ file.name }}</span>
                  <span class="file-size">{{ formatFileSize(file.size) }}</span>
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
            <p>{{ uploadResult?.totalUploaded }} photos uploaded successfully</p>

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
  `]
})
export class PhotoUploadComponent {
  selectedFiles: File[] = [];
  isUploading = false;
  uploadProgress = 0;
  uploadComplete = false;
  uploadResult: any = null;
  isDragOver = false;

  constructor(
    private photoService: PhotoService,
    private router: Router,
    private snackBar: MatSnackBar
  ) {}

  onFileSelect(event: Event): void {
    const files = (event.target as HTMLInputElement).files;
    if (files) {
      this.processFiles(files);
    }
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

    this.selectedFiles = validFiles;
  }

  removeFile(index: number): void {
    this.selectedFiles.splice(index, 1);
  }

  clearSelection(): void {
    this.selectedFiles = [];
  }

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
        }
      },
      error: (error) => {
        this.isUploading = false;
        this.snackBar.open('Upload failed. Please try again.', 'Close', { duration: 5000 });
      }
    });
  }

  uploadMore(): void {
    this.selectedFiles = [];
    this.uploadComplete = false;
    this.uploadResult = null;
    this.uploadProgress = 0;
  }

  viewGallery(): void {
    this.router.navigate(['/photos']);
  }

  getFilePreview(file: File): string {
    return URL.createObjectURL(file);
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  }
}
