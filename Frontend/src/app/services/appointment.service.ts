import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  BookAppointmentRequest,
  BookAppointmentResponse,
  Doctor,
  PatientAppointment,
  TimeSlot,
} from '../models/clinic';

const API = '/api/Appointments';

@Injectable({ providedIn: 'root' })
export class AppointmentService {
  private readonly http = inject(HttpClient);

  getDoctors(): Observable<Doctor[]> {
    return this.http.get<Doctor[]>(`${API}/doctors`);
  }

  getDates(): Observable<string[]> {
    return this.http.get<string[]>(`${API}/dates`);
  }

  getSchedule(doctorId: number, date: string): Observable<TimeSlot[]> {
    const params = new HttpParams().set('date', date);
    return this.http.get<TimeSlot[]>(`${API}/doctors/${doctorId}/schedule`, { params });
  }

  book(request: BookAppointmentRequest): Observable<BookAppointmentResponse> {
    return this.http.post<BookAppointmentResponse>(`${API}/book`, request);
  }

  getPatientAppointments(patientId: number): Observable<PatientAppointment[]> {
    return this.http.get<PatientAppointment[]>(`${API}/patients/${patientId}/appointments`);
  }

  cancel(appointmentId: number, patientId: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${API}/cancel`, {
      body: { appointmentId, patientId },
    });
  }
}
