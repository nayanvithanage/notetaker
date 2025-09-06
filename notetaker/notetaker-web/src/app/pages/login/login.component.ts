import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
  imports: [
    CommonModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule
  ],
  standalone: true
})
export class LoginComponent {
  isLoading = false;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  async loginWithGoogle() {
    this.isLoading = true;
    try {
      // Generate a random state parameter for security
      const state = Math.random().toString(36).substring(2, 15) + Math.random().toString(36).substring(2, 15);
      
      // Store state in localStorage for validation
      localStorage.setItem('oauth_state', state);
      
      // Redirect directly to Google OAuth
      const googleAuthUrl = 'https://accounts.google.com/o/oauth2/v2/auth?' +
        'client_id=1010111570699-oi8seird36hgpr9je5986r5u9p8gc8c3.apps.googleusercontent.com&' +
        'redirect_uri=http://localhost:5135/api/auth/google/callback&' +
        'response_type=code&' +
        'scope=openid email profile https://www.googleapis.com/auth/calendar.readonly&' +
        'access_type=offline&' +
        'prompt=consent&' +
        'state=' + encodeURIComponent(state);
      
      window.location.href = googleAuthUrl;
    } catch (error) {
      console.error('Login error:', error);
      this.isLoading = false;
    }
  }
}
