import { Routes } from '@angular/router';
import { AppointmentsPage } from './pages/appointments-page/appointments-page';
import { BookPage } from './pages/book-page/book-page';

export const routes: Routes = [
  { path: '', component: BookPage },
  { path: 'appointments', component: AppointmentsPage },
  { path: '**', redirectTo: '' },
];
