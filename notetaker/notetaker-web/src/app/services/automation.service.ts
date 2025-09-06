import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { Automation, CreateAutomationRequest, UpdateAutomationRequest } from '../models/automation.model';
import { ApiResponse } from '../models/api-response.model';

@Injectable({
  providedIn: 'root'
})
export class AutomationService {
  constructor(private apiService: ApiService) {}

  getAutomations(): Observable<ApiResponse<Automation[]>> {
    return this.apiService.get<Automation[]>('automations');
  }

  getAutomation(id: number): Observable<ApiResponse<Automation>> {
    return this.apiService.get<Automation>(`automations/${id}`);
  }

  createAutomation(request: CreateAutomationRequest): Observable<ApiResponse<Automation>> {
    return this.apiService.post<Automation>('automations', request);
  }

  updateAutomation(id: number, request: UpdateAutomationRequest): Observable<ApiResponse<Automation>> {
    return this.apiService.put<Automation>(`automations/${id}`, request);
  }

  deleteAutomation(id: number): Observable<ApiResponse<any>> {
    return this.apiService.delete(`automations/${id}`);
  }

  toggleAutomation(id: number, isActive: boolean): Observable<ApiResponse<Automation>> {
    return this.apiService.put<Automation>(`automations/${id}/toggle`, { isActive });
  }
}