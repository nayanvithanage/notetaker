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
import { MeetingService } from '../../services/meeting.service';
import { AutomationService } from '../../services/automation.service';
import { Meeting } from '../../models/meeting.model';
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
    MatSelectModule
  ],
  standalone: true
})
export class MeetingDetailComponent implements OnInit {
  meeting: Meeting | null = null;
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

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private meetingService: MeetingService,
    private automationService: AutomationService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit() {
    this.loadMeeting();
    this.loadAutomations();
  }

  async loadMeeting() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.router.navigate(['/meetings']);
      return;
    }

    this.loading = true;
    try {
      // For now, use sample data until the API is fully connected
      this.meeting = this.getSampleMeeting(parseInt(id));
      
      // TODO: Replace with actual API call when backend is ready
      // const response = await this.meetingService.getMeetingById(parseInt(id)).toPromise();
      // this.meeting = response?.data || null;
    } catch (error) {
      console.error('Error loading meeting:', error);
      this.snackBar.open('Failed to load meeting details', 'Close', { duration: 3000 });
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

  private getSampleMeeting(id: number): Meeting {
    return {
      id: id,
      title: 'Weekly Team Standup',
      description: 'Discuss project progress, blockers, and upcoming tasks',
      startsAt: new Date(Date.now() + 2 * 60 * 60 * 1000).toISOString(), // 2 hours from now
      endsAt: new Date(Date.now() + 3 * 60 * 60 * 1000).toISOString(), // 3 hours from now
      platform: 'Google Meet',
      joinUrl: 'https://meet.google.com/abc-defg-hij',
      attendees: ['john.doe@company.com', 'jane.smith@company.com', 'bob.wilson@company.com'],
      notetakerEnabled: true,
      status: 'ready',
      calendarEventId: 'event_123',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString()
    };
  }

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

  async copyToClipboard() {
    try {
      await navigator.clipboard.writeText(this.generatedContent);
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
      // TODO: Replace with actual API call when backend is ready
      // const response = await this.meetingService.postToSocialMedia({
      //   content: this.generatedContent,
      //   platforms: this.selectedPlatforms,
      //   meetingId: this.meeting?.id
      // }).toPromise();
      
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 2000));
      
      // Simulate successful posting
      const platforms = this.selectedPlatforms.length > 0 ? this.selectedPlatforms : this.socialPlatforms;
      const platformText = platforms.join(' and ');
      
      this.snackBar.open(`Content posted successfully to ${platformText}!`, 'Close', { duration: 5000 });
      
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
}