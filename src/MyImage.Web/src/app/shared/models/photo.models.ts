export interface Photo {
  id: string;
  filename: string;
  fileSize: number;
  uploadedAt: string;
  thumbnailUrl: string;
  downloadUrl: string;
  dimensions: {
    width: number;
    height: number;
    aspectRatio: string;
  };
  isOrdered: boolean;
  sourceFolder: string;
}

export interface PhotoUploadResult {
  uploadedPhotos: Photo[];
  failedUploads: any[];
  totalUploaded: number;
  sourceFolder: string;
}
