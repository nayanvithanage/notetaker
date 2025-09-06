import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/api-response.model';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private baseUrl = environment.apiBaseUrl;

  constructor(private http: HttpClient) {
    console.log('ApiService constructor - environment.apiBaseUrl:', environment.apiBaseUrl);
    console.log('ApiService constructor - this.baseUrl:', this.baseUrl);
  }

  private getHeaders(): HttpHeaders {
    const token = localStorage.getItem('accessToken');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      ...(token && { 'Authorization': `Bearer ${token}` })
    });
  }

  get<T>(endpoint: string, params?: any): Observable<ApiResponse<T>> {
    let httpParams = new HttpParams();
    if (params) {
      Object.keys(params).forEach(key => {
        if (params[key] !== null && params[key] !== undefined) {
          httpParams = httpParams.set(key, params[key].toString());
        }
      });
    }

    // Construct the URL properly
    const url = endpoint.startsWith('http') ? endpoint : `${this.baseUrl}/${endpoint}`;
    
    console.log('ApiService GET - baseUrl:', this.baseUrl, 'endpoint:', endpoint, 'final URL:', url);
    return this.http.get<ApiResponse<T>>(url, {
      headers: this.getHeaders(),
      params: httpParams
    }).pipe(
      map(response => ({
        ...response,
        errors: response.errors || []
      }))
    );
  }

  post<T>(endpoint: string, data: any): Observable<ApiResponse<T>> {
    const url = endpoint.startsWith('http') ? endpoint : `${this.baseUrl}/${endpoint}`;
    return this.http.post<ApiResponse<T>>(url, data, {
      headers: this.getHeaders()
    }).pipe(
      map(response => ({
        ...response,
        errors: response.errors || []
      }))
    );
  }

  put<T>(endpoint: string, data: any): Observable<ApiResponse<T>> {
    const url = endpoint.startsWith('http') ? endpoint : `${this.baseUrl}/${endpoint}`;
    return this.http.put<ApiResponse<T>>(url, data, {
      headers: this.getHeaders()
    }).pipe(
      map(response => ({
        ...response,
        errors: response.errors || []
      }))
    );
  }

  delete<T>(endpoint: string): Observable<ApiResponse<T>> {
    const url = endpoint.startsWith('http') ? endpoint : `${this.baseUrl}/${endpoint}`;
    return this.http.delete<ApiResponse<T>>(url, {
      headers: this.getHeaders()
    }).pipe(
      map(response => ({
        ...response,
        errors: response.errors || []
      }))
    );
  }
}