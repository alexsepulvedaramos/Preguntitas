import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { guestGuard } from './core/auth/guest.guard';

export const routes: Routes = [
    {
        path: '',
        redirectTo: 'groups',
        pathMatch: 'full'
    },
    {
        path: 'auth',
        loadComponent: () => import('./features/auth/layouts/auth-layout.component').then(m => m.AuthLayoutComponent),
        canActivateChild: [guestGuard],
        children: [
            {
                path: 'register',
                title: 'Register',
                loadComponent: () => import('./features/auth/components/register/register.component').then(m => m.RegisterComponent)
            },
            {
                path: 'login',
                title: 'Login',
                loadComponent: () => import('./features/auth/components/login/login.component').then(m => m.LoginComponent)
            },
            {
                path: '',
                redirectTo: 'login',
                pathMatch: 'full'
            }
        ]
    },
    {
        path: '',
        loadComponent: () => import('./core/layout/main-layout.component').then(m => m.MainLayoutComponent),
        canActivateChild: [authGuard],
        children: [
            {
                path: 'groups',
                loadComponent: () => import('./features/groups/components/group-list/group-list.component').then(m => m.GroupListComponent),
            },
            {
                path: 'groups/:groupId',
                loadComponent: () => import('./features/groups/components/group-detail/group-detail.component').then(m => m.GroupDetailComponent),
            },
            {
                path: 'groups/:groupId/history',
                loadComponent: () => import('./features/groups/components/history/history.component').then(m => m.HistoryComponent),
            },
            {
                path: 'groups/:groupId/settings',
                loadComponent: () => import('./features/groups/components/group-settings/group-settings.component').then(m => m.GroupSettingsComponent),
            },
            {
                path: 'profile',
                title: 'Mi perfil',
                loadComponent: () => import('./features/profile/profile-page.component').then(m => m.ProfilePageComponent),
            },
        ]
    },
    {
        path: 'join/:code',
        title: 'Unirse al grupo',
        loadComponent: () => import('./features/groups/components/join-by-link/join-by-link.component').then(m => m.JoinByLinkComponent),
    },
    {
        // Fallback
        path: '**',
        redirectTo: 'groups'
    }
];