import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { PermitService, errorMessages } from '../../core/api.services';
import { ISOLATION_TYPES, IsolationType, PermitAction, PermitConflict, PermitDetail } from '../../core/models';
import { ACTION_CONFIG, TYPE_LABELS } from '../../shared/labels';
import { StatusBadge } from '../../shared/status-badge';

@Component({
  selector: 'app-permit-detail',
  imports: [RouterLink, DatePipe, FormsModule, ReactiveFormsModule, StatusBadge],
  templateUrl: './permit-detail.html',
})
export class PermitDetailPage implements OnInit {
  private readonly api = inject(PermitService);
  private readonly fb = inject(FormBuilder).nonNullable;

  /** Route parameter (permits/:id) */
  readonly id = input.required<string>();

  protected readonly permit = signal<PermitDetail | null>(null);
  protected readonly conflicts = signal<PermitConflict[]>([]);
  protected readonly errors = signal<string[]>([]);
  protected readonly message = signal<string | null>(null);
  protected readonly busy = signal(false);

  /** Action waiting for a comment / confirmation. */
  protected readonly pendingAction = signal<PermitAction | null>(null);
  protected comment = '';

  protected readonly showGasForm = signal(false);
  protected readonly showIsolationForm = signal(false);

  protected readonly actionConfig = ACTION_CONFIG;
  protected readonly typeLabels = TYPE_LABELS;
  protected readonly isolationTypes = ISOLATION_TYPES;

  protected readonly gasForm = this.fb.group({
    oxygenPercent: [20.9, [Validators.required, Validators.min(0), Validators.max(100)]],
    lelPercent: [0, [Validators.required, Validators.min(0), Validators.max(100)]],
    h2sPpm: [0, [Validators.required, Validators.min(0)]],
    coPpm: [0, [Validators.required, Validators.min(0)]],
    remarks: [''],
  });

  protected readonly isolationForm = this.fb.group({
    type: ['Electrical' as IsolationType, Validators.required],
    isolationPoint: ['', [Validators.required, Validators.minLength(2)]],
    lockNumber: [''],
    description: [''],
  });

  protected readonly openIsolations = computed(() => this.permit()?.isolations.filter((i) => !i.isRemoved).length ?? 0);
  protected readonly latestGasTest = computed(() => this.permit()?.gasTests[0] ?? null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.api.get(+this.id()).subscribe({
      next: (p) => this.setPermit(p),
      error: (err) => this.errors.set(errorMessages(err)),
    });
  }

  // ---------- workflow ----------

  startAction(action: PermitAction): void {
    this.comment = '';
    this.errors.set([]);
    this.pendingAction.set(action);
  }

  confirmAction(): void {
    const action = this.pendingAction();
    const p = this.permit();
    if (!action || !p) return;

    if (ACTION_CONFIG[action].needsComment && !this.comment.trim()) {
      this.errors.set(['Please enter a reason.']);
      return;
    }
    this.run(this.api.act(p.id, action, this.comment.trim() || undefined), `${ACTION_CONFIG[action].label} - done.`);
  }

  // ---------- safety records ----------

  saveGasTest(): void {
    const p = this.permit();
    if (!p || this.gasForm.invalid) return;
    this.run(this.api.addGasTest(p.id, this.gasForm.getRawValue()), 'Gas test recorded.', () => {
      this.showGasForm.set(false);
      this.gasForm.reset();
    });
  }

  saveIsolation(): void {
    const p = this.permit();
    if (!p || this.isolationForm.invalid) {
      this.isolationForm.markAllAsTouched();
      return;
    }
    this.run(this.api.addIsolation(p.id, this.isolationForm.getRawValue()), 'Isolation applied.', () => {
      this.showIsolationForm.set(false);
      this.isolationForm.reset();
    });
  }

  removeIsolation(isolationId: number): void {
    const p = this.permit();
    if (!p || !confirm('Confirm the isolation has been physically removed and the lock returned?')) return;
    this.run(this.api.removeIsolation(p.id, isolationId), 'Isolation removed.');
  }

  // ---------- helpers ----------

  private run(request: Observable<PermitDetail>, successMessage: string, after?: () => void): void {
    this.busy.set(true);
    this.errors.set([]);
    this.message.set(null);
    request.subscribe({
      next: (p) => {
        this.setPermit(p);
        this.pendingAction.set(null);
        this.message.set(successMessage);
        this.busy.set(false);
        after?.();
      },
      error: (err) => {
        this.errors.set(errorMessages(err));
        this.busy.set(false);
      },
    });
  }

  private setPermit(p: PermitDetail): void {
    this.permit.set(p);
    if (['Approved', 'Active', 'Suspended', 'Submitted'].includes(p.status)) {
      this.api.conflicts(p.id).subscribe((c) => this.conflicts.set(c));
    } else {
      this.conflicts.set([]);
    }
  }
}
