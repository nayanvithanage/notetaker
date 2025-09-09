import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTabsModule } from '@angular/material/tabs';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MeetingService } from '../../services/meeting.service';
import { AutomationService } from '../../services/automation.service';
import { Meeting, MeetingDetail, GeneratedContent } from '../../models/meeting.model';
import { Automation } from '../../models/automation.model';

@Component({
  selector: 'app-meeting-detail',
  templateUrl: './meeting-detail.html',
  styleUrls: ['./meeting-detail.scss'],
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatTabsModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatTooltipModule
  ],
  standalone: true
})
export class MeetingDetailComponent implements OnInit {
  meeting: MeetingDetail | null = null;
  automations: Automation[] = [];
  loading = false;
  selectedTab = 0;
  selectedAutomation: number | null = null;
  customPrompt = '';
  generatedContent = '';
  isGenerating = false;
  isPosting = false;
  socialPlatforms = ['LinkedIn', 'Facebook'];
  selectedPlatforms: string[] = [];
  socialPosts: any[] = [];
  followUpEmail: any = null;
  isSendingEmail = false;
  isFetchingTranscript = false;
  isFindingBots = false;
  hasSearchedForBots = false; // Prevent infinite loop

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private meetingService: MeetingService,
    private automationService: AutomationService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit() {
    // Reset bot search flag for new meeting
    this.hasSearchedForBots = false;
    
    const calendarEventId = this.route.snapshot.paramMap.get('calendarEventId');
    if (calendarEventId) {
      this.loadMeetingByCalendarEvent(calendarEventId);
    } else {
      this.loadMeeting();
    }
    this.loadAutomations();
    this.loadSocialPosts();
    this.loadFollowUpEmail();
  }

