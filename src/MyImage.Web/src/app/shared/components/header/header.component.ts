import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { map, takeUntil } from 'rxjs/operators';
import { AuthService } from '../../../core/services/auth.service';
import { CartService } from '../../../core/services/cart.service';

/**
 * Fixed HeaderComponent with proper cart count handling and cleanup.
 * Resolves cart count display issues and memory leaks.
 *
 * Key improvements:
 * - Proper subscription management to prevent memory leaks
 * - Null-safe cart count handling
 * - Responsive design improvements
 * - Better user experience with loading states
 */
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
          <!-- Cart Icon with Count -->
          <button mat-icon-button routerLink="/cart" class="cart-button" matTooltip="Shopping Cart">
            <mat-icon
              [matBadge]="cartItemCount"
              [matBadgeHidden]="cartItemCount === 0"
              matBadgeColor="accent"
              matBadgeSize="small">
              shopping_cart
            </mat-icon>
          </button>

          <!-- Admin Menu -->
          <button *ngIf="isAdmin"
                  mat-button
                  routerLink="/admin"
                  class="admin-button"
                  matTooltip="Admin Panel">
            <mat-icon>admin_panel_settings</mat-icon>
            <span class="button-text">Admin</span>
          </button>

          <!-- Navigation Menu for authenticated users -->
          <button mat-button routerLink="/photos" class="nav-button">
            <mat-icon>photo_library</mat-icon>
            <span class="button-text">Photos</span>
          </button>

          <!-- User Menu -->
          <button mat-button [matMenuTriggerFor]="userMenu" class="user-menu-button">
            <mat-icon>account_circle</mat-icon>
            <span class="user-name">{{ user.firstName }}</span>
            <mat-icon class="dropdown-icon">arrow_drop_down</mat-icon>
          </button>

          <mat-menu #userMenu="matMenu" class="user-menu-panel">
            <div class="menu-header">
              <div class="user-info">
                <div class="user-name-full">{{ user.firstName }} {{ user.lastName }}</div>
                <div class="user-id">{{ user.userId }}</div>
              </div>
            </div>
            <mat-divider></mat-divider>
            <button mat-menu-item routerLink="/orders">
              <mat-icon>receipt</mat-icon>
              <span>My Orders</span>
            </button>
            <button mat-menu-item routerLink="/photos">
              <mat-icon>photo_library</mat-icon>
              <span>My Photos</span>
            </button>
            <button mat-menu-item routerLink="/cart">
              <mat-icon>shopping_cart</mat-icon>
              <span>Shopping Cart</span>
            </button>
            <mat-divider></mat-divider>
            <button mat-menu-item (click)="logout()" class="logout-button">
              <mat-icon>logout</mat-icon>
              <span>Logout</span>
            </button>
          </mat-menu>
        </div>

        <!-- Guest Menu -->
        <div *ngIf="!(currentUser$ | async)" class="guest-menu">
          <button mat-button routerLink="/login" class="login-button">
            <mat-icon>login</mat-icon>
            <span class="button-text">Login</span>
          </button>
          <button mat-raised-button color="accent" routerLink="/register" class="register-button">
            <mat-icon>person_add</mat-icon>
            <span class="button-text">Register</span>
          </button>
        </div>
      </div>
    </mat-toolbar>
  `,
  styles: [`
    .header-toolbar {
      position: sticky;
      top: 0;
      z-index: 1000;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }

    .toolbar-content {
      display: flex;
      align-items: center;
      width: 100%;
      max-width: 1200px;
      margin: 0 auto;
      padding: 0 16px;
    }

    .logo-button {
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: 1.1em;
      font-weight: 500;
      padding: 8px 16px;
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
      position: relative;
    }

    .cart-button .mat-icon {
      font-size: 24px;
    }

    .admin-button {
      display: flex;
      align-items: center;
      gap: 4px;
      background: rgba(255, 255, 255, 0.1);
      border-radius: 4px;
    }

    .nav-button {
      display: flex;
      align-items: center;
      gap: 4px;
    }

    .user-menu-button {
      display: flex;
      align-items: center;
      gap: 4px;
      padding: 8px 12px;
      border-radius: 4px;
      transition: background-color 0.2s;
    }

    .user-menu-button:hover {
      background: rgba(255, 255, 255, 0.1);
    }

    .user-name {
      font-weight: 500;
    }

    .dropdown-icon {
      font-size: 18px;
      width: 18px;
      height: 18px;
    }

    .menu-header {
      padding: 16px;
      background: #f5f5f5;
    }

    .user-info {
      text-align: center;
    }

    .user-name-full {
      font-weight: 500;
      font-size: 1.1em;
      color: #333;
    }

    .user-id {
      font-size: 0.9em;
      color: #666;
      margin-top: 4px;
    }

    .logout-button {
      color: #f44336;
    }

    .guest-menu .login-button {
      display: flex;
      align-items: center;
      gap: 4px;
    }

    .register-button {
      display: flex;
      align-items: center;
      gap: 4px;
    }

    /* Responsive Design */
    @media (max-width: 768px) {
      .toolbar-content {
        padding: 0 8px;
      }

      .logo-text {
        display: none;
      }

      .button-text {
        display: none;
      }

      .user-name {
        display: none;
      }

      .dropdown-icon {
        display: none;
      }

      .nav-button {
        display: none; /* Hide photos button on mobile since it's in the menu */
      }
    }

    @media (max-width: 480px) {
      .admin-button .button-text {
        display: none;
      }
    }

    /* Loading States */
    .cart-button[disabled] {
      opacity: 0.6;
    }

    /* Badge Styling */
    ::ng-deep .mat-badge-content {
      font-size: 11px;
      font-weight: 600;
    }
  `]
})
export class HeaderComponent implements OnInit, OnDestroy {
  currentUser$ = this.authService.currentUser$;
  cartItemCount = 0;

  // Subject for component cleanup
  private destroy$ = new Subject<void>();

  constructor(
    private authService: AuthService,
    private cartService: CartService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Subscribe to cart changes and update count
    this.cartService.cart$.pipe(
      takeUntil(this.destroy$),
      map(cart => cart?.summary?.totalPhotos ?? 0)
    ).subscribe(count => {
      this.cartItemCount = count;
    });

    // Load cart if user is authenticated
    if (this.isAuthenticated) {
      this.cartService.loadCart().subscribe({
        next: () => {
          // Cart loaded successfully
        },
        error: (error) => {
          console.error('Failed to load cart in header:', error);
          // Don't show error to user in header - handled by interceptor
        }
      });
    }
  }

  ngOnDestroy(): void {
    // Clean up subscriptions to prevent memory leaks
    this.destroy$.next();
    this.destroy$.complete();
  }

  get isAuthenticated(): boolean {
    return this.authService.isAuthenticated();
  }

  get isAdmin(): boolean {
    return this.authService.isAdmin();
  }

  logout(): void {
    // Clear cart when logging out
    this.cartService.clearCart().subscribe({
      complete: () => {
        this.authService.logout();
        this.router.navigate(['/']);
      }
    });
  }
}
