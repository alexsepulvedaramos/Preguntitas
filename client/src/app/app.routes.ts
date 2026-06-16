import { Routes } from '@angular/router';

export const routes: Routes = [
    {
        path: '',
        redirectTo: 'login',
        pathMatch: 'full'
    }, {
        path: 'register',
        loadComponent: () => import('./features/auth/register/register.component').then(m => m.RegisterComponent)
    }, {
        path: 'login',
        loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent)
    },
    // {
    // path: 'questions/:groupId',
    // loadComponent: () => import('./features/questions/question-list/question-list.component').then(m => m.QuestionListComponent)
    // },
    {
        path: '**',
        redirectTo: 'login'
    }
    // TODO: Add Home page which will explain how the app works.
];
