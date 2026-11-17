namespace ClinicAppointmentBooking.API.Data;

using ClinicAppointmentBooking.API.Models;
using Microsoft.EntityFrameworkCore;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Doctor)              
            .WithMany(d => d.Appointments)      
            .HasForeignKey(a => a.DoctorId)     
            .IsRequired()                       
            .OnDelete(DeleteBehavior.Restrict); 


        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Patient)             
            .WithMany(p => p.Appointments)      
            .HasForeignKey(a => a.PatientId)    
            .IsRequired()                       
            .OnDelete(DeleteBehavior.Restrict); 
    }
}