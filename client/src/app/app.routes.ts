import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { guestGuard } from './core/auth/guest.guard';

export const routes: Routes = [
    {
        path: '',
        redirectTo: 'login',
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
            }, {
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
        path: 'questions',
        loadComponent: () => import('./features/questions/question-list/question-list.component').then(m => m.QuestionListComponent),
        canActivate: [authGuard]
    },
    // {
    // path: 'questions/:groupId',
    // loadComponent: () => import('./features/questions/question-list/question-list.component').then(m => m.QuestionListComponent)
    // },
    {
        path: '**',
        redirectTo: 'auth/login'
    }
    // TODO: Add Home page which will explain how the app works.
];