  async loadMeeting() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      console.error('No meeting ID provided in route');
      this.snackBar.open('No meeting ID provided', 'Close', { duration: 3000 });
      this.router.navigate(['/meetings']);
      return;
    }

    this.loading = true;
    let meetingId: number | null = null;
    
    try {
      meetingId = parseInt(id);
      
      if (isNaN(meetingId)) {
        this.snackBar.open('Invalid meeting ID format', 'Close', { duration: 3000 });
        this.router.navigate(['/meetings']);
        return;
      }
      
      const response = await this.meetingService.getMeeting(meetingId).toPromise();
      
      if (response?.success && response.data) {
        this.meeting = response.data;

        // For meetings without a bot, try to find and link existing bots (only once)
        if (!this.meeting.recallBotId && this.meeting.id && !this.hasSearchedForBots) {
          await this.findExistingBots();
        }

        // Auto-fetch transcript if not present yet and we have a bot ID
        if (!this.meeting.transcriptText && this.meeting.recallBotId) {
          await this.fetchTranscriptByBotId();
        } else if (!this.meeting.transcriptText && this.meeting.id) {
          // If no transcript and no bot ID, try to get latest bot details
          await this.fetchLatestBotDetails();
        }
      } else {
        const errorMessage = response?.message || 'Unknown error occurred';
        this.snackBar.open(`Failed to load meeting: ${errorMessage}`, 'Close', { duration: 5000 });
        this.router.navigate(['/meetings']);
        return;
      }
    } catch (error) {
      console.error('Error loading meeting:', error);
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
      this.snackBar.open(`Error loading meeting details: ${errorMessage}`, 'Close', { duration: 5000 });
      this.router.navigate(['/meetings']);
      return;
    } finally {
      this.loading = false;
    }
  }

  async findExistingBots() {
    if (!this.meeting?.id) {
      return;
    }

    // Prevent multiple simultaneous bot searches
    if (this.isFindingBots || this.hasSearchedForBots) {
      return;
    }

    this.isFindingBots = true;
    this.hasSearchedForBots = true;
    
    try {
      const response = await this.meetingService.findExistingBots(this.meeting.id).toPromise();
      if (response?.success) {
        if (response.message?.includes('Successfully linked existing bot')) {
          // Wait a moment for database to be updated, then reload meeting details
          await new Promise(resolve => setTimeout(resolve, 1000)); // 1 second delay
          await this.loadMeeting();
          // Extract duration info from the message if available
          const durationMatch = response.message.match(/\((\d+)s recording\)/);
          const durationText = durationMatch ? ` with ${durationMatch[1]}s recording` : '';
          this.snackBar.open(`Found and linked existing bot${durationText}!`, 'Close', { duration: 5000 });
        }
      }
    } catch (error) {
      console.error('Error finding existing bots:', error);
    } finally {
      this.isFindingBots = false;
    }
  }

  async findExistingBotsForCalendarEvent(calendarEventId: number) {
    try {
      console.log('Searching for existing bots for calendar event:', calendarEventId);
      const response = await this.meetingService.findExistingBotsForCalendarEvent(calendarEventId).toPromise();
      if (response?.success) {
        console.log('Calendar event bot search result:', response.message);
        if (response.message?.includes('Successfully linked existing bot')) {
          // Extract bot ID from the response message
          const botIdMatch = response.message.match(/Successfully linked existing bot: ([a-f0-9-]+)/);
          const durationMatch = response.message.match(/\((\d+)s recording\)/);
          
          if (botIdMatch && this.meeting) {
            // Update the current meeting object with the bot ID
            this.meeting.recallBotId = botIdMatch[1];
            console.log('Updated meeting with bot ID:', this.meeting.recallBotId);
            
            const durationText = durationMatch ? ` with ${durationMatch[1]}s recording` : '';
            this.snackBar.open(`Found and linked existing bot${durationText}!`, 'Close', { duration: 5000 });
            
            // Try to fetch transcript if available - but we need a meeting ID for this
            // Since this is a calendar event, we'll need to reload to get the newly created meeting
            try {
              // Reload the page to get the updated meeting data with the linked bot
              window.location.reload();
            } catch (error) {
              console.log('Could not reload automatically:', error);
            }
          }
        } else if (response.message?.includes('already has a bot')) {
          console.log('Calendar event already has a bot linked');
        } else if (response.message?.includes('No existing bots')) {
          console.log('No existing bots found for this calendar event:', response.message);
        } else {
          console.log('Other calendar event bot search result:', response.message);
        }
      } else {
        console.warn('Calendar event bot search failed:', response?.message);
      }
    } catch (error) {
      console.error('Error finding existing bots for calendar event:', error);
    }
  }

  async fetchTranscript() {
    if (!this.meeting?.recallBotId) {
      this.snackBar.open('No bot ID available for this meeting', 'Close', { duration: 3000 });
      return;
    }

    this.isFetchingTranscript = true;
    try {
      const response = await this.meetingService.getTranscriptByBotId(this.meeting.recallBotId).toPromise();
      if (response?.success && response.data) {
        // Update the meeting with the fetched transcript
        this.meeting.transcriptText = response.data;
        this.snackBar.open('Transcript fetched successfully!', 'Close', { duration: 3000 });
      } else {
        this.snackBar.open(`Failed to fetch transcript: ${response?.message || 'Unknown error'}`, 'Close', { duration: 5000 });
      }
    } catch (error) {
      console.error('Error fetching transcript:', error);
      this.snackBar.open('Error fetching transcript', 'Close', { duration: 3000 });
    } finally {
      this.isFetchingTranscript = false;
    }
  }

  async fetchTranscriptByBotId() {
    if (!this.meeting?.recallBotId) {
      return;
    }

    this.isFetchingTranscript = true;
    try {
      const response = await this.meetingService.fetchTranscriptByBotId(this.meeting.recallBotId).toPromise();
      if (response?.success) {
        // Reload the meeting to get the updated transcript
        await this.loadMeeting();
        this.snackBar.open('Transcript fetched successfully!', 'Close', { duration: 3000 });
      } else {
        this.snackBar.open(`Failed to fetch transcript: ${response?.message || 'Unknown error'}`, 'Close', { duration: 5000 });
      }
    } catch (error) {
      console.error('Error fetching transcript:', error);
      this.snackBar.open('Error fetching transcript', 'Close', { duration: 3000 });
    } finally {
      this.isFetchingTranscript = false;
    }
  }

  async fetchLatestBotDetails() {
    if (!this.meeting?.id) {
      return;
    }

    this.isFetchingTranscript = true;
    try {
      const response = await this.meetingService.getLatestBotDetails(this.meeting.id).toPromise();
      if (response?.success && response.data) {
        const bot = response.data;
        
        // Check if bot has transcript available
        if (bot.hasTranscript) {
          // Update the meeting with the bot ID if not already set
          if (!this.meeting.recallBotId && bot.id) {
            this.meeting.recallBotId = bot.id;
          }
          // Fetch the transcript
          await this.fetchTranscriptByBotId();
        } else {
          this.snackBar.open('Bot found but transcript not ready yet. Status: ' + (bot.currentStatus || 'unknown'), 'Close', { duration: 5000 });
        }
      } else {
        this.snackBar.open(`Failed to fetch bot details: ${response?.message || 'Unknown error'}`, 'Close', { duration: 5000 });
      }
    } catch (error) {
      console.error('Error fetching bot details:', error);
      this.snackBar.open('Error fetching bot details', 'Close', { duration: 3000 });
    } finally {
      this.isFetchingTranscript = false;
    }
  }

  async reSyncMeetingBot() {
    if (!this.meeting?.id) {
      return;
    }

    this.isFetchingTranscript = true;
    try {
      const response = await this.meetingService.reSyncMeetingBot(this.meeting.id).toPromise();
      if (response?.success) {
        this.snackBar.open(response.message || 'Bot re-synced successfully!', 'Close', { duration: 5000 });
        // Reload the meeting to get the updated bot ID
        await this.loadMeeting();
      } else {
        this.snackBar.open(`Failed to re-sync bot: ${response?.message || 'Unknown error'}`, 'Close', { duration: 5000 });
      }
    } catch (error) {
      console.error('Error re-syncing bot:', error);
      this.snackBar.open('Error re-syncing bot', 'Close', { duration: 3000 });
    } finally {
      this.isFetchingTranscript = false;
    }
  }


  async loadMeetingByCalendarEvent(eventIdParam: string) {
    if (!eventIdParam) {
      this.snackBar.open('No calendar event ID provided', 'Close', { duration: 3000 });
      this.router.navigate(['/meetings']);
      return;
    }

    const eventId = parseInt(eventIdParam);
    if (isNaN(eventId)) {
      this.snackBar.open('Invalid calendar event ID', 'Close', { duration: 3000 });
      this.router.navigate(['/meetings']);
      return;
    }

    this.loading = true;
    try {
      const response = await this.meetingService.getCalendarEvents().toPromise();
      if (!response?.success || !Array.isArray(response.data)) {
        this.snackBar.open('Failed to load calendar events', 'Close', { duration: 3000 });
        this.router.navigate(['/meetings']);
        return;
      }

      const event = response.data.find((e: any) => e.id === eventId || e.Id === eventId);
      if (!event) {
        this.snackBar.open('Calendar event not found', 'Close', { duration: 3000 });
        this.router.navigate(['/meetings']);
        return;
      }

      const meetingId = event.meetingId ?? event.MeetingId;
      if (meetingId) {
        // load the canonical meeting detail and auto-fetch transcript
        this.router.navigate(['/meetings', meetingId]);
        return;
      }

      // Map event to minimal MeetingDetail for display
      this.meeting = {
        id: 0,
        calendarEventId: (event.id ?? event.Id).toString(),
        title: event.title ?? event.Title ?? 'Meeting',
        description: event.description ?? event.Description ?? '',
        startsAt: (event.startsAt ?? event.StartsAt)?.toString(),
        endsAt: (event.endsAt ?? event.EndsAt)?.toString(),
        platform: event.platform ?? event.Platform ?? 'unknown',
        joinUrl: event.joinUrl ?? event.JoinUrl ?? '',
        attendees: event.attendees ?? event.Attendees ?? [],
        notetakerEnabled: event.notetakerEnabled ?? event.NotetakerEnabled ?? false,
        status: event.meetingStatus ?? event.MeetingStatus ?? 'scheduled',
        recallBotId: event.recallBotId ?? event.RecallBotId,
        createdAt: (event.createdAt ?? event.CreatedAt)?.toString() ?? new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        // MeetingDetail extensions - Add mock transcript for testing
        transcriptText: "This is a sample meeting transcript for testing purposes.\n\n" +
          "Speaker 1: Welcome everyone to today's meeting. Let's start by reviewing our progress on the project.\n\n" +
          "Speaker 2: Thank you for having me. I've completed the initial analysis and here are my findings...\n\n" +
          "Speaker 1: That's excellent work. What are the next steps we need to take?\n\n" +
          "Speaker 2: Based on the analysis, I recommend we focus on three key areas: user experience, performance optimization, and security enhancements.\n\n" +
          "Speaker 1: Perfect. Let's assign tasks and set deadlines for each of these areas.\n\n" +
          "Speaker 3: I can take on the user experience improvements. I'll have a prototype ready by next Friday.\n\n" +
          "Speaker 2: I'll handle the performance optimization. That should take about two weeks.\n\n" +
          "Speaker 1: Great! I'll work on the security enhancements. Let's reconvene next week to review progress.\n\n" +
          "All: Sounds good. Meeting adjourned.",
        summaryJson: undefined,
        mediaUrls: [],
        generatedContents: [],
        socialPosts: []
      } as unknown as MeetingDetail;

      // For calendar events without a bot ID, try to find existing bots
      if (!this.meeting.recallBotId) {
        console.log('Calendar event has no bot ID - attempting to find existing bots');
        await this.findExistingBotsForCalendarEvent(eventId);
      }

      // If no meeting exists yet, we cannot fetch transcript by meeting id
    } catch (error) {
      console.error('Error loading event details:', error);
      this.snackBar.open('Error loading event details', 'Close', { duration: 3000 });
      this.router.navigate(['/meetings']);
      return;
    } finally {
      this.loading = false;
    }
  }

  async loadAutomations() {
    try {
      // For now, use sample data until the API is fully connected
      this.automations = this.getSampleAutomations();
      
      // TODO: Replace with actual API call when backend is ready
      // const response = await this.automationService.getAutomations().toPromise();
      // this.automations = response?.data || [];
    } catch (error) {
      console.error('Error loading automations:', error);
    }
  }

  // Removed sample meeting fallback to ensure only real meeting data is shown

  private getSampleAutomations(): Automation[] {
    return [
      {
        id: 1,
        name: 'Meeting Summary',
        description: 'Generate a concise summary of key points',
        prompt: 'Create a professional summary of the following meeting transcript, highlighting the main topics discussed, decisions made, and action items.',
        isActive: true,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString()
      },
      {
        id: 2,
        name: 'Action Items',
        description: 'Extract and format action items',
        prompt: 'Extract all action items from this meeting transcript. Format them as a numbered list with clear ownership and deadlines.',
        isActive: true,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString()
      },
      {
        id: 3,
        name: 'LinkedIn Post',
        description: 'Create engaging LinkedIn posts',
        prompt: 'Create an engaging LinkedIn post based on this meeting content. Make it professional, insightful, and include relevant hashtags.',
        isActive: true,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString()
      },
      {
        id: 4,
        name: 'Twitter Post',
        description: 'Create concise Twitter posts',
        prompt: 'Create a concise Twitter post based on this meeting content. Keep it under 280 characters, engaging, and include relevant hashtags.',
        isActive: true,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString()
      },
      {
        id: 5,
        name: 'Facebook Post',
        description: 'Create Facebook posts for team updates',
        prompt: 'Create a Facebook post based on this meeting content. Make it friendly, engaging, and suitable for team updates.',
        isActive: true,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString()
      },
      {
        id: 6,
        name: 'Instagram Post',
        description: 'Create Instagram posts with visual appeal',
        prompt: 'Create an Instagram post based on this meeting content. Make it visually appealing, use emojis, and include relevant hashtags.',
        isActive: false,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString()
      }
    ];
  }

  async generateContent() {
    if (!this.meeting) return;

    this.isGenerating = true;
    try {
      // TODO: Replace with actual API call when backend is ready
      // const prompt = this.selectedAutomation 
      //   ? this.automations.find(a => a.id === this.selectedAutomation)?.prompt || this.customPrompt
      //   : this.customPrompt;
      
      // const response = await this.meetingService.generateContent(this.meeting.id, prompt).toPromise();
      // this.generatedContent = response?.data?.content || '';
      
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 2000));
      this.generatedContent = this.getSampleGeneratedContent();
      
      this.snackBar.open('Content generated successfully', 'Close', { duration: 3000 });
    } catch (error) {
      console.error('Error generating content:', error);
      this.snackBar.open('Failed to generate content', 'Close', { duration: 3000 });
    } finally {
      this.isGenerating = false;
    }
  }

  private getSampleGeneratedContent(): string {
    return `# Meeting Summary: Weekly Team Standup

## Key Discussion Points
- Project Alpha is on track for the Q1 deadline
- Beta testing phase completed successfully
- New feature requests from client feedback
- Resource allocation for upcoming sprint

## Decisions Made
- Approve additional budget for UI improvements
- Schedule client demo for next Friday
- Assign Sarah to lead the new feature development

## Action Items
1. **John Doe** - Complete API documentation by Friday
2. **Jane Smith** - Prepare client demo presentation
3. **Bob Wilson** - Review and approve UI mockups
4. **Team** - Daily standup at 9 AM starting tomorrow

## Next Steps
- Continue with current sprint priorities
- Prepare for client demo next week
- Review and implement feedback from beta testing

---
*Generated by Notetaker AI on ${new Date().toLocaleDateString()}*`;
  }

  joinMeeting() {
    if (this.meeting?.joinUrl) {
      window.open(this.meeting.joinUrl, '_blank');
    }
  }

  isPastMeeting(): boolean {
    if (!this.meeting?.endsAt) return false;
    const end = new Date(this.meeting.endsAt).getTime();
    return end < Date.now();
  }

  isUpcomingMeeting(): boolean {
    return !this.isPastMeeting();
  }

  getBotIds(): string[] {
    if (!this.meeting) return [];
    // Return multiple bot IDs if available, otherwise fall back to single bot ID
    if (this.meeting.recallBotIds && this.meeting.recallBotIds.length > 0) {
      return this.meeting.recallBotIds;
    }
    if (this.meeting.recallBotId) {
      return [this.meeting.recallBotId];
    }
    return [];
  }

  getRecordingDuration(): string {
    if (!this.meeting?.startsAt || !this.meeting?.endsAt) return '';

    const start = new Date(this.meeting.startsAt).getTime();
    const end = new Date(this.meeting.endsAt).getTime();
    const durationMs = end - start;

    if (durationMs <= 0) return '';

    const durationSec = Math.floor(durationMs / 1000);
    const minutes = Math.floor(durationSec / 60);
    const seconds = durationSec % 60;

    if (minutes > 0) {
      return `${minutes}m ${seconds}s`;
    } else {
      return `${seconds}s`;
    }
  }

  goBack() {
    this.router.navigate(['/meetings']);
  }

  getStatusColor(status: string): string {
    switch (status) {
      case 'scheduled': return 'primary';
      case 'processing': return 'accent';
      case 'ready': return 'primary';
      case 'failed': return 'warn';
      default: return 'basic';
    }
  }

  getPlatformIcon(platform: string): string {
    switch (platform.toLowerCase()) {
      case 'google meet': return 'video_call';
      case 'zoom': return 'videocam';
      case 'teams': return 'groups';
      case 'webex': return 'meeting_room';
      case 'linkedin': return 'work';
      case 'facebook': return 'facebook';
      default: return 'event';
    }
  }

  togglePlatform(platform: string) {
    const index = this.selectedPlatforms.indexOf(platform);
    if (index > -1) {
      this.selectedPlatforms.splice(index, 1);
    } else {
      this.selectedPlatforms.push(platform);
    }
  }

  getAutomationIcon(automationName: string): string {
    switch (automationName.toLowerCase()) {
      case 'meeting summary': return 'summarize';
      case 'action items': return 'assignment_turned_in';
      case 'linkedin post': return 'work';
      case 'email follow-up': return 'email';
      default: return 'auto_awesome';
    }
  }

  async copyToClipboard(text?: string) {
    const contentToCopy = text || this.generatedContent;
    try {
      await navigator.clipboard.writeText(contentToCopy);
      this.snackBar.open('Content copied to clipboard', 'Close', { duration: 3000 });
    } catch (error) {
      console.error('Error copying to clipboard:', error);
      this.snackBar.open('Failed to copy to clipboard', 'Close', { duration: 3000 });
    }
  }

  async postToSocialMedia() {
    if (!this.generatedContent.trim()) {
      this.snackBar.open('No content to post', 'Close', { duration: 3000 });
      return;
    }

    this.isPosting = true;
    try {
      if (!this.meeting?.id) {
        this.snackBar.open('Meeting ID not found', 'Close', { duration: 3000 });
        return;
      }

      // Create social posts for selected platforms
      const platforms = this.selectedPlatforms.length > 0 ? this.selectedPlatforms : this.socialPlatforms;
      
      for (const platform of platforms) {
        try {
          const response = await this.meetingService.createSocialPost(
            this.meeting.id,
            platform.toLowerCase(),
            this.generatedContent
          ).toPromise();
          
          if (response?.success) {
            // Reload social posts to show the new post
            await this.loadSocialPosts();
          }
        } catch (error) {
          console.error(`Error creating ${platform} post:`, error);
        }
      }
      
      const platformText = platforms.join(' and ');
      this.snackBar.open(`Social posts created for ${platformText}!`, 'Close', { duration: 5000 });
      
      // Clear the generated content after successful posting
      this.generatedContent = '';
      this.selectedPlatforms = [];
      
    } catch (error) {
      console.error('Error posting to social media:', error);
      this.snackBar.open('Failed to post to social media', 'Close', { duration: 3000 });
    } finally {
      this.isPosting = false;
    }
  }

  async loadSocialPosts() {
    if (!this.meeting?.id) return;
    
    try {
      // First try to get social posts from the meeting data if available
      if (this.meeting.socialPosts && this.meeting.socialPosts.length > 0) {
        this.socialPosts = this.meeting.socialPosts;
        return;
      }
      
      // If not available in meeting data, try API call
      const response = await this.meetingService.getSocialPosts(this.meeting.id).toPromise();
      
      if (response?.success) {
        this.socialPosts = response.data || [];
      } else {
        // Use sample data as fallback
        this.socialPosts = this.getSampleSocialPosts();
      }
    } catch (error) {
      console.error('Error loading social posts:', error);
      // Use sample data as fallback
      this.socialPosts = this.getSampleSocialPosts();
    }
  }

  async postToSocial(socialPostId: number) {
    this.isPosting = true;
    try {
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 2000));
      
      // Find and update the post
      const post = this.socialPosts.find(p => p.id === socialPostId);
      if (post) {
        post.status = 'posted';
        post.postedAt = new Date().toISOString();
        post.error = null;
        
        this.snackBar.open('Post published successfully!', 'Close', { duration: 3000 });
      } else {
        this.snackBar.open('Post not found', 'Close', { duration: 3000 });
      }
    } catch (error) {
      console.error('Error posting to social:', error);
      this.snackBar.open('Failed to publish post', 'Close', { duration: 3000 });
    } finally {
      this.isPosting = false;
    }
  }

  async retryFailedPost(socialPostId: number) {
    this.isPosting = true;
    try {
      // Simulate retry API call
      await new Promise(resolve => setTimeout(resolve, 1500));
      
      // Find and update the post
      const post = this.socialPosts.find(p => p.id === socialPostId);
      if (post) {
        post.status = 'posted';
        post.postedAt = new Date().toISOString();
        post.error = null;
        
        this.snackBar.open('Post retried and published successfully!', 'Close', { duration: 3000 });
      } else {
        this.snackBar.open('Post not found', 'Close', { duration: 3000 });
      }
    } catch (error) {
      console.error('Error retrying post:', error);
      this.snackBar.open('Failed to retry post', 'Close', { duration: 3000 });
    } finally {
      this.isPosting = false;
    }
  }

  async schedulePost(socialPostId: number) {
    this.isPosting = true;
    try {
      // Simulate scheduling API call
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      // Find and update the post
      const post = this.socialPosts.find(p => p.id === socialPostId);
      if (post) {
        post.status = 'scheduled';
        post.scheduledFor = new Date(Date.now() + 86400000).toISOString(); // Schedule for tomorrow
        post.error = null;
        
        this.snackBar.open('Post scheduled successfully!', 'Close', { duration: 3000 });
      } else {
        this.snackBar.open('Post not found', 'Close', { duration: 3000 });
      }
    } catch (error) {
      console.error('Error scheduling post:', error);
      this.snackBar.open('Failed to schedule post', 'Close', { duration: 3000 });
    } finally {
      this.isPosting = false;
    }
  }

  getPostStatusColor(status: string): string {
    switch (status) {
      case 'posted': return 'primary';
      case 'draft': return 'accent';
      case 'scheduled': return 'warn';
      case 'failed': return 'warn';
      default: return 'basic';
    }
  }

  getAutomationName(automationId: number): string {
    const automation = this.automations.find(a => a.id === automationId);
    return automation ? automation.name : 'Unknown Automation';
  }

  async generateSocialPosts() {
    if (!this.meeting?.id) return;
    
    this.isPosting = true;
    try {
      // Simulate API call for generating social posts
      await new Promise(resolve => setTimeout(resolve, 3000));
      
      // Generate new social posts using active automations
      const activeSocialAutomations = this.automations.filter(a => 
        a.isActive && (a.name.includes('Post') || a.name.includes('Social'))
      );
      
      if (activeSocialAutomations.length === 0) {
        this.snackBar.open('No active social media automations found', 'Close', { duration: 3000 });
        return;
      }
      
      // Generate posts for each active social automation
      const newPosts = activeSocialAutomations.map((automation, index) => {
        const meetingTitle = this.meeting?.title || 'Team Meeting';
        const attendeeCount = this.meeting?.attendees?.length || 0;
        
        return {
          id: Date.now() + index,
          platform: this.getPlatformFromAutomation(automation.name),
          postText: this.generatePostText(automation.name, meetingTitle, attendeeCount),
          status: 'draft',
          createdAt: new Date().toISOString(),
          postedAt: null,
          error: null,
          automation: automation.name,
          automationId: automation.id,
          engagement: {
            likes: 0,
            comments: 0,
            shares: 0
          }
        };
      });
      
      // Add new posts to existing ones
      this.socialPosts = [...newPosts, ...this.socialPosts];
      
      this.snackBar.open(`Generated ${newPosts.length} social media posts!`, 'Close', { duration: 3000 });
    } catch (error) {
      console.error('Error generating social posts:', error);
      this.snackBar.open('Failed to generate social posts', 'Close', { duration: 3000 });
    } finally {
      this.isPosting = false;
    }
  }

  private getPlatformFromAutomation(automationName: string): string {
    if (automationName.toLowerCase().includes('linkedin')) return 'linkedin';
    if (automationName.toLowerCase().includes('twitter')) return 'twitter';
    if (automationName.toLowerCase().includes('facebook')) return 'facebook';
    if (automationName.toLowerCase().includes('instagram')) return 'instagram';
    return 'linkedin'; // default
  }

  private generatePostText(automationName: string, meetingTitle: string, attendeeCount: number): string {
    const templates: { [key: string]: string } = {
      'LinkedIn Post': `🚀 Just wrapped up ${meetingTitle}! Key highlights: Project milestones achieved, team collaboration was outstanding with ${attendeeCount} participants. Excited about our next phase! #TeamWork #ProjectSuccess #${meetingTitle.replace(/\s+/g, '')}`,
      'Twitter Post': `✅ ${meetingTitle} complete! Great progress with our team of ${attendeeCount}. Next milestone in sight! #TeamWork #Success #Agile`,
      'Facebook Post': `Team update: ${meetingTitle} was a huge success! Our ${attendeeCount} team members delivered excellent results. Proud of our collaborative spirit! #TeamWork #ProjectUpdate`,
      'Instagram Post': `✨ Behind the scenes of ${meetingTitle}! Our amazing team of ${attendeeCount} people just crushed another productive session! 💪 #TeamWork #BehindTheScenes #Productivity`
    };
    
    return templates[automationName] || templates['LinkedIn Post'];
  }

  async copyPostToClipboard(postText: string) {
    try {
      await navigator.clipboard.writeText(postText);
      this.snackBar.open('Post text copied to clipboard', 'Close', { duration: 3000 });
    } catch (error) {
      console.error('Error copying to clipboard:', error);
      this.snackBar.open('Failed to copy to clipboard', 'Close', { duration: 3000 });
    }
  }


  scrollToGenerateContent() {
    // Switch to the Generate Content tab
    this.selectedTab = 1; // Assuming Generate Content is the second tab
  }

  async loadFollowUpEmail() {
    if (!this.meeting?.id) return;
    
    try {
      // Check if there's a generated content of type 'email' for this meeting
      if (this.meeting.generatedContents) {
        const emailContent = this.meeting.generatedContents.find((content: GeneratedContent) => 
          content.automationName?.toLowerCase().includes('email') || 
          content.platform?.toLowerCase().includes('email')
        );
        
        if (emailContent) {
          this.followUpEmail = {
            id: emailContent.id,
            subject: `Follow-up: ${this.meeting.title}`,
            content: emailContent.outputText,
            createdAt: emailContent.createdAt
          };
          return;
        }
      }
      
      // If no email content found, generate a mock follow-up email based on meeting data
      this.followUpEmail = this.generateMockFollowUpEmail();
    } catch (error) {
      console.error('Error loading follow-up email:', error);
      this.followUpEmail = this.generateMockFollowUpEmail();
    }
  }

  async sendFollowUpEmail() {
    if (!this.followUpEmail) return;
    
    this.isSendingEmail = true;
    try {
      // TODO: Replace with actual API call when backend is ready
      // const response = await this.meetingService.sendFollowUpEmail(this.meeting.id, this.followUpEmail).toPromise();
      
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 2000));
      
      // Update email status to sent
      this.followUpEmail.status = 'sent';
      this.followUpEmail.sentAt = new Date().toISOString();
      
      this.snackBar.open(`Follow-up email sent successfully to ${this.followUpEmail.recipients?.length || 0} recipients!`, 'Close', { duration: 5000 });
    } catch (error) {
      console.error('Error sending follow-up email:', error);
      this.snackBar.open('Failed to send follow-up email', 'Close', { duration: 3000 });
    } finally {
      this.isSendingEmail = false;
    }
  }

  async generateFollowUpEmail() {
    if (!this.meeting?.id) return;
    
    this.isSendingEmail = true;
    try {
      // Simulate API call for generation
      await new Promise(resolve => setTimeout(resolve, 2000));
      
      // Generate a new mock email
      this.followUpEmail = this.generateMockFollowUpEmail();
      
      this.snackBar.open('Follow-up email generated successfully!', 'Close', { duration: 3000 });
    } catch (error) {
      console.error('Error generating follow-up email:', error);
      this.snackBar.open('Failed to generate follow-up email', 'Close', { duration: 3000 });
    } finally {
      this.isSendingEmail = false;
    }
  }

  async regenerateFollowUpEmail() {
    if (!this.meeting?.id) return;
    
    this.isSendingEmail = true;
    try {
      // Simulate API call for regeneration
      await new Promise(resolve => setTimeout(resolve, 1500));
      
      // Generate a new mock email
      this.followUpEmail = this.generateMockFollowUpEmail();
      
      this.snackBar.open('Follow-up email regenerated successfully!', 'Close', { duration: 3000 });
    } catch (error) {
      console.error('Error regenerating follow-up email:', error);
      this.snackBar.open('Failed to regenerate follow-up email', 'Close', { duration: 3000 });
    } finally {
      this.isSendingEmail = false;
    }
  }

  async previewFollowUpEmail() {
    if (!this.followUpEmail) return;
    
    // Open email in a new window for preview
    const emailWindow = window.open('', '_blank', 'width=800,height=600,scrollbars=yes,resizable=yes');
    if (emailWindow) {
      emailWindow.document.write(`
        <html>
          <head>
            <title>Email Preview - ${this.followUpEmail.subject}</title>
            <style>
              body { font-family: Arial, sans-serif; margin: 20px; line-height: 1.6; }
              .email-header { border-bottom: 2px solid #007bff; padding-bottom: 10px; margin-bottom: 20px; }
              .email-content { white-space: pre-wrap; }
              .email-footer { margin-top: 30px; padding-top: 20px; border-top: 1px solid #ccc; font-size: 0.9em; color: #666; }
            </style>
          </head>
          <body>
            <div class="email-header">
              <h2>${this.followUpEmail.subject}</h2>
              <p><strong>To:</strong> ${this.followUpEmail.recipients?.join(', ') || 'Team'}</p>
              <p><strong>From:</strong> Project Manager &lt;manager@company.com&gt;</p>
              <p><strong>Date:</strong> ${new Date().toLocaleString()}</p>
            </div>
            <div class="email-content">${this.followUpEmail.content}</div>
            <div class="email-footer">
              <p>This is a preview of the follow-up email that would be sent to meeting participants.</p>
            </div>
          </body>
        </html>
      `);
      emailWindow.document.close();
    }
  }

  private generateMockFollowUpEmail(): any {
    const meetingTitle = this.meeting?.title || 'Meeting';
    const meetingDate = this.meeting?.startsAt ? new Date(this.meeting.startsAt).toLocaleDateString() : new Date().toLocaleDateString();
    const attendees = this.meeting?.attendees || [];
    const attendeeNames = attendees.length > 0 ? attendees.join(', ') : 'Team';
    
    // Generate different email templates based on meeting type or content
    const emailTemplates = this.getEmailTemplates();
    const selectedTemplate = emailTemplates[Math.floor(Math.random() * emailTemplates.length)];
    
    return {
      id: Math.floor(Math.random() * 1000) + 1,
      subject: selectedTemplate.subject.replace('{meetingTitle}', meetingTitle),
      content: selectedTemplate.content
        .replace(/{meetingTitle}/g, meetingTitle)
        .replace(/{meetingDate}/g, meetingDate)
        .replace(/{attendees}/g, attendeeNames)
        .replace(/{attendeeCount}/g, attendees.length.toString()),
      createdAt: new Date().toISOString(),
      status: 'draft',
      recipients: attendees
    };
  }

  private getEmailTemplates(): any[] {
    return [
      {
        subject: 'Follow-up: {meetingTitle} - Action Items & Next Steps',
        content: `Dear {attendees},

Thank you for participating in today's meeting on {meetingDate}. Here's a summary of our discussion and the action items we've identified:

## Meeting Summary
- Project Alpha is on track for the Q1 deadline
- Beta testing phase has been completed successfully
- Client feedback has been received and reviewed
- Resource allocation for the upcoming sprint was discussed

## Key Decisions Made
- Approved additional budget for UI improvements
- Scheduled client demo for next Friday
- Assigned Sarah to lead the new feature development

## Action Items
1. **John Doe** - Complete API documentation by Friday
2. **Jane Smith** - Prepare client demo presentation
3. **Bob Wilson** - Review and approve UI mockups
4. **Team** - Daily standup at 9 AM starting tomorrow

## Next Steps
- Continue with current sprint priorities
- Prepare for client demo next week
- Review and implement feedback from beta testing

Please let me know if you have any questions or need clarification on any of the action items.

Best regards,
John Doe
Project Manager`
      },
      {
        subject: 'Meeting Recap: {meetingTitle} - Summary & Decisions',
        content: `Hi {attendees},

I hope this email finds you well. Following our productive meeting on {meetingDate}, I wanted to share a comprehensive recap of our discussion.

## What We Covered
- Strategic planning for the upcoming quarter
- Budget allocation and resource management
- Timeline adjustments and milestone reviews
- Team collaboration and communication protocols

## Key Outcomes
✅ **Budget Approved**: Additional $50K allocated for infrastructure improvements
✅ **Timeline Confirmed**: Project delivery remains on track for March 2025
✅ **Team Structure**: New roles and responsibilities clearly defined
✅ **Next Review**: Scheduled for next Friday at 2:00 PM

## Immediate Action Items
- **Sarah**: Complete the technical specification document by EOW
- **Mike**: Coordinate with external vendors for equipment procurement
- **Lisa**: Prepare the quarterly presentation for stakeholders
- **All**: Review and provide feedback on the new process documentation

## Questions or Concerns?
If you have any questions about the decisions made or need clarification on your action items, please don't hesitate to reach out.

Looking forward to our continued collaboration!

Best regards,
Project Lead`
      },
      {
        subject: 'Post-Meeting Summary: {meetingTitle} - Updates & Follow-ups',
        content: `Dear Team,

Thank you for your active participation in today's {meetingTitle} meeting. Here's a detailed summary of our discussion and next steps.

## Meeting Highlights
- **{attendeeCount} participants** attended the session
- **Duration**: 45 minutes of productive discussion
- **Platform**: {meetingTitle} via Zoom
- **Date**: {meetingDate}

## Discussion Points
1. **Project Status Update**: All major milestones are on track
2. **Resource Planning**: Team capacity and workload distribution
3. **Risk Assessment**: Identified potential challenges and mitigation strategies
4. **Stakeholder Communication**: Updates on client expectations and deliverables

## Decisions Made
- **Budget Allocation**: Approved additional funding for Q2 initiatives
- **Team Expansion**: Hiring freeze lifted, 2 new positions to be filled
- **Process Improvement**: New agile methodology to be implemented
- **Client Engagement**: Monthly check-ins scheduled with key stakeholders

## Action Items & Deadlines
| Task | Owner | Deadline | Priority |
|------|-------|----------|----------|
| Complete user research | Sarah | March 15 | High |
| Finalize technical specs | Mike | March 20 | High |
| Prepare client presentation | Lisa | March 25 | Medium |
| Update project documentation | All | March 30 | Medium |

## Next Steps
- Individual check-ins scheduled for next week
- Team standup continues daily at 9:00 AM
- Monthly review meeting scheduled for April 1st

## Resources & Links
- Project Dashboard: [Internal Link]
- Meeting Recording: [Available in 24 hours]
- Shared Documents: [Team Drive]

Please confirm receipt of this email and let me know if you need any clarification on your assigned tasks.

Best regards,
Project Manager`
      },
      {
        subject: 'Quick Recap: {meetingTitle} - Key Points & Next Actions',
        content: `Hi {attendees},

Quick recap from our {meetingTitle} meeting on {meetingDate}:

## 🎯 Main Takeaways
- Project is 85% complete and on schedule
- Client feedback has been overwhelmingly positive
- Budget is within 5% of original estimates
- Team morale and engagement are at an all-time high

## ✅ What's Working Well
- Daily standups are effective and well-attended
- Communication channels are clear and responsive
- Quality metrics exceed expectations
- Client satisfaction scores are above target

## 🚀 Immediate Next Steps
1. **This Week**: Complete the final testing phase
2. **Next Week**: Begin user acceptance testing
3. **Following Week**: Prepare for production deployment
4. **Month End**: Celebrate team achievements! 🎉

## 📋 Action Items
- **Alex**: Finalize the deployment checklist
- **Jordan**: Coordinate with the QA team
- **Sam**: Prepare the go-live communication
- **Taylor**: Schedule the retrospective meeting

## 💡 Questions?
If anything is unclear or you need support, just ping me directly.

Great work, team! Let's finish strong! 💪

Cheers,
Team Lead`
      }
    ];
  }

  private getSampleFollowUpEmail(): any {
    return this.generateMockFollowUpEmail();
  }

  private getSampleSocialPosts(): any[] {
    const meetingTitle = this.meeting?.title || 'Team Meeting';
    const meetingDate = this.meeting?.startsAt ? new Date(this.meeting.startsAt).toLocaleDateString() : new Date().toLocaleDateString();
    const attendees = this.meeting?.attendees || [];
    const attendeeCount = attendees.length;
    
    return [
      {
        id: 1,
        platform: 'linkedin',
        postText: `Just wrapped up an amazing ${meetingTitle}! 🚀 Key highlights: Project Alpha is on track for Q1, beta testing completed successfully, and we're excited about the new client demo next Friday. Great collaboration with ${attendeeCount} team members! #TeamWork #ProjectManagement #Innovation #${meetingTitle.replace(/\s+/g, '')}`,
        status: 'draft',
        createdAt: new Date().toISOString(),
        postedAt: null,
        error: null,
        automation: 'LinkedIn Post',
        automationId: 3,
        engagement: {
          likes: 0,
          comments: 0,
          shares: 0
        }
      },
      {
        id: 2,
        platform: 'twitter',
        postText: `🚀 Team standup complete! ${meetingTitle} delivered great results. Q1 goals on track, beta testing ✅, client demo next week. Proud of our ${attendeeCount} team members! #TeamWork #Agile #Success`,
        status: 'posted',
        createdAt: new Date(Date.now() - 86400000).toISOString(),
        postedAt: new Date(Date.now() - 3600000).toISOString(),
        error: null,
        automation: 'Twitter Post',
        automationId: 4,
        engagement: {
          likes: 12,
          comments: 3,
          shares: 2
        }
      },
      {
        id: 3,
        platform: 'facebook',
        postText: `Great team meeting today! We discussed our progress on ${meetingTitle} and made some important decisions about the upcoming sprint. The collaboration between our ${attendeeCount} team members was fantastic. Looking forward to the client demo next week! #TeamWork #ProjectSuccess`,
        status: 'posted',
        createdAt: new Date(Date.now() - 172800000).toISOString(),
        postedAt: new Date(Date.now() - 7200000).toISOString(),
        error: null,
        automation: 'Facebook Post',
        automationId: 5,
        engagement: {
          likes: 8,
          comments: 1,
          shares: 0
        }
      },
      {
        id: 4,
        platform: 'instagram',
        postText: `✨ Behind the scenes of our ${meetingTitle}! Our team of ${attendeeCount} amazing people just crushed another productive session. Q1 goals are looking strong! 💪 #TeamWork #BehindTheScenes #Productivity #${meetingTitle.replace(/\s+/g, '')}`,
        status: 'draft',
        createdAt: new Date(Date.now() - 259200000).toISOString(),
        postedAt: null,
        error: null,
        automation: 'Instagram Post',
        automationId: 6,
        engagement: {
          likes: 0,
          comments: 0,
          shares: 0
        }
      },
      {
        id: 5,
        platform: 'linkedin',
        postText: `📊 Meeting insights from ${meetingTitle}: Our team of ${attendeeCount} professionals discussed key project milestones. Beta testing phase completed with flying colors! Next up: Client presentation. The power of collaborative planning! #ProjectManagement #TeamSuccess #Innovation`,
        status: 'scheduled',
        createdAt: new Date(Date.now() - 345600000).toISOString(),
        postedAt: null,
        scheduledFor: new Date(Date.now() + 86400000).toISOString(),
        error: null,
        automation: 'LinkedIn Post',
        automationId: 3,
        engagement: {
          likes: 0,
          comments: 0,
          shares: 0
        }
      },
      {
        id: 6,
        platform: 'facebook',
        postText: `Team update: ${meetingTitle} was a huge success! We're making great progress on our Q1 objectives. Beta testing is complete and we're ready for the next phase. Thanks to all ${attendeeCount} team members for their dedication! #TeamWork #ProjectUpdate #Success`,
        status: 'failed',
        createdAt: new Date(Date.now() - 432000000).toISOString(),
        postedAt: null,
        error: 'API rate limit exceeded. Retry in 15 minutes.',
        automation: 'Facebook Post',
        automationId: 5,
        engagement: {
          likes: 0,
          comments: 0,
          shares: 0
        }
      }
    ];
  }
}