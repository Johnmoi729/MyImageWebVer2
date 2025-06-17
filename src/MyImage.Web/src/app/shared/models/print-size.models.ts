export interface PrintSize {
  id: string;
  sizeCode: string;
  displayName: string;
  width: number;
  height: number;
  unit: string;
  price: number;
  currency: string;
  isActive: boolean;
  minWidth: number;
  minHeight: number;
  recommendedWidth: number;
  recommendedHeight: number;
}
