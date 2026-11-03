import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { PermitService, errorMessages } from '../../core/api.services';
import { PERMIT_TYPES, PermitRequest, PermitType, RISK_LEVELS, RiskLevel } from '../../core/models';
import { TYPE_LABELS } from '../../shared/labels';

/** Suggested hazards / precautions / PPE per permit type - saves the requester typing. */
const TEMPLATES: Record<PermitType, { hazards: string; precautions: string; ppe: string }> = {
  HotWork: {
    hazards: 'Fire, sparks, hot surfaces, flammable atmosphere',
    precautions: 'Gas test before start, fire watch, fire extinguisher at site, drains covered, combustibles removed',
    ppe: 'FR coverall, welding helmet / face shield, leather gloves, safety shoes, hard hat',
  },
  ColdWork: {
    hazards: 'Manual handling, pinch points, slips and trips',
    precautions: 'Toolbox talk, area barricaded, correct tools inspected',
    ppe: 'Coverall, hard hat, safety glasses, gloves, safety shoes',
  },
  ConfinedSpaceEntry: {
    hazards: 'Oxygen deficiency, toxic / flammable gas, engulfment, restricted exit',
    precautions: 'Positive isolation, purge and ventilate, gas test, standby man, rescue plan, entry log',
    ppe: 'Coverall, full body harness, personal gas monitor, hard hat, safety shoes',
  },
  Electrical: {
    hazards: 'Electric shock, arc flash',
    precautions: 'Isolate and lock out, prove dead, arc flash boundary, authorised electrician only',
    ppe: 'Arc-rated coverall, insulated gloves, face shield, safety shoes',
  },
  WorkAtHeight: {
    hazards: 'Fall from height, dropped objects',
    precautions: 'Green-tagged scaffold, 100% tie-off, exclusion zone below, tool lanyards',
    ppe: 'Full body harness, hard hat with chin strap, safety shoes, gloves',
  },
  Excavation: {
    hazards: 'Underground services, cave-in, falling into excavation',
    precautions: 'Services drawing checked, cable detector survey, hand dig near services, shoring / battering, barricade',
    ppe: 'Coverall, hard hat, safety shoes, gloves, hi-vis vest',
  },
};

/** Form-level validator: end must be after start and within 24 hours. */
function validityWindow(group: AbstractControl): ValidationErrors | null {
  const start = new Date(group.get('plannedStart')?.value);
  const end = new Date(group.get('plannedEnd')?.value);
  if (isNaN(start.getTime()) || isNaN(end.getTime())) return null;
  if (end <= start) return { window: 'Planned end must be after planned start.' };
  if (end.getTime() - start.getTime() > 24 * 3600 * 1000) return { window: 'A permit cannot be valid for more than 24 hours.' };
  return null;
}

/** Date → value for <input type="datetime-local"> in the user's local time. */
function toLocalInput(d: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

@Component({
  selector: 'app-permit-form',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './permit-form.html',
})
export class PermitForm implements OnInit {
  private readonly api = inject(PermitService);
  private readonly router = inject(Router);

  /** Bound from the route (permits/:id/edit). Undefined when creating. */
  readonly id = input<string>();
  protected readonly isEdit = computed(() => !!this.id());

  protected readonly types = PERMIT_TYPES;
  protected readonly riskLevels = RISK_LEVELS;
  protected readonly typeLabels = TYPE_LABELS;

  protected readonly saving = signal(false);
  protected readonly errors = signal<string[]>([]);
  protected readonly permitNumber = signal<string | null>(null);

  protected readonly form = inject(FormBuilder).nonNullable.group(
    {
      type: ['HotWork' as PermitType, Validators.required],
      riskLevel: ['Medium' as RiskLevel, Validators.required],
      title: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(150)]],
      workDescription: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(2000)]],
      location: ['', [Validators.required, Validators.maxLength(150)]],
      equipmentTag: [''],
      workersCount: [2, [Validators.required, Validators.min(1), Validators.max(100)]],
      plannedStart: ['', Validators.required],
      plannedEnd: ['', Validators.required],
      hazards: ['', Validators.required],
      precautions: ['', Validators.required],
      requiredPpe: ['', Validators.required],
    },
    { validators: validityWindow },
  );

  ngOnInit(): void {
    const id = this.id();
    if (id) {
      this.api.get(+id).subscribe({
        next: (p) => {
          this.permitNumber.set(p.permitNumber);
          this.form.patchValue({
            ...p,
            equipmentTag: p.equipmentTag ?? '',
            plannedStart: toLocalInput(new Date(p.plannedStart)),
            plannedEnd: toLocalInput(new Date(p.plannedEnd)),
          });
        },
        error: (err) => this.errors.set(errorMessages(err)),
      });
    } else {
      const start = new Date();
      start.setHours(start.getHours() + 1, 0, 0, 0);
      const end = new Date(start.getTime() + 8 * 3600 * 1000);
      this.form.patchValue({ plannedStart: toLocalInput(start), plannedEnd: toLocalInput(end) });
      this.applyTemplate();
    }
  }

  /** Fill hazards / precautions / PPE with the standard text for the selected type. */
  applyTemplate(): void {
    const t = TEMPLATES[this.form.controls.type.value];
    this.form.patchValue({ hazards: t.hazards, precautions: t.precautions, requiredPpe: t.ppe });
  }

  invalid(name: keyof typeof this.form.controls): boolean {
    const c = this.form.controls[name];
    return c.invalid && (c.touched || c.dirty);
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const body: PermitRequest = {
      ...v,
      equipmentTag: v.equipmentTag || null,
      // datetime-local is local time; send UTC ISO strings to the API
      plannedStart: new Date(v.plannedStart).toISOString(),
      plannedEnd: new Date(v.plannedEnd).toISOString(),
    };

    this.saving.set(true);
    this.errors.set([]);
    const id = this.id();
    const request = id ? this.api.update(+id, body) : this.api.create(body);

    request.subscribe({
      next: (p) => this.router.navigate(['/permits', p.id]),
      error: (err) => {
        this.errors.set(errorMessages(err));
        this.saving.set(false);
      },
    });
  }
}
