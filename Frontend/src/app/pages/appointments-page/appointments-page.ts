import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PatientAppointment } from '../../models/clinic';
import { AppointmentService } from '../../services/appointment.service';
import { SessionService } from '../../services/session.service';
import { extractApiError } from '../../utils/api-error';

@Component({
  selector: 'app-appointments-page',
  imports: [DatePipe, FormsModule],
  templateUrl: './appointments-page.html',
  styleUrl: './appointments-page.css',
})
export class AppointmentsPage {
  private readonly api = inject(AppointmentService);
  readonly session = inject(SessionService);

  readonly appointments = signal<PatientAppointment[]>([]);
  readonly loading = signal(false);
  readonly cancellingId = signal<number | null>(null);
  readonly error = signal<string | null>(null);
  readonly success = signal<string | null>(null);
  readonly hasLoaded = signal(false);
  patientIdInput: number | null = null;

  load(): void {
    const id = this.patientIdInput;
    if (!id || !Number.isInteger(id) || id < 1) {
      this.error.set('Please enter a valid Patient ID.');
      return;
    }

    this.session.setPatientId(id);
    this.error.set(null);
    this.loading.set(true);
    this.hasLoaded.set(false);

    this.api.getPatientAppointments(id).subscribe({
      next: (items) => {
        this.appointments.set(items);
        this.loading.set(false);
        this.hasLoaded.set(true);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(extractApiError(err));
      },
    });
  }

  applyPatientId(): void {
    this.success.set(null);
    this.load();
  }

  canCancel(appointment: PatientAppointment): boolean {
    return (
      appointment.appointmentStatus === 'Upcoming' &&
      new Date(appointment.startTime).getTime() > Date.now()
    );
  }

  cancel(appointment: PatientAppointment): void {
    if (!this.canCancel(appointment)) {
      return;
    }

    this.error.set(null);
    this.success.set(null);
    this.cancellingId.set(appointment.appointmentId);

    this.api.cancel(appointment.appointmentId, this.session.patientId()).subscribe({
      next: (result) => {
        this.cancellingId.set(null);
        this.success.set(result.message);
        this.load();
      },
      error: (err) => {
        this.cancellingId.set(null);
        this.error.set(extractApiError(err));
      },
    });
  }
}
