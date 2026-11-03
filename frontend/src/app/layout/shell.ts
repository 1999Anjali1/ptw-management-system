import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { ROLE_LABELS } from '../shared/labels';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="shell">
      <aside class="sidebar">
        <div class="brand">
          <span class="brand__mark">PTW</span>
          <div>
            <div class="brand__name">Permit to Work</div>
            <div class="brand__sub">Site Safety System</div>
          </div>
        </div>

        <nav class="nav">
          <a routerLink="/dashboard" routerLinkActive="nav__link--active" class="nav__link">Dashboard</a>
          <a routerLink="/permits" routerLinkActive="nav__link--active" [routerLinkActiveOptions]="{ exact: true }" class="nav__link">All permits</a>
          @if (canCreate()) {
            <a routerLink="/permits/new" routerLinkActive="nav__link--active" class="nav__link">Raise new permit</a>
          }
        </nav>

        <div class="sidebar__user">
          <div class="avatar">{{ initials() }}</div>
          <div class="sidebar__user-info">
            <div class="sidebar__user-name">{{ auth.user()?.fullName }}</div>
            <div class="sidebar__user-role">{{ roleLabel() }}</div>
          </div>
          <button class="btn btn--ghost btn--sm" (click)="auth.logout()">Log out</button>
        </div>
      </aside>

      <main class="content">
        <router-outlet />
      </main>
    </div>
  `,
})
export class Shell {
  protected readonly auth = inject(AuthService);

  protected readonly canCreate = computed(() => this.auth.hasRole('Requester', 'Admin'));
  protected readonly roleLabel = computed(() => {
    const role = this.auth.user()?.role;
    return role ? ROLE_LABELS[role] : '';
  });
  protected readonly initials = computed(() =>
    (this.auth.user()?.fullName ?? '?')
      .split(' ')
      .map((p) => p[0])
      .slice(0, 2)
      .join(''),
  );
}
