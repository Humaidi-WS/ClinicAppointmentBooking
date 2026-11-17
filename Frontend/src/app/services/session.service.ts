import { Injectable, signal } from '@angular/core';

const STORAGE_KEY = 'clinic-patient-id';

@Injectable({ providedIn: 'root' })
export class SessionService {
  readonly patientId = signal(readStoredPatientId());

  setPatientId(id: number): void {
    this.patientId.set(id);
    localStorage.setItem(STORAGE_KEY, String(id));
  }
}

function readStoredPatientId(): number {
  const raw = localStorage.getItem(STORAGE_KEY);
  const parsed = raw ? Number(raw) : 0;
  return Number.isInteger(parsed) && parsed > 0 ? parsed : 0;
}
