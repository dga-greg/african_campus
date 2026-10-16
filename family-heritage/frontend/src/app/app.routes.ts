import type { Routes } from '@angular/router';
import { authGuard, guestGuard, superAdminGuard } from './core/auth/guards';
import { NAV_ITEMS } from './core/navigation';

const comingSoon: Routes = NAV_ITEMS.filter((i) => i.phase).map((i) => ({
  path: i.path.slice(1),
  title: `${i.label} · Family Heritage`,
  loadComponent: () => import('./features/placeholder/coming-soon.page').then((m) => m.ComingSoonPage),
}));

export const routes: Routes = [
  {
    path: 'auth',
    loadComponent: () => import('./layout/auth-layout.component').then((m) => m.AuthLayoutComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'login' },
      {
        path: 'login',
        title: 'Sign in · Family Heritage',
        canActivate: [guestGuard],
        loadComponent: () => import('./features/auth/login.page').then((m) => m.LoginPage),
      },
      {
        path: 'register',
        title: 'Create account · Family Heritage',
        canActivate: [guestGuard],
        loadComponent: () => import('./features/auth/register.page').then((m) => m.RegisterPage),
      },
      {
        path: 'forgot-password',
        title: 'Reset password · Family Heritage',
        loadComponent: () => import('./features/auth/forgot-password.page').then((m) => m.ForgotPasswordPage),
      },
      {
        path: 'reset-password',
        title: 'Choose a new password · Family Heritage',
        loadComponent: () => import('./features/auth/reset-password.page').then((m) => m.ResetPasswordPage),
      },
    ],
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/shell.component').then((m) => m.ShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        title: 'Dashboard · Family Heritage',
        loadComponent: () => import('./features/dashboard/dashboard.page').then((m) => m.DashboardPage),
      },
      {
        path: 'settings',
        title: 'Settings · Family Heritage',
        loadComponent: () => import('./features/settings/settings.page').then((m) => m.SettingsPage),
      },
      {
        path: 'admin',
        title: 'Administration · Family Heritage',
        canActivate: [superAdminGuard],
        loadComponent: () => import('./features/admin/admin-users.page').then((m) => m.AdminUsersPage),
      },
      ...comingSoon,
    ],
  },
  {
    path: '**',
    title: 'Not found · Family Heritage',
    loadComponent: () => import('./features/placeholder/not-found.page').then((m) => m.NotFoundPage),
  },
];
