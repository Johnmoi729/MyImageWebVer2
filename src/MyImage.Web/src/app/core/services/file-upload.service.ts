import { Injectable } from '@angular/core';

export interface FileValidationResult {
  valid: boolean;
  error?: string;
}

export interface UploadProgress {
  file: File;
  progress: number;
  completed: boolean;
  error?: string;
}

@Injectable({
  providedIn: 'root'
})
export class FileUploadService {
  private maxFileSize = 52428800; // 50MB
  private allowedTypes = ['image/jpeg', 'image/jpg'];

  validateFile(file: File): FileValidationResult {
    // Check file size
    if (file.size > this.maxFileSize) {
      return {
        valid: false,
        error: `File "${file.name}" is too large. Maximum size is 50MB.`
      };
    }

    // Check file type
    if (!this.allowedTypes.includes(file.type.toLowerCase())) {
      return {
        valid: false,
        error: `File "${file.name}" is not a supported format. Only JPEG files are allowed.`
      };
    }

    return { valid: true };
  }

  validateFiles(files: File[]): { validFiles: File[]; errors: string[] } {
    const validFiles: File[] = [];
    const errors: string[] = [];

    files.forEach(file => {
      const validation = this.validateFile(file);
      if (validation.valid) {
        validFiles.push(file);
      } else if (validation.error) {
        errors.push(validation.error);
      }
    });

    return { validFiles, errors };
  }

  // Generate preview URL for file
  generatePreview(file: File): string {
    return URL.createObjectURL(file);
  }

  // Clean up preview URL
  revokePreview(url: string): void {
    URL.revokeObjectURL(url);
  }

  // Format file size for display
  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';

    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));

    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  }

  // Check if file is an image
  isImageFile(file: File): boolean {
    return file.type.startsWith('image/');
  }

  // Create thumbnail from file
  createThumbnail(file: File, maxWidth: number = 150, maxHeight: number = 150): Promise<string> {
    return new Promise((resolve, reject) => {
      const canvas = document.createElement('canvas');
      const ctx = canvas.getContext('2d');
      const img = new Image();

      img.onload = () => {
        // Calculate new dimensions
        let { width, height } = img;

        if (width > height) {
          if (width > maxWidth) {
            height = (height * maxWidth) / width;
            width = maxWidth;
          }
        } else {
          if (height > maxHeight) {
            width = (width * maxHeight) / height;
            height = maxHeight;
          }
        }

        canvas.width = width;
        canvas.height = height;

        // Draw and convert to data URL
        ctx?.drawImage(img, 0, 0, width, height);
        resolve(canvas.toDataURL('image/jpeg', 0.8));
      };

      img.onerror = () => reject(new Error('Failed to create thumbnail'));
      img.src = URL.createObjectURL(file);
    });
  }
}
