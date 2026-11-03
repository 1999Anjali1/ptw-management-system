import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DashboardService, errorMessages } from '../../core/api.services';
import { AuthService } from '../../core/auth.service';
import { DashboardSummary, PERMIT_TYPES } from '../../core/models';
import { TYPE_LABELS } from '../../shared/labels';
import { StatusBadge } from '../../shared/status-badge';

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, DatePipe, StatusBadge],
  templateUrl: './dashboard.html',
})
export class Dashboard implements OnInit {
  private readonly api = inject(DashboardService);
  protected readonly auth = inject(AuthService);

  protected readonly summary = signal<DashboardSummary | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly typeLabels = TYPE_LABELS;

  /** Permit types with a bar width relative to the largest count. */
  protected readonly typeBars = computed(() => {
    const s = this.summary();
    if (!s) return [];
    const max = Math.max(1, ...Object.values(s.byType));
    return PERMIT_TYPES.map((t) => ({
      type: t,
      label: TYPE_LABELS[t],
      count: s.byType[t] ?? 0,
      width: ((s.byType[t] ?? 0) / max) * 100,
    }));
  });

  /** What the logged-in role should look at first. */
  protected readonly focus = computed(() => {
    const s = this.summary();
    if (!s) return null;
    switch (this.auth.user()?.role) {
      case 'AreaAuthority':
        return { text: `${s.pendingApproval} permit(s) waiting for your approval`, status: 'Submitted' };
      case 'Issuer':
        return { text: `${s.readyToIssue} approved permit(s) ready to issue`, status: 'Approved' };
      case 'Requester':
        return { text: 'Raise a new permit or track your existing ones', status: null };
      default:
        return null;
    }
  });

  ngOnInit(): void {
    this.api.summary().subscribe({
      next: (s) => this.summary.set(s),
      error: (err) => this.error.set(errorMessages(err)[0]),
    });
  }
}
