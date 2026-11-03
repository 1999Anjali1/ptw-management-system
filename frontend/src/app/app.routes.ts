import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth.guard';
import { Shell } from './layout/shell';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./pages/login/login').then((m) => m.Login),
  },
  {
    path: '',
    component: Shell,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        title: 'Dashboard',
        loadComponent: () => import('./pages/dashboard/dashboard').then((m) => m.Dashboard),
      },
      {
        path: 'permits',
        title: 'Permits',
        loadComponent: () => import('./pages/permit-list/permit-list').then((m) => m.PermitList),
      },
      {
        path: 'permits/new',
        title: 'New permit',
        loadComponent: () => import('./pages/permit-form/permit-form').then((m) => m.PermitForm),
      },
      {
        path: 'permits/:id/edit',
        title: 'Edit permit',
        loadComponent: () => import('./pages/permit-form/permit-form').then((m) => m.PermitForm),
      },
      {
        path: 'permits/:id',
        title: 'Permit',
        loadComponent: () => import('./pages/permit-detail/permit-detail').then((m) => m.PermitDetailPage),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
