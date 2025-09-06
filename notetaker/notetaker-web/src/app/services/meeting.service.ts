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
    return this.apiService.get<Meeting[]>(`/meetings?type=${type}`);
  }

  getCalendarEvents(from?: Date, to?: Date): Observable<ApiResponse<any[]>> {
    const params: any = {};
    if (from) params.from = from.toISOString();
    if (to) params.to = to.toISOString();
    
    return this.apiService.get<any[]>('/calendar/events', params);
  }

  connectGoogleCalendar(code: string, state: string): Observable<ApiResponse<any>> {
    return this.apiService.post('/calendar/google/connect', {
      code,
      state
    });
  }

  syncCalendarEvents(calendarAccountId: number): Observable<ApiResponse<any>> {
    return this.apiService.post('/calendar/sync', {
      calendarAccountId
    });
  }

  getMeeting(id: number): Observable<ApiResponse<MeetingDetail>> {
    return this.apiService.get<MeetingDetail>(`/meetings/${id}`);
  }

  toggleNotetaker(calendarEventId: number, enabled: boolean): Observable<ApiResponse<any>> {
    return this.apiService.post(`/calendar/events/${calendarEventId}/notetaker:toggle`, {
      enabled
    });
  }

  generateContent(meetingId: number, automationId?: number): Observable<ApiResponse<any>> {
    return this.apiService.post('/meetings/generate-content', {
      meetingId,
      automationId
    });
  }

  deleteMeeting(id: number): Observable<ApiResponse<any>> {
    return this.apiService.delete(`/meetings/${id}`);
  }
}