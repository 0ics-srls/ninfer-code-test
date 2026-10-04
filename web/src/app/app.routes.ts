import { Routes } from '@angular/router';
import { TodosView } from './features/todos/todos-view';
import { WeekView } from './features/week/week-view';

export const routes: Routes = [
  { path: '', redirectTo: 'todos', pathMatch: 'full' },
  { path: 'todos', component: TodosView },
  { path: 'week', component: WeekView }
];
