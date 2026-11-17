export interface Doctor{
  id: number;
  firstName: string;
  lastName: string;
  specialty: string;
}

export interface TimeSlot {
  time: string;
  isAvailable: boolean;
}

export interface BookAppointmentRequest {
  doctorId: number;
  startTime: string;
  patientFirstName: string;
  patientLastName: string;
  patientPhoneNumber: string;
  patientDateOfBirth: string;
}

export interface BookAppointmentResponse {
  message: string;
  appointmentId: number;
  patientId: number;
}

export interface PatientAppointment {
  appointmentId: number;
  doctorId: number;
  doctorName: string;
  doctorSpecialty: string;
  startTime: string;
  appointmentStatus: string;
}

export interface ApiMessage {
  message?: string;
  error?: string;
}
