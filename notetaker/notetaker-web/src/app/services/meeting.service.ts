import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { Meeting, MeetingDetail } from '../models/meeting.model';
import { ApiResponse } from '../models/api-response.model';

@Injectable({
  providedIn: 'root'
})
export class MeetingService {
  constructor(private apiService: ApiService) {}

  getMeetings(type: 'upcoming' | 'past'): Observable<ApiResponse<Meeting[]>> {
    return this.apiService.get<Meeting[]>(`meetings?type=${type}`);
  }

  getCalendarEvents(from?: Date, to?: Date): Observable<ApiResponse<any[]>> {
    const params: any = {};
    if (from) params.from = from.toISOString();
    if (to) params.to = to.toISOString();
    
    return this.apiService.get<any[]>('calendar/events', params);
  }

  connectGoogleCalendar(code: string, state: string): Observable<ApiResponse<any>> {
    return this.apiService.post('calendar/google/connect', {
      code,
      state
    });
  }

  syncCalendarEvents(calendarAccountId: number): Observable<ApiResponse<any>> {
    return this.apiService.post('calendar/sync', {
      calendarAccountId
    });
  }

  getMeeting(id: number): Observable<ApiResponse<MeetingDetail>> {
    return this.apiService.get<MeetingDetail>(`meetings/${id}`);
  }

  fetchTranscript(meetingId: number): Observable<ApiResponse<any>> {
    return this.apiService.post<any>(`meetings/${meetingId}/transcript:fetch`, {});
  }

  findExistingBots(meetingId: number): Observable<ApiResponse<any>> {
    return this.apiService.post<any>(`meetings/${meetingId}/bots:find`, {});
  }

  findExistingBotsForCalendarEvent(calendarEventId: number): Observable<ApiResponse<any>> {
    return this.apiService.post<any>(`calendar/events/${calendarEventId}/bots:find`, {});
  }

  toggleNotetaker(calendarEventId: number, enabled: boolean): Observable<ApiResponse<any>> {
    return this.apiService.post(`calendar/events/${calendarEventId}/notetaker:toggle`, {
      Enabled: enabled
    });
  }

  generateContent(meetingId: number, automationId?: number): Observable<ApiResponse<any>> {
    return this.apiService.post(`meetings/${meetingId}/generate`, {
      AutomationId: automationId || 0
    });
  }

  deleteMeeting(id: number): Observable<ApiResponse<any>> {
    return this.apiService.delete(`meetings/${id}`);
  }

  getBotStatus(meetingId: number): Observable<ApiResponse<any>> {
    return this.apiService.get(`calendar/bot-status/${meetingId}`);
  }

  getBotSettings(): Observable<ApiResponse<any>> {
    return this.apiService.get('calendar/bot-settings');
  }

  updateBotSettings(settings: any): Observable<ApiResponse<any>> {
    return this.apiService.post('calendar/bot-settings', settings);
  }

  // Social Posts methods
  getSocialPosts(meetingId?: number): Observable<ApiResponse<any[]>> {
    const params = meetingId ? { meetingId } : {};
    return this.apiService.get<any[]>('meetings/social-posts', params);
  }

  createSocialPost(meetingId: number, platform: string, postText: string, targetId?: string): Observable<ApiResponse<any>> {
    return this.apiService.post(`meetings/${meetingId}/social-posts`, {
      platform,
      postText,
      targetId
    });
  }

  postToSocial(socialPostId: number): Observable<ApiResponse<any>> {
    return this.apiService.post(`meetings/social-posts/${socialPostId}/post`, {});
  }

  getTranscriptByBotId(botId: string): Observable<ApiResponse<string>> {
    return this.apiService.get<string>(`meetings/transcript/${botId}`);
  }

  fetchTranscriptByBotId(botId: string): Observable<ApiResponse<any>> {
    return this.apiService.post(`meetings/transcript:fetch-by-bot`, { botId });
  }

  getLatestBotDetails(meetingId: number): Observable<ApiResponse<any>> {
    return this.apiService.get(`meetings/${meetingId}/bot:latest`);
  }

  reSyncMeetingBot(meetingId: number): Observable<ApiResponse<any>> {
    return this.apiService.post(`meetings/${meetingId}/bot:resync`, {});
  }

  deltaSyncBots(): Observable<ApiResponse<any>> {
    return this.apiService.post('calendar/bots:delta-sync', {});
  }
}