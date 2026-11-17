import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Doctor, TimeSlot } from '../../models/clinic';
import { AppointmentService } from '../../services/appointment.service';
import { SessionService } from '../../services/session.service';
import { extractApiError } from '../../utils/api-error';
import {
  bookingRuleMessage,
  bookingRulesValidator,
  isPastSlot,
  toLocalStartTime,
} from '../../utils/booking-rules';

@Component({
  selector: 'app-book-page',
  imports: [ReactiveFormsModule],
  templateUrl: './book-page.html',
  styleUrl: './book-page.css',
})
export class BookPage implements OnInit {
  private readonly api = inject(AppointmentService);
  private readonly session = inject(SessionService);
  private readonly fb = inject(FormBuilder);

  readonly doctors = signal<Doctor[]>([]);
  readonly dates = signal<string[]>([]);
  readonly slots = signal<TimeSlot[]>([]);
  readonly loadingSchedule = signal(false);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly success = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group(
    {
      doctorId: [0, [Validators.required, Validators.min(1)]],
      date: ['', Validators.required],
      time: ['', Validators.required],
      patientFirstName: ['', Validators.required],
      patientLastName: ['', Validators.required],
      patientPhoneNumber: ['', Validators.required],
      patientDateOfBirth: ['', Validators.required],
    },
    { validators: bookingRulesValidator() },
  );

  ngOnInit(): void {
    this.api.getDoctors().subscribe({
      next: (doctors) => this.doctors.set(doctors),
      error: (err) => this.error.set(extractApiError(err)),
    });

    this.api.getDates().subscribe({
      next: (dates) => this.dates.set(dates),
      error: (err) => this.error.set(extractApiError(err)),
    });

    this.form.controls.doctorId.valueChanges.subscribe(() => this.reloadSchedule());
    this.form.controls.date.valueChanges.subscribe(() => this.reloadSchedule());
  }

  slotStatus(slot: TimeSlot): 'free' | 'booked' | 'past' {
    const date = this.form.controls.date.value;
    if (date && isPastSlot(date, slot.time)) {
      return 'past';
    }

    return slot.isAvailable ? 'free' : 'booked';
  }

  selectSlot(slot: TimeSlot): void {
    if (this.slotStatus(slot) !== 'free') {
      return;
    }

    this.form.controls.time.setValue(slot.time);
    this.form.controls.time.markAsTouched();
    this.clearMessages();
  }

  bookingRuleError(): string | null {
    return this.form.touched ? bookingRuleMessage(this.form.errors) : null;
  }

  fieldError(name: 'doctorId' | 'date' | 'time' | 'patientFirstName' | 'patientLastName' | 'patientPhoneNumber' | 'patientDateOfBirth'): string | null {
    const control = this.form.controls[name];
    if (!control.touched || !control.errors) {
      return null;
    }

    if (control.errors['required'] || control.errors['min']) {
      if (name === 'doctorId') return 'Please pick a doctor.';
      if (name === 'date') return 'Please pick a date.';
      if (name === 'time') return 'Please pick a free slot.';
      if (name === 'patientFirstName') return 'First name is required.';
      if (name === 'patientLastName') return 'Last name is required.';
      if (name === 'patientPhoneNumber') return 'Phone number is required.';
      if (name === 'patientDateOfBirth') return 'Date of birth is required.';
    }

    return null;
  }

  submit(): void {
    this.form.markAllAsTouched();
    this.clearMessages();

    if (this.form.invalid) {
      this.error.set(this.bookingRuleError() ?? 'Please fix the form before booking.');
      return;
    }

    const { doctorId, date, time, patientFirstName, patientLastName, patientPhoneNumber, patientDateOfBirth } = this.form.getRawValue();
    const selected = this.slots().find((slot) => slot.time === time);
    if (!selected || this.slotStatus(selected) !== 'free') {
      this.error.set('Please pick a free slot.');
      return;
    }

    this.submitting.set(true);
    this.api
      .book({
        doctorId,
        startTime: toLocalStartTime(date, time),
        patientFirstName,
        patientLastName,
        patientPhoneNumber,
        patientDateOfBirth,
      })
      .subscribe({
        next: (result) => {
          this.submitting.set(false);
          this.session.setPatientId(result.patientId);
          this.success.set(`${result.message} Appointment #${result.appointmentId}.`);
          this.form.controls.time.reset('');
          this.reloadSchedule();
        },
        error: (err) => {
          this.submitting.set(false);
          this.error.set(extractApiError(err));
          this.reloadSchedule();
        },
      });
  }

  private reloadSchedule(): void {
    const doctorId = this.form.controls.doctorId.value;
    const date = this.form.controls.date.value;
    this.form.controls.time.reset('');
    this.slots.set([]);

    if (!doctorId || !date) {
      return;
    }

    this.loadingSchedule.set(true);
    this.api.getSchedule(doctorId, date).subscribe({
      next: (slots) => {
        this.slots.set(slots);
        this.loadingSchedule.set(false);
      },
      error: (err) => {
        this.loadingSchedule.set(false);
        this.error.set(extractApiError(err));
      },
    });
  }

  private clearMessages(): void {
    this.error.set(null);
    this.success.set(null);
  }
}
