import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { errorMessages } from '../../core/api.services';
import { AuthService } from '../../core/auth.service';

interface DemoAccount {
  role: string;
  email: string;
}

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  templateUrl: './login.html',
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  protected readonly demoAccounts: DemoAccount[] = [
    { role: 'Requester', email: 'requester@ptwdemo.com' },
    { role: 'Area Authority', email: 'areaauthority@ptwdemo.com' },
    { role: 'Permit Issuer', email: 'issuer@ptwdemo.com' },
    { role: 'Admin', email: 'admin@ptwdemo.com' },
  ];

  useDemo(account: DemoAccount): void {
    this.form.setValue({ email: account.email, password: 'Password@123' });
    this.submit();
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading.set(true);
    this.error.set(null);

    const { email, password } = this.form.getRawValue();
    this.auth.login(email, password).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: (err) => {
        this.error.set(errorMessages(err)[0]);
        this.loading.set(false);
      },
    });
  }
}
