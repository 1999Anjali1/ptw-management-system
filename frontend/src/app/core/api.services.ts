import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  DashboardSummary,
  GasTestRequest,
  IsolationRequest,
  PagedResult,
  PermitAction,
  PermitConflict,
  PermitDetail,
  PermitQuery,
  PermitRequest,
  PermitSummary,
} from './models';

@Injectable({ providedIn: 'root' })
export class PermitService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/permits`;

  list(query: PermitQuery): Observable<PagedResult<PermitSummary>> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);
    if (query.status) params = params.set('status', query.status);
    if (query.type) params = params.set('type', query.type);
    if (query.search?.trim()) params = params.set('search', query.search.trim());
    if (query.mine) params = params.set('mine', true);
    return this.http.get<PagedResult<PermitSummary>>(this.base, { params });
  }

  get(id: number): Observable<PermitDetail> {
    return this.http.get<PermitDetail>(`${this.base}/${id}`);
  }

  create(body: PermitRequest): Observable<PermitDetail> {
    return this.http.post<PermitDetail>(this.base, body);
  }

  update(id: number, body: PermitRequest): Observable<PermitDetail> {
    return this.http.put<PermitDetail>(`${this.base}/${id}`, body);
  }

  act(id: number, action: PermitAction, comment?: string): Observable<PermitDetail> {
    return this.http.post<PermitDetail>(`${this.base}/${id}/actions`, { action, comment });
  }

  addGasTest(id: number, body: GasTestRequest): Observable<PermitDetail> {
    return this.http.post<PermitDetail>(`${this.base}/${id}/gas-tests`, body);
  }

  addIsolation(id: number, body: IsolationRequest): Observable<PermitDetail> {
    return this.http.post<PermitDetail>(`${this.base}/${id}/isolations`, body);
  }

  removeIsolation(id: number, isolationId: number): Observable<PermitDetail> {
    return this.http.post<PermitDetail>(`${this.base}/${id}/isolations/${isolationId}/remove`, {});
  }

  conflicts(id: number): Observable<PermitConflict[]> {
    return this.http.get<PermitConflict[]>(`${this.base}/${id}/conflicts`);
  }
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);

  summary(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${environment.apiUrl}/dashboard/summary`);
  }
}

/** Turns an API error (ASP.NET ProblemDetails / validation errors) into readable messages. */
export function errorMessages(err: unknown): string[] {
  if (!(err instanceof HttpErrorResponse)) return ['Unexpected error.'];
  if (err.status === 0) return ['Cannot reach the API. Is the .NET backend running on port 5089?'];

  const body = err.error;
  if (body?.errors) {
    // Our PTW rule errors: string[]; ASP.NET model validation: { field: string[] }
    if (Array.isArray(body.errors)) return body.errors;
    return Object.values(body.errors as Record<string, string[]>).flat();
  }
  return [body?.detail ?? body?.title ?? err.message];
}
