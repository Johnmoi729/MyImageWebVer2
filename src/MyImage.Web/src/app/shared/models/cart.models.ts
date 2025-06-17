export interface PrintSelection {
  sizeCode: string;
  sizeName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface CartItem {
  id: string;
  photoId: string;
  photoFilename: string;
  photoThumbnailUrl: string;
  photoDimensions: {
    width: number;
    height: number;
    aspectRatio: string;
  };
  printSelections: PrintSelection[];
  photoTotal: number;
  addedAt: string;
}

export interface Cart {
  items: CartItem[];
  summary: {
    totalPhotos: number;
    totalPrints: number;
    subtotal: number;
    tax: number;
    total: number;
  };
}
