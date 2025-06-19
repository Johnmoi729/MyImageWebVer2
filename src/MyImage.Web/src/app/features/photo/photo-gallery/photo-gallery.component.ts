// src/MyImage.Web/src/app/features/photo/photo-gallery/photo-gallery.component.ts
import { Component, OnDestroy, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { PageEvent } from '@angular/material/paginator';
import { MatSnackBar } from '@angular/material/snack-bar';
import { PhotoService } from '../../../core/services/photo.service';
import { Photo } from '../../../shared/models/photo.models';
import { PhotoPreviewComponent } from '../photo-preview/photo-preview.component';
import { PrintSelectorComponent } from '../print-selector/print-selector.component';

@Component({
  selector: 'app-photo-gallery',
  standalone: false,
  template: `
    <div class="gallery-container">
      <div class="gallery-header">
        <h2>My Photos</h2>
        <button mat-raised-button color="primary" routerLink="/photos/upload">
          <mat-icon>add</mat-icon>
          Upload Photos
        </button>
      </div>

      <div class="gallery-stats" *ngIf="totalPhotos > 0">
        <mat-chip-listbox>
          <mat-chip>{{ totalPhotos }} Photos</mat-chip>
          <mat-chip>{{ availablePhotos }} Available for Printing</mat-chip>
        </mat-chip-listbox>
      </div>

      <!-- Photos Grid -->
      <div class="photos-grid" *ngIf="photos.length > 0">
        <mat-card class="photo-card" *ngFor="let photo of photos; trackBy: trackByPhotoId">
          <div class="photo-container" (click)="openPhotoPreview(photo)">
            <!-- FIXED: Use authenticated blob URL with loading states -->
            <div *ngIf="!getPhotoImageUrl(photo.id)" class="photo-loading">
              <mat-spinner diameter="30"></mat-spinner>
            </div>

            <img *ngIf="getPhotoImageUrl(photo.id)"
                 [src]="getPhotoImageUrl(photo.id)"
                 [alt]="photo.filename"
                 class="photo-thumbnail"
                 loading="lazy"
                 (error)="onImageError(photo.id)">

            <!-- Error state -->
            <div *ngIf="imageErrors.has(photo.id)" class="photo-error">
              <mat-icon>broken_image</mat-icon>
              <span>Image unavailable</span>
            </div>

            <div class="photo-overlay" *ngIf="getPhotoImageUrl(photo.id) && !imageErrors.has(photo.id)">
              <mat-icon class="zoom-icon">zoom_in</mat-icon>
            </div>
          </div>

          <mat-card-content class="photo-info">
            <div class="photo-details">
              <span class="photo-name">{{ photo.filename }}</span>
              <span class="photo-size">{{ photo.dimensions.width }} × {{ photo.dimensions.height }}</span>
            </div>

            <div class="photo-actions">
              <button mat-icon-button (click)="selectForPrinting(photo)"
                      [disabled]="photo.isOrdered"
                      matTooltip="Add to Cart">
                <mat-icon>shopping_cart</mat-icon>
              </button>
              <button mat-icon-button (click)="deletePhoto(photo)"
                      [disabled]="photo.isOrdered"
                      color="warn"
                      matTooltip="Delete Photo">
                <mat-icon>delete</mat-icon>
              </button>
            </div>
          </mat-card-content>

          <div class="ordered-badge" *ngIf="photo.isOrdered">
            <mat-icon>check_circle</mat-icon>
            Ordered
          </div>
        </mat-card>
      </div>

      <!-- Empty State -->
      <div class="empty-state" *ngIf="photos.length === 0 && !isLoading">
        <mat-icon class="empty-icon">photo_library</mat-icon>
        <h3>No Photos Yet</h3>
        <p>Upload your first photos to start printing</p>
        <button mat-raised-button color="primary" routerLink="/photos/upload">
          <mat-icon>add</mat-icon>
          Upload Photos
        </button>
      </div>

      <!-- Loading -->
      <div class="loading-container" *ngIf="isLoading">
        <mat-spinner></mat-spinner>
        <p>Loading photos...</p>
      </div>

      <!-- Pagination -->
      <mat-paginator
        *ngIf="totalPhotos > pageSize"
        [length]="totalPhotos"
        [pageSize]="pageSize"
        [pageSizeOptions]="[12, 24, 48]"
        (page)="onPageChange($event)"
        showFirstLastButtons>
      </mat-paginator>
    </div>
  `,
  styles: [`
    .gallery-container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 20px;
    }

    .gallery-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
    }

    .gallery-stats {
      margin-bottom: 20px;
    }

    .photos-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
      gap: 20px;
      margin-bottom: 20px;
    }

    .photo-card {
      position: relative;
      overflow: hidden;
    }

    .photo-container {
      position: relative;
      width: 100%;
      height: 200px;
      cursor: pointer;
      overflow: hidden;
      background: #f5f5f5;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .photo-thumbnail {
      width: 100%;
      height: 100%;
      object-fit: cover;
      transition: transform 0.3s ease;
    }

    .photo-container:hover .photo-thumbnail {
      transform: scale(1.05);
    }

    .photo-loading {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 100%;
      height: 100%;
      color: #666;
    }

    .photo-error {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      width: 100%;
      height: 100%;
      color: #999;
      font-size: 0.9em;
    }

    .photo-error mat-icon {
      font-size: 32px;
      width: 32px;
      height: 32px;
      margin-bottom: 8px;
    }

    .photo-overlay {
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0, 0, 0, 0.5);
      display: flex;
      align-items: center;
      justify-content: center;
      opacity: 0;
      transition: opacity 0.3s ease;
    }

    .photo-container:hover .photo-overlay {
      opacity: 1;
    }

    .zoom-icon {
      color: white;
      font-size: 32px;
      width: 32px;
      height: 32px;
    }

    .photo-info {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 12px 16px;
    }

    .photo-details {
      display: flex;
      flex-direction: column;
      flex: 1;
    }

    .photo-name {
      font-weight: 500;
      margin-bottom: 4px;
    }

    .photo-size {
      color: #666;
      font-size: 0.9em;
    }

    .photo-actions {
      display: flex;
      gap: 4px;
    }

    .ordered-badge {
      position: absolute;
      top: 8px;
      right: 8px;
      background: #4caf50;
      color: white;
      padding: 4px 8px;
      border-radius: 12px;
      font-size: 0.8em;
      display: flex;
      align-items: center;
      gap: 4px;
    }

    .ordered-badge mat-icon {
      font-size: 16px;
      width: 16px;
      height: 16px;
    }

    .empty-state, .loading-container {
      text-align: center;
      padding: 60px 20px;
    }

    .empty-icon {
      font-size: 64px;
      width: 64px;
      height: 64px;
      color: #ccc;
      margin-bottom: 16px;
    }

    @media (max-width: 768px) {
      .photos-grid {
        grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
        gap: 16px;
      }

      .gallery-header {
        flex-direction: column;
        gap: 16px;
        align-items: stretch;
      }
    }
  `]
})
export class PhotoGalleryComponent implements OnInit, OnDestroy {
  photos: Photo[] = [];
  totalPhotos = 0;
  availablePhotos = 0;
  pageSize = 12;
  currentPage = 1;
  isLoading = false;

  // FIXED: Track image URLs and errors
  photoImageUrls = new Map<string, string>();
  imageErrors = new Set<string>();

  constructor(
    private photoService: PhotoService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadPhotos();
  }

  ngOnDestroy(): void {
    // FIXED: Clean up blob URLs to prevent memory leaks
    this.photoService.clearImageCache();
  }

  trackByPhotoId(index: number, photo: Photo): string {
    return photo.id;
  }

  loadPhotos(): void {
    this.isLoading = true;

    this.photoService.getPhotos(this.currentPage, this.pageSize).subscribe({
      next: (response) => {
        this.isLoading = false;
        console.log('Gallery - Photos loaded:', response);

        if (response.success && response.data) {
          this.photos = response.data.items;
          this.totalPhotos = response.data.totalCount;
          this.availablePhotos = this.photos.filter(p => !p.isOrdered).length;

          // FIXED: Load authenticated image URLs for each photo
          this.loadPhotoImages();
        } else {
          console.error('Gallery - API returned success=false:', response);
          this.snackBar.open('Failed to load photos: ' + response.message, 'Close', { duration: 5000 });
        }
      },
      error: (error) => {
        this.isLoading = false;
        console.error('Gallery - Error loading photos:', error);

        let errorMessage = 'Failed to load photos';
        if (error.status === 0) {
          errorMessage += ' - Check API connection';
        } else if (error.status === 401) {
          errorMessage += ' - Please log in again';
        }

        this.snackBar.open(errorMessage, 'Close', { duration: 5000 });
      }
    });
  }

  /**
   * FIXED: Load authenticated image URLs for all photos.
   */
  private loadPhotoImages(): void {
    this.photos.forEach(photo => {
      this.photoService.getThumbnailUrl(photo.id).subscribe({
        next: (blobUrl) => {
          this.photoImageUrls.set(photo.id, blobUrl);
          console.log(`Gallery - Loaded image for ${photo.id}`);
        },
        error: (error) => {
          console.error(`Gallery - Failed to load image for ${photo.id}:`, error);
          this.imageErrors.add(photo.id);
        }
      });
    });
  }

  /**
   * FIXED: Get cached image URL for display.
   */
  getPhotoImageUrl(photoId: string): string | null {
    return this.photoImageUrls.get(photoId) || null;
  }

  /**
   * FIXED: Handle image loading errors.
   */
  onImageError(photoId: string): void {
    console.error(`Gallery - Image error for photo ${photoId}`);
    this.imageErrors.add(photoId);
  }

  onPageChange(event: PageEvent): void {
    this.currentPage = event.pageIndex + 1;
    this.pageSize = event.pageSize;

    // Clear current images
    this.photoImageUrls.clear();
    this.imageErrors.clear();

    this.loadPhotos();
  }

  openPhotoPreview(photo: Photo): void {
    const dialogRef = this.dialog.open(PhotoPreviewComponent, {
      data: photo,
      maxWidth: '90vw',
      maxHeight: '90vh'
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result?.action === 'addToCart') {
        this.selectForPrinting(result.photo);
      }
    });
  }

  selectForPrinting(photo: Photo): void {
    console.log('Gallery - Selecting photo for printing:', photo);

    const dialogRef = this.dialog.open(PrintSelectorComponent, {
      data: photo,
      width: '600px',
      maxHeight: '80vh',
      disableClose: false
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result?.action === 'added') {
        this.snackBar.open('Photo added to cart successfully!', 'Close', { duration: 3000 });
        this.loadPhotos();
      }
    });
  }

  deletePhoto(photo: Photo): void {
    if (confirm(`Are you sure you want to delete ${photo.filename}?`)) {
      this.photoService.deletePhoto(photo.id).subscribe({
        next: (response) => {
          if (response.success) {
            this.snackBar.open('Photo deleted successfully', 'Close', { duration: 3000 });
            this.loadPhotos();
          } else {
            this.snackBar.open('Failed to delete photo: ' + response.message, 'Close', { duration: 5000 });
          }
        },
        error: (error) => {
          console.error('Gallery - Delete error:', error);
          this.snackBar.open('Failed to delete photo', 'Close', { duration: 3000 });
        }
      });
    }
  }
}
