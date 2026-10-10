using PatientAdmissionPortal.Models;

namespace PatientAdmissionPortal.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (db.Admissions.Any()) return;

        db.Admissions.AddRange(
            new Admission
            {
                FullName = "Anita Nair", DateOfBirth = new DateTime(1988, 4, 12), Gender = "Female",
                Phone = "9876543210", EmergencyContactName = "Ravi Nair", EmergencyContactPhone = "9876500000",
                Department = "Cardiology", Type = AdmissionType.Planned, Reason = "Scheduled angiography"
            },
            new Admission
            {
                FullName = "Joseph Mathew", DateOfBirth = new DateTime(1961, 9, 3), Gender = "Male",
                Phone = "9447000111", EmergencyContactName = "Mary Mathew", EmergencyContactPhone = "9447000222",
                Department = "Orthopedics", Type = AdmissionType.Emergency, Reason = "Fracture after fall"
            });
        db.SaveChanges();
    }
}
