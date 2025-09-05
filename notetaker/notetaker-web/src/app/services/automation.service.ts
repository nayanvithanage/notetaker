import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { Automation, CreateAutomation, UpdateAutomation } from '../models/automation.model';

@Injectable({
  providedIn: 'root'
})
export class AutomationService {
  constructor(private apiService: ApiService) {}

  getAutomations(): Observable<ApiResponse<Automation[]>> {
    return this.apiService.get<Automation[]>('/automations');
  }

  getAutomation(id: number): Observable<ApiResponse<Automation>> {
    return this.apiService.get<Automation>(`/automations/${id}`);
  }

  createAutomation(automation: CreateAutomation): Observable<ApiResponse<Automation>> {
    return this.apiService.post<Automation>('/automations', automation);
  }

  updateAutomation(id: number, automation: UpdateAutomation): Observable<ApiResponse<Automation>> {
    return this.apiService.put<Automation>(`/automations/${id}`, automation);
  }

  deleteAutomation(id: number): Observable<ApiResponseVoid> {
    return this.apiService.delete<ApiResponseVoid>(`/automations/${id}`);
  }
}

interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  errors: string[];
}

interface ApiResponseVoid {
  success: boolean;
  message?: string;
  errors: string[];
}
