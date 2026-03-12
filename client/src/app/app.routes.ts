import { Routes } from '@angular/router';

export const routes: Routes = [
    {
        path: '',
        loadComponent: () => import('./features/questions/question-list/question-list.component').then(m => m.QuestionListComponent)
    }
];
