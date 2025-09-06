import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Meeting } from '../../models/meeting.model';
import { MeetingService } from '../../services/meeting.service';

export interface BotDetailsData {
  meeting: Meeting;
  botStatus?: {
    id: string;
    status: string;
    meeting_url?: string;
    start_time?: Date;
    end_time?: Date;
    error?: string;
  };
}

@Component({
  selector: 'app-bot-details-modal',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    MatButtonModule,
    MatChipsModule,
    MatProgressSpinnerModule
  ],
  template: `
    <div class="bot-details-modal">
      <div class="modal-header">
        <h2 mat-dialog-title>Bot Details</h2>
        <button mat-icon-button (click)="close()">
          <mat-icon>close</mat-icon>
        </button>
      </div>
      
      <div class="modal-content" mat-dialog-content>
        <div class="meeting-info">
          <h3>{{ data.meeting.title }}</h3>
          <p class="meeting-time">
            {{ data.meeting.startsAt | date:'medium' }} - {{ data.meeting.endsAt | date:'short' }}
          </p>
          <p class="meeting-platform">{{ data.meeting.platform | titlecase }}</p>
        </div>
        
        <div class="bot-status" *ngIf="botStatus || data.botStatus">
          <h4>Bot Status</h4>
          <div class="status-info" *ngIf="!loading">
            <mat-chip [color]="getStatusColor((botStatus || data.botStatus)?.status)" selected>
              {{ (botStatus || data.botStatus)?.status | titlecase }}
            </mat-chip>
            <p class="bot-id">Bot ID: {{ (botStatus || data.botStatus)?.id }}</p>
          </div>
          
          <div class="loading-info" *ngIf="loading">
            <mat-spinner diameter="20"></mat-spinner>
            <span>Loading bot status...</span>
          </div>
          
          <div class="bot-details" *ngIf="(botStatus || data.botStatus)?.start_time || (botStatus || data.botStatus)?.end_time">
            <p *ngIf="(botStatus || data.botStatus)?.start_time">
              <strong>Started:</strong> {{ (botStatus || data.botStatus)?.start_time | date:'medium' }}
            </p>
            <p *ngIf="(botStatus || data.botStatus)?.end_time">
              <strong>Ended:</strong> {{ (botStatus || data.botStatus)?.end_time | date:'medium' }}
            </p>
          </div>
          
          <div class="error-info" *ngIf="(botStatus || data.botStatus)?.error">
            <mat-icon color="warn">error</mat-icon>
            <p class="error-message">{{ (botStatus || data.botStatus)?.error }}</p>
          </div>
        </div>
        
        <div class="no-bot-info" *ngIf="!botStatus && !data.botStatus && !loading">
          <mat-icon>robot</mat-icon>
          <p>No bot information available for this meeting.</p>
        </div>
      </div>
      
      <div class="modal-actions" mat-dialog-actions>
        <button mat-button (click)="close()">Close</button>
        <button mat-raised-button color="primary" (click)="refreshBotStatus()" [disabled]="loading">
          <mat-icon>refresh</mat-icon>
          Refresh Status
        </button>
      </div>
    </div>
  `,
  styles: [`
    .bot-details-modal {
      min-width: 500px;
      max-width: 600px;
    }
    
    .modal-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 20px 24px 0;
      border-bottom: 1px solid #e0e0e0;
      margin-bottom: 20px;
    }
    
    .modal-header h2 {
      margin: 0;
      color: #333;
    }
    
    .modal-content {
      padding: 0 24px;
    }
    
    .meeting-info {
      margin-bottom: 24px;
    }
    
    .meeting-info h3 {
      margin: 0 0 8px 0;
      color: #333;
      font-size: 18px;
    }
    
    .meeting-time {
      color: #666;
      margin: 4px 0;
    }
    
    .meeting-platform {
      color: #888;
      font-size: 14px;
      margin: 4px 0;
    }
    
    .bot-status h4 {
      margin: 0 0 12px 0;
      color: #333;
    }
    
    .status-info {
      margin-bottom: 16px;
    }
    
    .bot-id {
      font-family: monospace;
      font-size: 12px;
      color: #666;
      margin: 8px 0 0 0;
    }
    
    .bot-details {
      margin-bottom: 16px;
    }
    
    .bot-details p {
      margin: 4px 0;
      color: #666;
    }
    
    .error-info {
      display: flex;
      align-items: flex-start;
      gap: 8px;
      padding: 12px;
      background: #ffebee;
      border-radius: 4px;
      border-left: 4px solid #f44336;
    }
    
    .error-message {
      margin: 0;
      color: #d32f2f;
      font-size: 14px;
    }
    
    .no-bot-info {
      text-align: center;
      padding: 40px 20px;
      color: #666;
    }
    
    .no-bot-info mat-icon {
      font-size: 48px;
      width: 48px;
      height: 48px;
      margin-bottom: 16px;
    }
    
    .modal-actions {
      display: flex;
      justify-content: flex-end;
      gap: 12px;
      padding: 20px 24px;
      border-top: 1px solid #e0e0e0;
    }
    
    .modal-actions button {
      display: flex;
      align-items: center;
      gap: 6px;
    }
    
    .loading-info {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 16px;
      background: #f5f5f5;
      border-radius: 4px;
      margin-bottom: 16px;
    }
    
    .loading-info span {
      color: #666;
      font-size: 14px;
    }
  `]
})
export class BotDetailsModalComponent implements OnInit {
  loading = false;
  botStatus: any = null;

  constructor(
    public dialogRef: MatDialogRef<BotDetailsModalComponent>,
    @Inject(MAT_DIALOG_DATA) public data: BotDetailsData,
    private meetingService: MeetingService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadBotStatus();
  }

  close(): void {
    this.dialogRef.close();
  }

  getStatusColor(status: string): string {
    switch (status?.toLowerCase()) {
      case 'scheduled': return 'primary';
      case 'recording': return 'accent';
      case 'done': return 'primary';
      case 'error': return 'warn';
      default: return 'basic';
    }
  }

  loadBotStatus(): void {
    if (!this.data.meeting.recallBotId) {
      return;
    }

    this.loading = true;
    this.meetingService.getBotStatus(this.data.meeting.id).subscribe({
      next: (response) => {
        this.loading = false;
        if (response.success) {
          this.botStatus = response.data;
        } else {
          this.snackBar.open(`Failed to load bot status: ${response.message}`, 'Close', {
            duration: 5000
          });
        }
      },
      error: (error) => {
        this.loading = false;
        console.error('Error loading bot status:', error);
        this.snackBar.open('Error loading bot status', 'Close', {
          duration: 5000
        });
      }
    });
  }

  refreshBotStatus(): void {
    this.loadBotStatus();
  }
}
