import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { ApiService } from './api.service';
import { User, AuthResult, SocialAccount } from '../models/auth.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private currentUserSubject = new BehaviorSubject<User | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private apiService: ApiService) {
    this.loadUserFromStorage();
  }

  getCurrentUser(): User | null {
    return this.currentUserSubject.value;
  }

  isAuthenticated(): boolean {
    return !!this.getCurrentUser();
  }

  async loginWithGoogle(): Promise<string> {
    const response = await this.apiService.post<{ authUrl: string; state: string }>('auth/google/start', {}).toPromise();
    return response?.data?.authUrl || '';
  }

  async handleGoogleCallback(code: string, state: string): Promise<boolean> {
    try {
      const response = await this.apiService.post<AuthResult>('auth/google/callback', { code, state }).toPromise();
      if (response?.success && response.data) {
        this.setAuthData(response.data);
        return true;
      }
      return false;
    } catch (error) {
      console.error('Google callback error:', error);
      return false;
    }
  }

  async refreshToken(): Promise<boolean> {
    try {
      const refreshToken = localStorage.getItem('refreshToken');
      if (!refreshToken) return false;

      const response = await this.apiService.post<AuthResult>('auth/refresh', { refreshToken }).toPromise();
      if (response?.success && response.data) {
        this.setAuthData(response.data);
        return true;
      }
      return false;
    } catch (error) {
      console.error('Token refresh error:', error);
      return false;
    }
  }

  async logout(): Promise<void> {
    try {
      await this.apiService.post('auth/logout', {}).toPromise();
    } catch (error) {
      console.error('Logout error:', error);
    } finally {
      this.clearAuthData();
    }
  }

  async startSocialAuth(platform: string): Promise<string> {
    const response = await this.apiService.post<{ authUrl: string; state: string }>(`auth/social/connect?platform=${platform}`, {}).toPromise();
    return response?.data?.authUrl || '';
  }

  async handleSocialCallback(platform: string, code: string, state: string): Promise<SocialAccount | null> {
    try {
      const response = await this.apiService.post<SocialAccount>('auth/social/callback', { platform, code, state }).toPromise();
      return response?.data || null;
    } catch (error) {
      console.error('Social callback error:', error);
      return null;
    }
  }

  async getSocialAccounts(): Promise<SocialAccount[]> {
    try {
      const response = await this.apiService.get<SocialAccount[]>('auth/social/accounts').toPromise();
      return response?.data || [];
    } catch (error) {
      console.error('Get social accounts error:', error);
      return [];
    }
  }

  async disconnectSocialAccount(accountId: number): Promise<boolean> {
    try {
      const response = await this.apiService.delete(`auth/social/accounts/${accountId}`).toPromise();
      return response?.success || false;
    } catch (error) {
      console.error('Disconnect social account error:', error);
      return false;
    }
  }

  async setTokens(accessToken: string, refreshToken: string): Promise<void> {
    localStorage.setItem('accessToken', accessToken);
    localStorage.setItem('refreshToken', refreshToken);
    
    // Try to decode the access token to get user info
    try {
      const payload = JSON.parse(atob(accessToken.split('.')[1]));
      
      const user: User = {
        id: parseInt(payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']) || 0,
        email: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] || '',
        name: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || '',
        picture: payload.picture_url || '',
        pictureUrl: payload.picture_url || '',
        googleId: payload.googleId || '',
        authProvider: payload.auth_provider || 'google',
        createdAt: payload.createdAt || new Date().toISOString()
      };
      
      localStorage.setItem('user', JSON.stringify(user));
      this.currentUserSubject.next(user);
    } catch (error) {
      console.error('Error decoding access token:', error);
      // If we can't decode the token, we'll need to make an API call to get user info
      // For now, we'll create a minimal user object
      const user: User = {
        id: 0,
        email: '',
        name: '',
        picture: '',
        pictureUrl: '',
        googleId: '',
        authProvider: 'google',
        createdAt: new Date().toISOString()
      };
      localStorage.setItem('user', JSON.stringify(user));
      this.currentUserSubject.next(user);
    }
  }

  private setAuthData(authResult: AuthResult): void {
    localStorage.setItem('accessToken', authResult.accessToken);
    localStorage.setItem('refreshToken', authResult.refreshToken);
    localStorage.setItem('user', JSON.stringify(authResult.user));
    this.currentUserSubject.next(authResult.user);
  }

  private clearAuthData(): void {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
    this.currentUserSubject.next(null);
  }

  private loadUserFromStorage(): void {
    const userJson = localStorage.getItem('user');
    if (userJson) {
      try {
        const user = JSON.parse(userJson);
        this.currentUserSubject.next(user);
      } catch (error) {
        console.error('Error parsing user from storage:', error);
        this.clearAuthData();
      }
    }
  }
}
