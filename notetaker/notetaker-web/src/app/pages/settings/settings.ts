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
import { AuthService } from '../../services/auth.service';
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
    MatSnackBarModule
  ],
  standalone: true
})
export class SettingsComponent implements OnInit {
  currentUser: User | null = null;
  selectedTab = 0;

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

  constructor(
    private authService: AuthService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit() {
    this.loadUserProfile();
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

  connectGoogleCalendar() {
    // TODO: Implement Google Calendar connection
    this.snackBar.open('Google Calendar connection will open here', 'Close', { duration: 3000 });
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
}