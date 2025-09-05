import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { AutomationService } from '../../services/automation.service';
import { Automation } from '../../models/automation.model';

@Component({
  selector: 'app-automations',
  templateUrl: './automations.html',
  styleUrls: ['./automations.scss'],
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatDialogModule,
    MatSnackBarModule,
    MatSlideToggleModule
  ],
  standalone: true
})
export class AutomationsComponent implements OnInit {
  automations: Automation[] = [];
  loading = false;

  constructor(
    private router: Router,
    private automationService: AutomationService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit() {
    this.loadAutomations();
  }

  async loadAutomations() {
    this.loading = true;
    try {
      // For now, use sample data until the API is fully connected
      this.automations = this.getSampleAutomations();
      
      // TODO: Replace with actual API call when backend is ready
      // const response = await this.automationService.getAutomations().toPromise();
      // this.automations = response?.data || [];
    } catch (error) {
      console.error('Error loading automations:', error);
      this.snackBar.open('Failed to load automations', 'Close', { duration: 3000 });
    } finally {
      this.loading = false;
    }
  }

  private getSampleAutomations(): Automation[] {
    return [
      {
        id: 1,
        name: 'Meeting Summary',
        description: 'Generate a concise summary of key points from the meeting',
        prompt: 'Create a professional summary of the following meeting transcript, highlighting the main topics discussed, decisions made, and action items.',
        isActive: true,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString()
      },
      {
        id: 2,
        name: 'Action Items',
        description: 'Extract and format action items from meeting discussions',
        prompt: 'Extract all action items from this meeting transcript. Format them as a numbered list with clear ownership and deadlines where mentioned.',
        isActive: true,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString()
      },
      {
        id: 3,
        name: 'LinkedIn Post',
        description: 'Create engaging LinkedIn posts from meeting insights',
        prompt: 'Create an engaging LinkedIn post based on this meeting content. Make it professional, insightful, and include relevant hashtags.',
        isActive: false,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString()
      },
      {
        id: 4,
        name: 'Email Follow-up',
        description: 'Generate follow-up emails for meeting participants',
        prompt: 'Create a professional follow-up email summarizing the meeting outcomes and next steps for all participants.',
        isActive: true,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString()
      }
    ];
  }

  createAutomation() {
    // TODO: Open create automation dialog
    this.snackBar.open('Create automation dialog will open here', 'Close', { duration: 3000 });
  }

  editAutomation(automation: Automation) {
    // TODO: Open edit automation dialog
    this.snackBar.open(`Edit automation: ${automation.name}`, 'Close', { duration: 3000 });
  }

  async deleteAutomation(automation: Automation) {
    if (confirm(`Are you sure you want to delete "${automation.name}"?`)) {
      try {
        // TODO: Replace with actual API call
        // await this.automationService.deleteAutomation(automation.id).toPromise();
        
        this.automations = this.automations.filter(a => a.id !== automation.id);
        this.snackBar.open('Automation deleted successfully', 'Close', { duration: 3000 });
      } catch (error) {
        console.error('Error deleting automation:', error);
        this.snackBar.open('Failed to delete automation', 'Close', { duration: 3000 });
      }
    }
  }

  async toggleAutomation(automation: Automation) {
    try {
      // TODO: Replace with actual API call
      // await this.automationService.toggleAutomation(automation.id, !automation.isActive).toPromise();
      
      automation.isActive = !automation.isActive;
      this.snackBar.open(
        `Automation ${automation.isActive ? 'enabled' : 'disabled'}`, 
        'Close', 
        { duration: 3000 }
      );
    } catch (error) {
      console.error('Error toggling automation:', error);
      this.snackBar.open('Failed to toggle automation', 'Close', { duration: 3000 });
    }
  }

  getStatusColor(isActive: boolean): string {
    return isActive ? 'primary' : 'basic';
  }

  getStatusText(isActive: boolean): string {
    return isActive ? 'Active' : 'Inactive';
  }
}