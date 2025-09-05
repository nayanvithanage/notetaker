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
  status: 'scheduled' | 'ready' | 'processing' | 'failed';
  calendarEventId: string;
  createdAt: string;
  updatedAt: string;
}

export interface MeetingDetail extends Meeting {
  transcript?: string;
  summary?: string;
  actionItems?: string[];
  generatedContent?: GeneratedContent[];
}

export interface GeneratedContent {
  id: number;
  type: 'summary' | 'action_items' | 'social_post' | 'email';
  content: string;
  automationId?: number;
  createdAt: string;
}