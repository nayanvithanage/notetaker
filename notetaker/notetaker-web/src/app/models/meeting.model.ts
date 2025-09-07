export interface Meeting {
  id: number;
  title: string;
  description?: string;
  startsAt: string;
  endsAt: string;
  platform: string;
  joinUrl?: string;
  attendees: string[];
  notetakerEnabled: boolean;
  status: 'scheduled' | 'ready' | 'processing' | 'failed' | 'recording' | 'cancelled';
  calendarEventId: string;
  recallBotId?: string;
  createdAt: string;
  updatedAt: string;
}

export interface MeetingDetail extends Meeting {
  transcriptText?: string;
  summaryJson?: string;
  mediaUrls?: string[];
  generatedContents?: GeneratedContent[];
  socialPosts?: SocialPost[];
}

export interface SocialPost {
  id: number;
  platform: string;
  postText: string;
  status: string;
  externalPostId?: string;
  postedAt?: string;
  error?: string;
  createdAt: string;
}

export interface GeneratedContent {
  id: number;
  automationId: number;
  automationName: string;
  platform: string;
  model: string;
  outputText: string;
  createdAt: string;
}