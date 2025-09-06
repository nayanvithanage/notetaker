import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatTabsModule } from '@angular/material/tabs';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { MeetingService } from '../../services/meeting.service';
import { AuthService } from '../../services/auth.service';
import { ApiService } from '../../services/api.service';
import { Meeting } from '../../models/meeting.model';
import { BotDetailsModalComponent, BotDetailsData } from '../../components/bot-details-modal/bot-details-modal.component';

@Component({
  selector: 'app-meetings',
  templateUrl: './meetings.html',
  styleUrls: ['./meetings.scss'],
  imports: [
    CommonModule,
    MatTabsModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatSlideToggleModule,
    MatChipsModule,
    MatProgressSpinnerModule
  ],
  standalone: true
})
export class MeetingsComponent implements OnInit {
  upcomingMeetings: Meeting[] = [];
  pastMeetings: Meeting[] = [];
  loading = false;
  togglingMeetings = new Set<number>();

  constructor(
    private meetingService: MeetingService,
    private authService: AuthService,
    private apiService: ApiService,
    private router: Router,
    private dialog: MatDialog
  ) {}

  ngOnInit() {
    console.log('Meetings component initialized');
    this.loadMeetings();
  }

  async loadMeetings() {
    this.loading = true;
    try {
      // Load calendar events from the API
      const now = new Date();
      const fromDate = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000); // 7 days ago
      const toDate = new Date(now.getTime() + 30 * 24 * 60 * 60 * 1000); // 30 days from now
      
      console.log('Loading calendar events from:', fromDate, 'to:', toDate);
      const response = await this.meetingService.getCalendarEvents(fromDate, toDate).toPromise();
      console.log('Calendar API response:', response);
      
      if (response?.success && response.data) {
        const allEvents = response.data;
        console.log('Received calendar events:', allEvents);
        const now = new Date();
        
        this.upcomingMeetings = allEvents
          .filter(event => new Date(event.startsAt) > now)
          .map(event => this.mapCalendarEventToMeeting(event));
          
        this.pastMeetings = allEvents
          .filter(event => new Date(event.startsAt) <= now)
          .map(event => this.mapCalendarEventToMeeting(event));
          
        console.log('Upcoming meetings:', this.upcomingMeetings);
        console.log('Past meetings:', this.pastMeetings);
      } else {
        console.log('API failed or no data, using sample data');
        // Fallback to sample data if API fails
        this.upcomingMeetings = this.getSampleUpcomingMeetings();
        this.pastMeetings = this.getSamplePastMeetings();
      }
    } catch (error) {
      console.error('Error loading meetings:', error);
      // Fallback to sample data on error
      this.upcomingMeetings = this.getSampleUpcomingMeetings();
      this.pastMeetings = this.getSamplePastMeetings();
    } finally {
      this.loading = false;
    }
  }

  private getSampleUpcomingMeetings(): Meeting[] {
    return [
      {
        id: 1,
        title: 'Weekly Team Standup',
        description: 'Daily standup meeting with the development team',
        startsAt: new Date(Date.now() + 2 * 60 * 60 * 1000).toISOString(), // 2 hours from now
        endsAt: new Date(Date.now() + 3 * 60 * 60 * 1000).toISOString(), // 3 hours from now
        platform: 'Zoom',
        joinUrl: 'https://zoom.us/j/123456789',
        attendees: ['john.doe@company.com', 'jane.smith@company.com', 'bob.wilson@company.com'],
        notetakerEnabled: true,
        status: 'scheduled',
        calendarEventId: 'cal_event_1',
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString()
      },
      {
        id: 2,
        title: 'Product Planning Session',
        description: 'Planning next quarter product roadmap',
        startsAt: new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString(), // Tomorrow
        endsAt: new Date(Date.now() + 25 * 60 * 60 * 1000).toISOString(),
        platform: 'Teams',
        joinUrl: 'https://teams.microsoft.com/l/meetup-join/...',
        attendees: ['alice.johnson@company.com', 'charlie.brown@company.com'],
        notetakerEnabled: false,
        status: 'scheduled',
        calendarEventId: 'cal_event_2',
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString()
      }
    ];
  }

  private getSamplePastMeetings(): Meeting[] {
    return [
      {
        id: 3,
        title: 'Client Demo Presentation',
        description: 'Demonstrating new features to client',
        startsAt: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000).toISOString(), // 2 days ago
        endsAt: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000 + 60 * 60 * 1000).toISOString(),
        platform: 'Google Meet',
        attendees: ['client@example.com', 'sales@company.com'],
        notetakerEnabled: true,
        status: 'ready',
        calendarEventId: 'cal_event_3',
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString()
      },
      {
        id: 4,
        title: 'Sprint Retrospective',
        description: 'Reviewing last sprint and planning improvements',
        startsAt: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString(), // 1 week ago
        endsAt: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000 + 90 * 60 * 1000).toISOString(),
        platform: 'Zoom',
        attendees: ['dev.team@company.com'],
        notetakerEnabled: true,
        status: 'processing',
        calendarEventId: 'cal_event_4',
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString()
      }
    ];
  }

  async refreshMeetings() {
    await this.loadMeetings();
  }

  async syncCalendar() {
    this.loading = true;
    try {
      console.log('Starting calendar sync...');
      
      // Get all connected Google accounts and sync them
      const socialAccounts = await this.authService.getSocialAccounts();
      console.log('Retrieved social accounts:', socialAccounts);
      
      const googleAccounts = socialAccounts.filter((account: any) => account.platform === 'google');
      console.log('Filtered Google accounts:', googleAccounts);
      
      if (googleAccounts.length === 0) {
        console.log('No Google accounts connected');
        alert('No Google accounts connected. Please connect a Google account first.');
        return;
      }
      

      // Sync each Google account
      for (const account of googleAccounts) {
        console.log('Syncing account:', account);
        try {
          const response = await this.meetingService.syncCalendarEvents(account.id).toPromise();
          console.log('Sync response for account', account.id, ':', response);
          
          if (response?.success) {
            console.log(`Successfully synced account: ${account.displayName || account.accountId}`);
          } else {
            console.error(`Failed to sync account ${account.id}:`, response?.message);
            alert(`Failed to sync account ${account.displayName || account.accountId}: ${response?.message || 'Unknown error'}`);
          }
        } catch (accountError: any) {
          console.error(`Error syncing account ${account.id}:`, accountError);
          const errorMessage = accountError?.error?.message || accountError?.message || accountError?.toString() || 'Unknown error';
          alert(`Error syncing account ${account.displayName || account.accountId}: ${errorMessage}`);
        }
      }
      
      // Reload meetings after sync
      console.log('Reloading meetings after sync...');
      await this.loadMeetings();
      console.log('Calendar sync completed');
      alert('Calendar sync completed successfully!');
    } catch (error: any) {
      console.error('Error syncing calendar:', error);
      const errorMessage = error?.error?.message || error?.message || error?.toString() || 'Unknown error';
      alert(`Error syncing calendar: ${errorMessage}`);
    } finally {
      this.loading = false;
    }
  }

  async toggleNotetaker(meeting: Meeting, enabled: boolean) {
    const originalState = meeting.notetakerEnabled;
    const meetingId = parseInt(meeting.calendarEventId);
    
    console.log('Toggle notetaker for meeting:', meeting);
    console.log('CalendarEventId:', meeting.calendarEventId, 'Parsed ID:', meetingId);
    
    if (isNaN(meetingId) || meetingId === 0) {
      console.error('Invalid calendarEventId:', meeting.calendarEventId);
      alert('Invalid meeting ID. Please refresh the page and try again.');
      return;
    }

    
    // Add to loading set
    this.togglingMeetings.add(meetingId);
    
    // Optimistically update UI
    meeting.notetakerEnabled = enabled;
    
    try {
      console.log('Sending toggle request:', { calendarEventId: meetingId, enabled });
      const response = await this.meetingService.toggleNotetaker(meetingId, enabled).toPromise();
      console.log('Toggle response:', response);
      
      if (response?.success) {
        // Show success message
        console.log(`Notetaker ${enabled ? 'enabled' : 'disabled'} successfully for meeting: ${meeting.title}`);
        
        // Refresh meetings to get updated data with bot details
        await this.loadMeetings();
      } else {
        // Revert UI state on failure
        meeting.notetakerEnabled = originalState;
        console.error('Failed to toggle notetaker:', response?.message || 'Unknown error');
        alert(`Failed to ${enabled ? 'enable' : 'disable'} notetaker: ${response?.message || 'Unknown error'}`);
      }
    } catch (error) {
      // Revert UI state on error
      meeting.notetakerEnabled = originalState;
      console.error('Error toggling notetaker:', error);
      
      // Show user-friendly error message
      alert(`Failed to ${enabled ? 'enable' : 'disable'} notetaker. Please try again.`);
    } finally {
      // Remove from loading set
      this.togglingMeetings.delete(meetingId);
    }
  }

  isToggling(meeting: Meeting): boolean {
    return this.togglingMeetings.has(parseInt(meeting.calendarEventId));
  }

  openBotDetails(meeting: Meeting): void {
    console.log('Opening bot details for meeting:', meeting);
    console.log('Meeting recallBotId:', meeting.recallBotId);
    console.log('Meeting status:', meeting.status);
    
    const dialogData: BotDetailsData = {
      meeting: meeting,
      botStatus: meeting.recallBotId ? {
        id: meeting.recallBotId,
        status: meeting.status,
        meeting_url: meeting.joinUrl
      } : undefined
    };

    console.log('Dialog data:', dialogData);

    const dialogRef = this.dialog.open(BotDetailsModalComponent, {
      data: dialogData,
      width: '600px',
      maxWidth: '90vw'
    });

    // Refresh meeting data when dialog closes
    dialogRef.afterClosed().subscribe(() => {
      this.loadMeetings();
    });
  }

  joinMeeting(meeting: Meeting) {
    if (meeting.joinUrl) {
      window.open(meeting.joinUrl, '_blank');
    }
  }

  viewMeeting(meeting: Meeting) {
    this.router.navigate(['/meetings', meeting.id]);
  }

  async generateContent(meeting: Meeting) {
    // This would open a modal or navigate to content generation
    console.log('Generate content for meeting:', meeting.id);
  }

  getPlatformIcon(platform: string): string {
    switch (platform.toLowerCase()) {
      case 'zoom': return 'video_call';
      case 'teams': return 'groups';
      case 'meet': return 'meeting_room';
      default: return 'meeting_room';
    }
  }

  getStatusColor(status: string): string {
    switch (status.toLowerCase()) {
      case 'ready': return 'primary';
      case 'processing': return 'accent';
      case 'failed': return 'warn';
      default: return 'basic';
    }
  }

  private mapCalendarEventToMeeting(event: any): Meeting {
    console.log('Mapping calendar event to meeting:', event);
    console.log('Event recallBotId:', event.recallBotId);
    console.log('Event meetingStatus:', event.meetingStatus);
    console.log('Event meetingId:', event.meetingId);
    
    const meeting: Meeting = {
      id: event.meetingId || event.id, // Use meetingId if available, otherwise use event.id
      title: event.title,
      description: event.description || '',
      startsAt: event.startsAt,
      endsAt: event.endsAt,
      platform: event.platform || 'unknown',
      joinUrl: event.joinUrl || '',
      attendees: event.attendees || [],
      notetakerEnabled: event.notetakerEnabled || false,
      status: event.meetingStatus || 'scheduled',
      calendarEventId: event.id.toString(), // Use the calendar event ID directly
      recallBotId: event.recallBotId,
      createdAt: event.createdAt || new Date().toISOString(),
      updatedAt: event.updatedAt || new Date().toISOString()
    };
    
    console.log('Mapped meeting:', meeting);
    return meeting;
  }
}