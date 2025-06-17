import { HttpClient, HttpEventType } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../../shared/models/api.models';
import { Photo, PhotoUploadResult } from '../../shared/models/photo.models';

@Injectable({
  providedIn: 'root'
})
export class PhotoService {
  constructor(private http: HttpClient) {}

  // No folder scan endpoint - handled client-side
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
          return {
            progress: 100,
            result: event.body?.data
          };
        }
        return { progress: 0 };
      })
    );
  }

  getPhotos(page: number = 1, pageSize: number = 20): Observable<ApiResponse<PagedResult<Photo>>> {
    return this.http.get<ApiResponse<PagedResult<Photo>>>(
      `${environment.apiUrl}/photos?page=${page}&pageSize=${pageSize}`
    );
  }

  deletePhoto(photoId: string): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`${environment.apiUrl}/photos/${photoId}`);
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
