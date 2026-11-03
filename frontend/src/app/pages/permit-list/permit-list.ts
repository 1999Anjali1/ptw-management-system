import { DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PermitService, errorMessages } from '../../core/api.services';
import { AuthService } from '../../core/auth.service';
import {
  PERMIT_STATUSES,
  PERMIT_TYPES,
  PagedResult,
  PermitQuery,
  PermitStatus,
  PermitSummary,
  PermitType,
} from '../../core/models';
import { STATUS_LABELS, TYPE_LABELS } from '../../shared/labels';
import { StatusBadge } from '../../shared/status-badge';

@Component({
  selector: 'app-permit-list',
  imports: [FormsModule, RouterLink, DatePipe, StatusBadge],
  templateUrl: './permit-list.html',
})
export class PermitList implements OnInit {
  private readonly api = inject(PermitService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly auth = inject(AuthService);

  protected readonly statuses = PERMIT_STATUSES;
  protected readonly types = PERMIT_TYPES;
  protected readonly statusLabels = STATUS_LABELS;
  protected readonly typeLabels = TYPE_LABELS;

  protected readonly query = signal<PermitQuery>({ page: 1, pageSize: 10 });
  protected readonly result = signal<PagedResult<PermitSummary> | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected search = '';

  protected readonly totalPages = computed(() => {
    const r = this.result();
    return r ? Math.max(1, Math.ceil(r.totalCount / r.pageSize)) : 1;
  });

  ngOnInit(): void {
    // The URL is the source of truth for filters, so filtered lists can be bookmarked / linked from the dashboard
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const q: PermitQuery = {
        status: (params.get('status') as PermitStatus) ?? '',
        type: (params.get('type') as PermitType) ?? '',
        search: params.get('search') ?? '',
        mine: params.get('mine') === 'true',
        page: Number(params.get('page') ?? 1),
        pageSize: 10,
      };
      this.search = q.search ?? '';
      this.query.set(q);
      this.load();
    });
  }

  applyFilter(changes: Partial<PermitQuery>): void {
    const next = { ...this.query(), ...changes };
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        status: next.status || null,
        type: next.type || null,
        search: next.search?.trim() || null,
        mine: next.mine || null,
        page: next.page > 1 ? next.page : null,
      },
    });
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.list(this.query()).subscribe({
      next: (r) => {
        this.result.set(r);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(errorMessages(err)[0]);
        this.loading.set(false);
      },
    });
  }
}
