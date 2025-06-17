import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { PageEvent } from '@angular/material/paginator';
import { MatSnackBar } from '@angular/material/snack-bar';
import { PhotoService } from '../../../core/services/photo.service';
import { Photo } from '../../../shared/models/photo.models';
import { PhotoPreviewComponent } from '../photo-preview/photo-preview.component';
import { PrintSelectorComponent } from '../print-selector/print-selector.component';

@Component({
  selector: 'app-photo-gallery',
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
        <mat-card class="photo-card" *ngFor="let photo of photos">
          <div class="photo-container" (click)="openPhotoPreview(photo)">
            <img [src]="photo.thumbnailUrl"
                 [alt]="photo.filename"
                 class="photo-thumbnail"
                 loading="lazy">
            <div class="photo-overlay">
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

    .empty-state {
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
export class PhotoGalleryComponent implements OnInit {
  photos: Photo[] = [];
  totalPhotos = 0;
  availablePhotos = 0;
  pageSize = 12;
  currentPage = 1;
  isLoading = false;

  constructor(
    private photoService: PhotoService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadPhotos();
  }

  loadPhotos(): void {
    this.isLoading = true;

    this.photoService.getPhotos(this.currentPage, this.pageSize).subscribe({
      next: (response) => {
        this.isLoading = false;
        if (response.success) {
          this.photos = response.data.items;
          this.totalPhotos = response.data.totalCount;
          this.availablePhotos = this.photos.filter(p => !p.isOrdered).length;
        }
      },
      error: (error) => {
        this.isLoading = false;
        this.snackBar.open('Failed to load photos', 'Close', { duration: 3000 });
      }
    });
  }

  onPageChange(event: PageEvent): void {
    this.currentPage = event.pageIndex + 1;
    this.pageSize = event.pageSize;
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
    const dialogRef = this.dialog.open(PrintSelectorComponent, {
      data: photo,
      width: '600px',
      maxHeight: '80vh'
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result?.action === 'added') {
        this.snackBar.open('Photo added to cart successfully!', 'Close', { duration: 3000 });
        // Optionally reload photos to update isOrdered status
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
          }
        },
        error: (error) => {
          this.snackBar.open('Failed to delete photo', 'Close', { duration: 3000 });
        }
      });
    }
  }
}
