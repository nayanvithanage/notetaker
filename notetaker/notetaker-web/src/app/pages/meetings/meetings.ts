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
import { MeetingService } from '../../services/meeting.service';
import { Meeting } from '../../models/meeting.model';

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

  constructor(
    private meetingService: MeetingService,
    private router: Router
  ) {}

  ngOnInit() {
    console.log('Meetings component initialized');
    this.loadMeetings();
  }

  async loadMeetings() {
    this.loading = true;
    try {
      // For now, use sample data until the API is fully connected
      this.upcomingMeetings = this.getSampleUpcomingMeetings();
      this.pastMeetings = this.getSamplePastMeetings();
      
      // TODO: Replace with actual API calls when backend is ready
      // const upcomingResponse = await this.meetingService.getMeetings('upcoming').toPromise();
      // const pastResponse = await this.meetingService.getMeetings('past').toPromise();
      // this.upcomingMeetings = upcomingResponse?.data || [];
      // this.pastMeetings = pastResponse?.data || [];
    } catch (error) {
      console.error('Error loading meetings:', error);
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

  async toggleNotetaker(meeting: Meeting, enabled: boolean) {
    try {
      await this.meetingService.toggleNotetaker(meeting.calendarEventId, enabled).toPromise();
      meeting.notetakerEnabled = enabled;
    } catch (error) {
      console.error('Error toggling notetaker:', error);
    }
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
}