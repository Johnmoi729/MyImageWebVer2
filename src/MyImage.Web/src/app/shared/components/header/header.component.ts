import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { AuthService } from '../../../core/services/auth.service';
import { CartService } from '../../../core/services/cart.service';

@Component({
  selector: 'app-header',
  standalone: false,
  template: `
    <mat-toolbar color="primary" class="header-toolbar">
      <div class="toolbar-content">
        <!-- Logo -->
        <button mat-button class="logo-button" [routerLink]="isAuthenticated ? '/photos' : '/'">
          <mat-icon>photo_camera</mat-icon>
          <span class="logo-text">MyImage</span>
        </button>

        <!-- Spacer -->
        <span class="toolbar-spacer"></span>

        <!-- Authenticated User Menu -->
        <div *ngIf="currentUser$ | async as user" class="user-menu">
          <!-- Cart Icon -->
          <button mat-icon-button routerLink="/cart" class="cart-button">
            <mat-icon [matBadge]="cartItemCount$ | async" matBadgeColor="accent">shopping_cart</mat-icon>
          </button>

          <!-- Admin Menu -->
          <button *ngIf="isAdmin" mat-button routerLink="/admin">
            <mat-icon>admin_panel_settings</mat-icon>
            Admin
          </button>

          <!-- User Menu -->
          <button mat-button [matMenuTriggerFor]="userMenu" class="user-menu-button">
            <mat-icon>account_circle</mat-icon>
            {{ user.firstName }}
          </button>
          <mat-menu #userMenu="matMenu">
            <button mat-menu-item routerLink="/orders">
              <mat-icon>receipt</mat-icon>
              My Orders
            </button>
            <button mat-menu-item (click)="logout()">
              <mat-icon>logout</mat-icon>
              Logout
            </button>
          </mat-menu>
        </div>

        <!-- Guest Menu -->
        <div *ngIf="!(currentUser$ | async)" class="guest-menu">
          <button mat-button routerLink="/login">Login</button>
          <button mat-raised-button color="accent" routerLink="/register">Register</button>
        </div>
      </div>
    </mat-toolbar>
  `,
  styles: [`
    .header-toolbar {
      position: sticky;
      top: 0;
      z-index: 1000;
    }

    .toolbar-content {
      display: flex;
      align-items: center;
      width: 100%;
      max-width: 1200px;
      margin: 0 auto;
    }

    .logo-button {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .logo-text {
      font-size: 1.2em;
      font-weight: 500;
    }

    .toolbar-spacer {
      flex: 1;
    }

    .user-menu, .guest-menu {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .cart-button {
      margin-right: 8px;
    }

    .user-menu-button {
      display: flex;
      align-items: center;
      gap: 4px;
    }

    @media (max-width: 768px) {
      .logo-text {
        display: none;
      }

      .user-menu-button span:not(.mat-icon) {
        display: none;
      }
    }
  `]
})
export class HeaderComponent {
  currentUser$ = this.authService.currentUser$;
  cartItemCount$: Observable<number>;

  constructor(
    private authService: AuthService,
    private cartService: CartService,
    private router: Router
  ) {
    // Initialize cartItemCount$ with proper null checking
    this.cartItemCount$ = this.cartService.cart$.pipe(
      map(cart => cart?.summary?.totalPhotos ?? 0)
    );
  }

  get isAuthenticated(): boolean {
    return this.authService.isAuthenticated();
  }

  get isAdmin(): boolean {
    return this.authService.isAdmin();
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
