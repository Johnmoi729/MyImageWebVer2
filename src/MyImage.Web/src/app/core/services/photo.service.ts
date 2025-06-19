// src/MyImage.Web/src/app/core/services/photo.service.ts
import { HttpClient, HttpEventType } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, catchError, map, of, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../../shared/models/api.models';
import { Photo, PhotoUploadResult } from '../../shared/models/photo.models';

@Injectable({
  providedIn: 'root'
})
export class PhotoService {
  // FIXED: Better cache management with validation and subjects for reactivity
  private imageCache = new Map<string, { url: string; timestamp: number; isValid: boolean }>();
  private readonly CACHE_DURATION = 10 * 60 * 1000; // 10 minutes

  // Subject to notify components when cache is invalidated
  private cacheInvalidated = new BehaviorSubject<string | null>(null);
  public cacheInvalidated$ = this.cacheInvalidated.asObservable();

  constructor(private http: HttpClient) {
    // Periodic cleanup of expired cache entries
    setInterval(() => this.cleanupExpiredCache(), 60000); // Every minute
  }

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
   * FIXED: Get authenticated image URL with proper validation and error recovery.
   */
  getAuthenticatedImageUrl(photoId: string, type: 'thumbnail' | 'download' = 'thumbnail'): Observable<string> {
    const cacheKey = `${photoId}-${type}`;
    const now = Date.now();

    // Check cache and validate
    const cached = this.imageCache.get(cacheKey);
    if (cached && cached.isValid && (now - cached.timestamp) < this.CACHE_DURATION) {
      console.log(`PhotoService - Using valid cached ${type} for photo ${photoId}`);
      return of(cached.url);
    }

    // Clear expired or invalid cache entry
    if (cached) {
      this.invalidateImageUrl(cacheKey);
    }

    // Fetch fresh image
    return this.fetchImageAsBlob(photoId, type, cacheKey, now);
  }

  /**
   * FIXED: Fetch image as blob with better error handling
   */
  private fetchImageAsBlob(photoId: string, type: 'thumbnail' | 'download', cacheKey: string, timestamp: number): Observable<string> {
    const endpoint = type === 'thumbnail'
      ? `${environment.apiUrl}/photos/${photoId}/thumbnail`
      : `${environment.apiUrl}/photos/${photoId}/download`;

    console.log(`PhotoService - Fetching fresh ${type} for photo ${photoId}`);

    return this.http.get(endpoint, {
      responseType: 'blob',
      headers: {
        'Accept': 'image/jpeg,image/*,*/*',
        'Cache-Control': 'no-cache'
      }
    }).pipe(
      map(blob => {
        console.log(`PhotoService - Received blob for ${photoId} ${type}:`, blob.size, 'bytes');

        // Validate blob
        if (blob.size === 0) {
          throw new Error('Received empty blob');
        }

        if (!blob.type.startsWith('image/')) {
          console.warn(`PhotoService - Unexpected blob type: ${blob.type}`);
        }

        // Create object URL
        const objectUrl = URL.createObjectURL(blob);

        // Cache with validation flag
        this.imageCache.set(cacheKey, {
          url: objectUrl,
          timestamp: timestamp,
          isValid: true
        });

        console.log(`PhotoService - Cached new blob URL for ${photoId} ${type}:`, objectUrl);
        return objectUrl;
      }),
      catchError(error => {
        console.error(`PhotoService - Error fetching ${type} for photo ${photoId}:`, error);

        // Mark any existing cache as invalid
        const existing = this.imageCache.get(cacheKey);
        if (existing) {
          existing.isValid = false;
        }

        return throwError(() => error);
      })
    );
  }

  /**
   * FIXED: Invalidate specific image URL and notify components
   */
  private invalidateImageUrl(cacheKey: string): void {
    const cached = this.imageCache.get(cacheKey);
    if (cached) {
      URL.revokeObjectURL(cached.url);
      this.imageCache.delete(cacheKey);
      console.log(`PhotoService - Invalidated cache: ${cacheKey}`);

      // Notify components that cache was invalidated
      this.cacheInvalidated.next(cacheKey);
    }
  }

  /**
   * Get thumbnail URL with authentication.
   */
  getThumbnailUrl(photoId: string): Observable<string> {
    return this.getAuthenticatedImageUrl(photoId, 'thumbnail');
  }

  /**
   * Get download URL with authentication.
   */
  getDownloadUrl(photoId: string): Observable<string> {
    return this.getAuthenticatedImageUrl(photoId, 'download');
  }

  /**
   * Delete photo.
   */
  deletePhoto(photoId: string): Observable<ApiResponse<void>> {
    // Clear cache before deleting
    this.clearImageCache(photoId);

    return this.http.delete<ApiResponse<void>>(`${environment.apiUrl}/photos/${photoId}`);
  }

  /**
   * FIXED: Clear cached image URLs with proper validation.
   */
  clearImageCache(photoId?: string): void {
    if (photoId) {
      // Clear specific photo cache
      const thumbnailKey = `${photoId}-thumbnail`;
      const downloadKey = `${photoId}-download`;

      this.invalidateImageUrl(thumbnailKey);
      this.invalidateImageUrl(downloadKey);

      console.log(`PhotoService - Cleared all cache for photo ${photoId}`);
    } else {
      // Clear all cache
      const allKeys = Array.from(this.imageCache.keys());
      allKeys.forEach(key => this.invalidateImageUrl(key));
      console.log('PhotoService - Cleared all image cache');
    }
  }

  /**
   * FIXED: Check if cached URL is still valid
   */
  isCacheValid(photoId: string, type: 'thumbnail' | 'download' = 'thumbnail'): boolean {
    const cacheKey = `${photoId}-${type}`;
    const cached = this.imageCache.get(cacheKey);

    if (!cached) return false;

    const now = Date.now();
    const isExpired = (now - cached.timestamp) > this.CACHE_DURATION;

    return cached.isValid && !isExpired;
  }

  /**
   * Periodic cleanup of expired cache entries
   */
  private cleanupExpiredCache(): void {
    const now = Date.now();
    const expiredKeys: string[] = [];

    this.imageCache.forEach((cached, key) => {
      if (!cached.isValid || (now - cached.timestamp) > this.CACHE_DURATION) {
        URL.revokeObjectURL(cached.url);
        expiredKeys.push(key);
      }
    });

    expiredKeys.forEach(key => this.imageCache.delete(key));

    if (expiredKeys.length > 0) {
      console.log(`PhotoService - Cleaned up ${expiredKeys.length} expired cache entries`);
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
