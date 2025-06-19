// src/MyImage.Web/src/app/core/services/photo.service.ts
import { HttpClient, HttpEventType } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../../shared/models/api.models';
import { Photo, PhotoUploadResult } from '../../shared/models/photo.models';

@Injectable({
  providedIn: 'root'
})
export class PhotoService {
  // Cache for blob URLs to avoid refetching images
  private imageCache = new Map<string, string>();

  constructor(private http: HttpClient) {}

  /**
   * Upload photos with proper progress tracking and API URL handling.
   */
  uploadPhotos(files: File[], folderPath?: string): Observable<{progress: number, result?: PhotoUploadResult}> {
    const formData = new FormData();

    // Add files to form data
    Array.from(files).forEach(file => {
      formData.append('files', file);
    });

    if (folderPath) {
      formData.append('folderPath', folderPath);
    }

    return this.http.post<ApiResponse<PhotoUploadResult>>(
      `${environment.apiUrl}/photos/bulk-upload`,
      formData,
      {
        reportProgress: true,
        observe: 'events'
      }
    ).pipe(
      map(event => {
        if (event.type === HttpEventType.UploadProgress) {
          const progress = Math.round(100 * event.loaded / (event.total || 1));
          return { progress };
        } else if (event.type === HttpEventType.Response) {
          const result = event.body?.data;
          return {
            progress: 100,
            result: result
          };
        }
        return { progress: 0 };
      })
    );
  }

  /**
   * Get photos with pagination.
   */
  getPhotos(page: number = 1, pageSize: number = 20): Observable<ApiResponse<PagedResult<Photo>>> {
    return this.http.get<ApiResponse<PagedResult<Photo>>>(
      `${environment.apiUrl}/photos?page=${page}&pageSize=${pageSize}`
    );
  }

  /**
   * FIXED: Get authenticated image URL as blob.
   * This fetches the image through Angular HTTP client with auth headers,
   * then creates a blob URL that can be used in img src attributes.
   */
  getAuthenticatedImageUrl(photoId: string, type: 'thumbnail' | 'download' = 'thumbnail'): Observable<string> {
    const cacheKey = `${photoId}-${type}`;

    // Return cached URL if available
    if (this.imageCache.has(cacheKey)) {
      return of(this.imageCache.get(cacheKey)!);
    }

    // Fetch image as blob with authentication
    const endpoint = type === 'thumbnail'
      ? `${environment.apiUrl}/photos/${photoId}/thumbnail`
      : `${environment.apiUrl}/photos/${photoId}/download`;

    console.log(`PhotoService - Fetching authenticated ${type} for photo ${photoId}`);

    return this.http.get(endpoint, {
      responseType: 'blob',
      // This ensures auth headers are included
    }).pipe(
      map(blob => {
        // Create object URL from blob
        const objectUrl = URL.createObjectURL(blob);

        // Cache the URL
        this.imageCache.set(cacheKey, objectUrl);

        console.log(`PhotoService - Created blob URL for ${photoId} ${type}:`, objectUrl);
        return objectUrl;
      })
    );
  }

  /**
   * FIXED: Get thumbnail URL with authentication.
   * Returns an Observable that resolves to a blob URL.
   */
  getThumbnailUrl(photoId: string): Observable<string> {
    return this.getAuthenticatedImageUrl(photoId, 'thumbnail');
  }

  /**
   * FIXED: Get download URL with authentication.
   * Returns an Observable that resolves to a blob URL.
   */
  getDownloadUrl(photoId: string): Observable<string> {
    return this.getAuthenticatedImageUrl(photoId, 'download');
  }

  /**
   * Delete photo.
   */
  deletePhoto(photoId: string): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`${environment.apiUrl}/photos/${photoId}`);
  }

  /**
   * Clear cached image URL when no longer needed.
   * Important: Call this to prevent memory leaks from blob URLs.
   */
  clearImageCache(photoId?: string): void {
    if (photoId) {
      // Clear specific photo cache
      const thumbnailKey = `${photoId}-thumbnail`;
      const downloadKey = `${photoId}-download`;

      if (this.imageCache.has(thumbnailKey)) {
        URL.revokeObjectURL(this.imageCache.get(thumbnailKey)!);
        this.imageCache.delete(thumbnailKey);
      }

      if (this.imageCache.has(downloadKey)) {
        URL.revokeObjectURL(this.imageCache.get(downloadKey)!);
        this.imageCache.delete(downloadKey);
      }
    } else {
      // Clear all cache
      this.imageCache.forEach(url => URL.revokeObjectURL(url));
      this.imageCache.clear();
    }
  }

  // Helper method to filter JPEG files client-side
  filterJpegFiles(files: FileList): File[] {
    return Array.from(files).filter(file => {
      const extension = file.name.toLowerCase();
      return environment.supportedFormats.some(format =>
        extension.endsWith(format.toLowerCase())
      );
    }).filter(file => file.size <= environment.maxFileSize);
  }

  // Validate file before upload
  validateFile(file: File): { valid: boolean; error?: string } {
    if (file.size > environment.maxFileSize) {
      return { valid: false, error: 'File size exceeds 50MB limit' };
    }

    const extension = file.name.toLowerCase();
    const isValidFormat = environment.supportedFormats.some(format =>
      extension.endsWith(format.toLowerCase())
    );

    if (!isValidFormat) {
      return { valid: false, error: 'Only JPEG files are supported' };
    }

    return { valid: true };
  }
}
