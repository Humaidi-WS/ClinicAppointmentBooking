# Solution Diagram — ClinicAppointmentBooking

## Overview

```mermaid
flowchart TB
    User([User's Browser]) -->|"http://localhost:8080"| Web

    subgraph Docker["Docker Compose"]
        Web["clinic-web\nAngular 21 + TypeScript\nserved by nginx\n:8080 → 80"]
        API["clinic-api\nASP.NET Core 10 REST API\n+ Swagger UI\n:8081 → 8080"]

        subgraph API_Layers["API internals"]
            Controller["AppointmentController\n(HTTP endpoints)"]
            Service["AppointmentService\n(IAppointment)"]
            DbContext["AppDbContext\n(EF Core)"]
            Controller --> Service --> DbContext
        end

        DB[("SQLite\nclinic.db")]

        Web -->|"/api/ proxied by nginx"| API
        API --> DB
        DB -.- Vol["clinic-data volume\n/app/data"]
    end

    Tests["xUnitTests\n(booking, cancel, schedule,\ndoctors, dates, ...)"] -.->|integration tests| API
```

## Compact view

```
 Browser (localhost:8080)
        │
        ▼
┌─────────────────────┐      /api/ proxy       ┌──────────────────────────────┐
│  clinic-web         │ ──────────────────────► │  clinic-api                  │
│  Angular 21 (TS)    │     nginx → api:8080    │  ASP.NET Core 10             │
│  • Book page        │                         │  AppointmentController       │
│  • Appointments pg  │                         │    → AppointmentService      │
└─────────────────────┘                         │    → AppDbContext (EF Core)  │
                                                └──────────────┬───────────────┘
                                                               ▼
                                                   SQLite clinic.db
                                                (clinic-data Docker volume)

  xUnitTests ──► integration tests against the API
```

## API endpoints (`AppointmentController`)

| Method   | Route                                        | Purpose                      |
| -------- | -------------------------------------------- | ---------------------------- |
| GET      | `/api/appointments/doctors`                  | List doctors                 |
| GET      | `/api/appointments/dates`                    | Available booking dates      |
| GET      | `/api/appointments/doctors/{id}/schedule`    | Time slots for a doctor/date |
| POST     | `/api/appointments/book`                     | Book an appointment          |
| GET      | `/api/appointments/patients/{id}/appointments` | Patient's appointments     |
| DELETE   | `/api/appointments/cancel`                   | Cancel an appointment        |

## Data model

```
Person (base)
 ├── Doctor
 └── Patient
Appointment  ──►  Doctor + Patient + date/time slot
```
