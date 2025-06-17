import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../../shared/models/api.models';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  constructor(private http: HttpClient) {}

  getDashboardStats(): Observable<ApiResponse<any>> {
    return this.http.get<ApiResponse<any>>(`${environment.apiUrl}/admin/dashboard`);
  }

  getOrders(status?: string, page: number = 1, pageSize: number = 20): Observable<ApiResponse<PagedResult<any>>> {
    let url = `${environment.apiUrl}/admin/orders?page=${page}&pageSize=${pageSize}`;
    if (status) {
      url += `&status=${status}`;
    }
    return this.http.get<ApiResponse<PagedResult<any>>>(url);
  }

  updateOrderStatus(orderId: string, status: string, notes?: string): Observable<ApiResponse<any>> {
    return this.http.put<ApiResponse<any>>(`${environment.apiUrl}/admin/orders/${orderId}/status`, {
      status,
      notes
    });
  }

  completeOrder(orderId: string, data: any): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${environment.apiUrl}/admin/orders/${orderId}/complete`, data);
  }

  getAllPrintSizes(): Observable<ApiResponse<any[]>> {
    return this.http.get<ApiResponse<any[]>>(`${environment.apiUrl}/admin/print-sizes`);
  }

  updatePrintSize(sizeId: string, data: any): Observable<ApiResponse<any>> {
    return this.http.put<ApiResponse<any>>(`${environment.apiUrl}/admin/print-sizes/${sizeId}`, data);
  }

  addPrintSize(data: any): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${environment.apiUrl}/admin/print-sizes`, data);
  }
}
