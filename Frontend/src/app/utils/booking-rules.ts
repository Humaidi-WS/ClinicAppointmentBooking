import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export function isWeekend(date: Date): boolean {
  const day = date.getDay();
  return day === 5 || day === 6;
}

export function isAlignedSlot(date: Date): boolean {
  return (
    (date.getMinutes() === 0 || date.getMinutes() === 30) &&
    date.getSeconds() === 0 &&
    date.getMilliseconds() === 0
  );
}

export function isWithinWorkingHours(date: Date): boolean {
  const minutes = date.getHours() * 60 + date.getMinutes();
  return minutes >= 8 * 60 && minutes < 16 * 60;
}

export function combineLocalDateTime(date: string, time: string): Date | null {
  if (!date || !time) {
    return null;
  }

  const value = new Date(`${date}T${time}:00`);
  return Number.isNaN(value.getTime()) ? null : value;
}

export function bookingRulesValidator(): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const date = group.get('date')?.value as string | null;
    const time = group.get('time')?.value as string | null;

    if (!date || !time) {
      return null;
    }

    const start = combineLocalDateTime(date, time);
    if (!start) {
      return { invalidStartTime: 'Choose a valid date and time.' };
    }

    if (start.getTime() <= Date.now()) {
      return { past: 'Cannot book an appointment in the past.' };
    }

    if (isWeekend(start)) {
      return { closed: 'The clinic is closed on Fridays and Saturdays.' };
    }

    if (!isWithinWorkingHours(start)) {
      return { hours: 'Time is outside working hours (08:00 to 16:00).' };
    }

    if (!isAlignedSlot(start)) {
      return { slot: 'Appointments must be aligned to exact 30-minute slots.' };
    }

    return null;
  };
}

export function bookingRuleMessage(errors: ValidationErrors | null): string | null {
  if (!errors) {
    return null;
  }

  return (
    (errors['past'] as string | undefined) ??
    (errors['closed'] as string | undefined) ??
    (errors['hours'] as string | undefined) ??
    (errors['slot'] as string | undefined) ??
    (errors['invalidStartTime'] as string | undefined) ??
    null
  );
}

export function toLocalStartTime(date: string, time: string): string {
  return `${date}T${time}:00`;
}

export function isPastSlot(date: string, time: string): boolean {
  const start = combineLocalDateTime(date, time);
  return !start || start.getTime() <= Date.now();
}
