import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTabsModule } from '@angular/material/tabs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { MatChipsModule } from '@angular/material/chips';
import { AuthService } from '../../services/auth.service';
import { MeetingService } from '../../services/meeting.service';
import { User } from '../../models/auth.model';

@Component({
  selector: 'app-settings',
  templateUrl: './settings.html',
  styleUrls: ['./settings.scss'],
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatTabsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatSlideToggleModule,
    MatSnackBarModule,
    MatChipsModule
  ],
  standalone: true
})
export class SettingsComponent implements OnInit {
  currentUser: User | null = null;
  selectedTab = 0;
  googleAccounts: any[] = [];
  isLoadingAccounts = false;

  // Profile settings
  profileForm = {
    name: '',
    email: '',
    picture: ''
  };

  // Notification settings
  notificationSettings = {
    emailNotifications: true,
    meetingReminders: true,
    contentGenerated: true,
    weeklyDigest: false
  };

  // Integration settings
  integrationSettings = {
    googleCalendar: true,
    linkedin: false,
    facebook: false,
    recallAi: false
  };

  // Privacy settings
  privacySettings = {
    dataRetention: '1year',
    shareAnalytics: false,
    allowMarketing: false
  };

  // Bot settings
  botSettings = {
    leadMinutes: 5
  };

  constructor(
    private authService: AuthService,
    private meetingService: MeetingService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit() {
    this.loadUserProfile();
    this.loadGoogleAccounts();
    this.loadBotSettings();
  }

  loadUserProfile() {
    this.currentUser = this.authService.getCurrentUser();
    if (this.currentUser) {
      this.profileForm = {
        name: this.currentUser.name,
        email: this.currentUser.email,
        picture: this.currentUser.picture || ''
      };
    }
  }

  async saveProfile() {
    try {
      // TODO: Implement profile update API call
      this.snackBar.open('Profile updated successfully', 'Close', { duration: 3000 });
    } catch (error) {
      console.error('Error updating profile:', error);
      this.snackBar.open('Failed to update profile', 'Close', { duration: 3000 });
    }
  }

  async saveNotifications() {
    try {
      // TODO: Implement notification settings API call
      this.snackBar.open('Notification settings updated', 'Close', { duration: 3000 });
    } catch (error) {
      console.error('Error updating notifications:', error);
      this.snackBar.open('Failed to update notification settings', 'Close', { duration: 3000 });
    }
  }

  async saveIntegrations() {
    try {
      // TODO: Implement integration settings API call
      this.snackBar.open('Integration settings updated', 'Close', { duration: 3000 });
    } catch (error) {
      console.error('Error updating integrations:', error);
      this.snackBar.open('Failed to update integration settings', 'Close', { duration: 3000 });
    }
  }

  async savePrivacy() {
    try {
      // TODO: Implement privacy settings API call
      this.snackBar.open('Privacy settings updated', 'Close', { duration: 3000 });
    } catch (error) {
      console.error('Error updating privacy settings:', error);
      this.snackBar.open('Failed to update privacy settings', 'Close', { duration: 3000 });
    }
  }

  async logout() {
    try {
      await this.authService.logout();
      this.snackBar.open('Logged out successfully', 'Close', { duration: 3000 });
    } catch (error) {
      console.error('Error logging out:', error);
      this.snackBar.open('Failed to logout', 'Close', { duration: 3000 });
    }
  }

  async loadGoogleAccounts() {
    this.isLoadingAccounts = true;
    try {
      const response = await this.authService.getSocialAccounts();
      this.googleAccounts = response.filter(account => account.platform === 'google');
    } catch (error) {
      console.error('Error loading Google accounts:', error);
      this.snackBar.open('Failed to load Google accounts', 'Close', { duration: 3000 });
    } finally {
      this.isLoadingAccounts = false;
    }
  }

  async connectGoogleCalendar() {
    try {
      const state = Math.random().toString(36).substring(2, 15) + Math.random().toString(36).substring(2, 15);
      localStorage.setItem('oauth_state', state);
      
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
      console.error('Error connecting Google Calendar:', error);
      this.snackBar.open('Failed to connect Google Calendar', 'Close', { duration: 3000 });
    }
  }

  async disconnectGoogleAccount(accountId: number) {
    try {
      const success = await this.authService.disconnectSocialAccount(accountId);
      if (success) {
        this.snackBar.open('Google account disconnected successfully', 'Close', { duration: 3000 });
        this.loadGoogleAccounts();
      } else {
        this.snackBar.open('Failed to disconnect Google account', 'Close', { duration: 3000 });
      }
    } catch (error) {
      console.error('Error disconnecting Google account:', error);
      this.snackBar.open('Failed to disconnect Google account', 'Close', { duration: 3000 });
    }
  }

  async syncGoogleAccount(accountId: number) {
    try {
      const response = await this.meetingService.syncCalendarEvents(accountId).toPromise();
      if (response?.success) {
        this.snackBar.open('Google Calendar synced successfully', 'Close', { duration: 3000 });
      } else {
        this.snackBar.open('Failed to sync Google Calendar', 'Close', { duration: 3000 });
      }
    } catch (error) {
      console.error('Error syncing Google Calendar:', error);
      this.snackBar.open('Failed to sync Google Calendar', 'Close', { duration: 3000 });
    }
  }

  connectLinkedIn() {
    // TODO: Implement LinkedIn connection
    this.snackBar.open('LinkedIn connection will open here', 'Close', { duration: 3000 });
  }

  connectFacebook() {
    // TODO: Implement Facebook connection
    this.snackBar.open('Facebook connection will open here', 'Close', { duration: 3000 });
  }

  connectRecallAi() {
    // TODO: Implement Recall.ai connection
    this.snackBar.open('Recall.ai connection will open here', 'Close', { duration: 3000 });
  }

  async loadBotSettings() {
    try {
      const response = await this.meetingService.getBotSettings().toPromise();
      if (response?.success && response.data) {
        this.botSettings = response.data;
      }
    } catch (error) {
      console.error('Error loading bot settings:', error);
    }
  }

  async saveBotSettings() {
    try {
      const response = await this.meetingService.updateBotSettings(this.botSettings).toPromise();
      if (response?.success) {
        this.snackBar.open('Bot settings saved!', 'Close', { duration: 3000 });
      } else {
        this.snackBar.open('Failed to save bot settings', 'Close', { duration: 3000 });
      }
    } catch (error) {
      console.error('Error saving bot settings:', error);
      this.snackBar.open('Failed to save bot settings', 'Close', { duration: 3000 });
    }
  }
}