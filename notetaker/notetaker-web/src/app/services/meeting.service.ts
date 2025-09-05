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

  getMeeting(id: number): Observable<ApiResponse<MeetingDetail>> {
    return this.apiService.get<MeetingDetail>(`/meetings/${id}`);
  }

  toggleNotetaker(calendarEventId: string, enabled: boolean): Observable<ApiResponse<any>> {
    return this.apiService.post('/meetings/toggle-notetaker', {
      calendarEventId,
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