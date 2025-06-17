import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../shared/models/api.models';
import { PrintSize } from '../../shared/models/print-size.models';

@Injectable({
  providedIn: 'root'
})
export class PrintSizeService {
  constructor(private http: HttpClient) {}

  getPrintSizes(): Observable<ApiResponse<PrintSize[]>> {
    return this.http.get<ApiResponse<PrintSize[]>>(`${environment.apiUrl}/print-sizes`);
  }

  getPrintSizeRecommendations(photoWidth: number, photoHeight: number): Observable<ApiResponse<any[]>> {
    return this.http.get<ApiResponse<any[]>>(
      `${environment.apiUrl}/print-sizes/recommendations?photoWidth=${photoWidth}&photoHeight=${photoHeight}`
    );
  }
}
