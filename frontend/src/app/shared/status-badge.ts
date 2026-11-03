import { Component, computed, input } from '@angular/core';
import { PermitStatus } from '../core/models';
import { STATUS_LABELS } from './labels';

@Component({
  selector: 'app-status-badge',
  template: `<span class="badge" [class]="'badge badge--' + status().toLowerCase()">{{ label() }}</span>`,
})
export class StatusBadge {
  readonly status = input.required<PermitStatus>();
  protected readonly label = computed(() => STATUS_LABELS[this.status()]);
}
