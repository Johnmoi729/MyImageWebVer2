import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../../shared/models/api.models';
import { Order, ShippingAddress } from '../../shared/models/order.models';

@Injectable({
  providedIn: 'root'
})
export class OrderService {
  constructor(private http: HttpClient) {}

  createOrder(shippingAddress: ShippingAddress, paymentMethod: string, creditCard?: any): Observable<ApiResponse<any>> {
    const orderData: any = {
      shippingAddress,
      paymentMethod
    };

    if (creditCard) {
      orderData.creditCard = creditCard;
    }

    return this.http.post<ApiResponse<any>>(`${environment.apiUrl}/orders`, orderData);
  }

  getOrders(page: number = 1, pageSize: number = 20): Observable<ApiResponse<PagedResult<Order>>> {
    return this.http.get<ApiResponse<PagedResult<Order>>>(
      `${environment.apiUrl}/orders?page=${page}&pageSize=${pageSize}`
    );
  }

  getOrderDetails(orderId: string): Observable<ApiResponse<any>> {
    return this.http.get<ApiResponse<any>>(`${environment.apiUrl}/orders/${orderId}`);
  }
}
