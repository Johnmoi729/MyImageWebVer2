export interface Order {
  orderId: string;
  orderNumber: string;
  orderDate: string;
  status: string;
  totalAmount: number;
  photoCount: number;
  printCount: number;
  paymentMethod: string;
}

export interface ShippingAddress {
  fullName: string;
  streetLine1: string;
  streetLine2?: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  phone: string;
}
