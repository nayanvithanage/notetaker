import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterOutlet } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { AuthService } from '../../services/auth.service';
import { User } from '../../models/auth.model';

@Component({
  selector: 'app-main-layout',
  templateUrl: './main-layout.component.html',
  styleUrls: ['./main-layout.component.scss'],
  imports: [
    CommonModule,
    RouterOutlet,
    MatToolbarModule,
    MatSidenavModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule
  ],
  standalone: true
})
export class MainLayoutComponent implements OnInit {
  currentUser: User | null = null;
  isAuthenticated = false;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit() {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
      this.isAuthenticated = !!user;
      
      // If no user is authenticated, set a mock user for development
      if (!user && !this.isAuthenticated) {
        this.setMockUser();
      }
      
      // Set up global functions for testing
      this.setCustomUserName(this.currentUser?.name || 'Notetaker User');
      
      // Make Gmail user switching available globally
      (window as any).switchToGmailUser = (userType: 'john' | 'sarah' | 'alex' | 'maria' | 'david') => {
        this.switchToGmailUser(userType);
      };
      
      // Make custom name setting available globally
      (window as any).setUserName = (newName: string) => this.updateUserName(newName);
    });
  }

  private setMockUser() {
    // Set a mock Gmail user for development/testing purposes
    const mockUser: User = {
      id: 1,
      email: 'john.doe@gmail.com',
      name: 'John Doe',
      pictureUrl: 'https://lh3.googleusercontent.com/a/default-user',
      picture: 'https://lh3.googleusercontent.com/a/default-user',
      googleId: 'mock-google-id-123456789',
      authProvider: 'google',
      createdAt: new Date().toISOString()
    };
    
    this.currentUser = mockUser;
    this.isAuthenticated = true;
  }

  async logout() {
    try {
      await this.authService.logout();
      this.router.navigate(['/login']);
    } catch (error) {
      console.error('Logout error:', error);
    }
  }

  navigateTo(route: string) {
    this.router.navigate([route]);
  }

  getUserDisplayName(): string {
    if (!this.currentUser) {
      return 'user@gmail.com';
    }
    
    // Show the full email address
    if (this.currentUser.email && this.currentUser.email.trim()) {
      return this.currentUser.email;
    }
    
    return 'user@gmail.com';
  }

  // Method to update user name (for testing/development)
  updateUserName(newName: string) {
    if (this.currentUser) {
      this.currentUser.name = newName;
      // Update localStorage if user is stored there
      localStorage.setItem('user', JSON.stringify(this.currentUser));
    }
  }

  // Method to set a custom user name (for testing)
  setCustomUserName(name: string) {
    this.updateUserName(name);
    // Make it available globally for console testing
    (window as any).setUserName = (newName: string) => this.updateUserName(newName);
  }

  // Method to switch to different Gmail users for testing
  switchToGmailUser(userType: 'john' | 'sarah' | 'alex' | 'maria' | 'david') {
    const gmailUsers = {
      john: { name: 'John Doe', email: 'john.doe@gmail.com' },
      sarah: { name: 'Sarah Johnson', email: 'sarah.johnson@gmail.com' },
      alex: { name: 'Alex Chen', email: 'alex.chen@gmail.com' },
      maria: { name: 'Maria Garcia', email: 'maria.garcia@gmail.com' },
      david: { name: 'David Wilson', email: 'david.wilson@gmail.com' }
    };

    const user = gmailUsers[userType];
    if (user && this.currentUser) {
      this.currentUser.name = user.name;
      this.currentUser.email = user.email;
      this.currentUser.googleId = `mock-google-id-${userType}`;
      localStorage.setItem('user', JSON.stringify(this.currentUser));
    }
  }
}
