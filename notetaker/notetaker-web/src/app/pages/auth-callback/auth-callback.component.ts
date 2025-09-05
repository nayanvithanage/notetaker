import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-auth-callback',
  templateUrl: './auth-callback.component.html',
  styleUrls: ['./auth-callback.component.scss'],
  imports: [
    CommonModule,
    MatProgressSpinnerModule,
    MatIconModule,
    MatButtonModule
  ],
  standalone: true
})
export class AuthCallbackComponent implements OnInit {
  isLoading = true;
  error: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit() {
    this.handleAuthCallback();
  }

  private async handleAuthCallback() {
    try {
      const token = this.route.snapshot.queryParams['token'];
      const refresh = this.route.snapshot.queryParams['refresh'];
      const error = this.route.snapshot.queryParams['error'];

      if (error) {
        this.error = error;
        this.isLoading = false;
        return;
      }

      if (token && refresh) {
        // Store tokens and redirect to main app
        await this.authService.setTokens(token, refresh);
        this.router.navigate(['/meetings']);
      } else {
        this.error = 'No authentication tokens received';
      }
    } catch (error) {
      console.error('Auth callback error:', error);
      this.error = 'Authentication failed';
    } finally {
      this.isLoading = false;
    }
  }

  retryLogin() {
    this.router.navigate(['/login']);
  }
}
