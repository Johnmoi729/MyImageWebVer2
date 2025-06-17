import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../shared/models/api.models';
import { Cart, PrintSelection } from '../../shared/models/cart.models';

@Injectable({
  providedIn: 'root'
})
export class CartService {
  private cartSubject = new BehaviorSubject<Cart | null>(null);
  public cart$ = this.cartSubject.asObservable();

  constructor(private http: HttpClient) {
    this.loadCart();
  }

  loadCart(): Observable<ApiResponse<Cart>> {
    return this.http.get<ApiResponse<Cart>>(`${environment.apiUrl}/cart`)
      .pipe(
        tap(response => {
          if (response.success) {
            this.cartSubject.next(response.data);
          }
        })
      );
  }

  addToCart(photoId: string, printSelections: PrintSelection[]): Observable<ApiResponse<Cart>> {
    return this.http.post<ApiResponse<Cart>>(`${environment.apiUrl}/cart/items`, {
      photoId,
      printSelections
    }).pipe(
      tap(response => {
        if (response.success) {
          this.cartSubject.next(response.data);
        }
      })
    );
  }

  updateCartItem(itemId: string, printSelections: PrintSelection[]): Observable<ApiResponse<Cart>> {
    return this.http.put<ApiResponse<Cart>>(`${environment.apiUrl}/cart/items/${itemId}`, {
      printSelections
    }).pipe(
      tap(response => {
        if (response.success) {
          this.cartSubject.next(response.data);
        }
      })
    );
  }

  removeFromCart(itemId: string): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`${environment.apiUrl}/cart/items/${itemId}`)
      .pipe(
        tap(() => {
          this.loadCart().subscribe();
        })
      );
  }

  clearCart(): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`${environment.apiUrl}/cart`)
      .pipe(
        tap(() => {
          this.cartSubject.next(null);
        })
      );
  }

  calculateTotal(subtotal: number, state: string, postalCode: string): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${environment.apiUrl}/cart/calculate-total`, {
      subtotal,
      state,
      postalCode
    });
  }

  getCurrentCart(): Cart | null {
    return this.cartSubject.value;
  }
}
