// Routes lazy-load each feature and apply authentication before role restrictions.
// Backend APIs remain responsible for authorization even if browser guards are bypassed.
import { Routes } from '@angular/router';
import { authGuard, roleGuard } from './core/guards/auth.guard';
import { ROLES } from './core/models/auth.models';
import { Shell } from './shared/layout/shell';
const authPage = () => import('./features/auth/auth-page').then((m) => m.AuthPage);
const organizer = [authGuard, roleGuard([ROLES.organizer, ROLES.admin])];
const admin = [authGuard, roleGuard([ROLES.admin])];
export const routes: Routes = [
  {
    path: '',
    component: Shell,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'events' },
      { path: 'login', loadComponent: authPage },
      { path: 'register', loadComponent: authPage, data: { register: true } },
      {
        path: 'events',
        loadComponent: () => import('./features/events/events-page').then((m) => m.EventsPage),
      },
      {
        path: 'events/:id',
        loadComponent: () => import('./features/events/event-details').then((m) => m.EventDetails),
      },
      {
        path: 'my-bookings',
        canActivate: [authGuard],
        loadComponent: () =>
          import('./features/bookings/bookings-page').then((m) => m.BookingsPage),
      },
      {
        path: 'my-events',
        canActivate: organizer,
        loadComponent: () =>
          import('./features/organizer/my-events-page').then((m) => m.MyEventsPage),
      },
      {
        path: 'my-events/new',
        canActivate: organizer,
        loadComponent: () => import('./features/organizer/event-editor').then((m) => m.EventEditor),
      },
      {
        path: 'my-events/:id/edit',
        canActivate: organizer,
        loadComponent: () => import('./features/organizer/event-editor').then((m) => m.EventEditor),
      },
      {
        path: 'dashboard',
        canActivate: organizer,
        loadComponent: () =>
          import('./features/organizer/dashboard-page').then((m) => m.DashboardPage),
      },
      {
        path: 'admin/users',
        canActivate: admin,
        loadComponent: () => import('./features/admin/users-page').then((m) => m.UsersPage),
      },
      {
        path: 'admin/bookings',
        canActivate: admin,
        loadComponent: () =>
          import('./features/admin/all-bookings-page').then((m) => m.AllBookingsPage),
      },
      { path: '**', redirectTo: 'events' },
    ],
  },
];
