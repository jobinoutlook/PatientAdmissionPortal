using System.ComponentModel.DataAnnotations;

namespace PatientAdmissionPortal.Models;

public enum AdmissionStatus { Pending, Admitted, Discharged }
public enum AdmissionType { Planned, Emergency }

public class Admission
{
    public static readonly string[] Departments =
        { "General Medicine", "Cardiology", "Orthopedics", "Pediatrics", "Obstetrics", "Surgery", "Neurology" };

    public int Id { get; set; }

    [Required, StringLength(100), Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Required, DataType(DataType.Date), Display(Name = "Date of birth")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime? DateOfBirth { get; set; }

    [Required] public string Gender { get; set; } = "";

    [Required, Phone] public string Phone { get; set; } = "";

    [EmailAddress] public string? Email { get; set; }

    [Required, StringLength(100), Display(Name = "Emergency contact")]
    public string EmergencyContactName { get; set; } = "";

    [Required, Phone, Display(Name = "Emergency contact phone")]
    public string EmergencyContactPhone { get; set; } = "";

    [Required] public string Department { get; set; } = "";

    [Display(Name = "Admission type")]
    public AdmissionType Type { get; set; } = AdmissionType.Planned;

    [Required, StringLength(500), Display(Name = "Reason for admission")]
    public string Reason { get; set; } = "";

    [Display(Name = "Insurance provider")] public string? InsuranceProvider { get; set; }
    [Display(Name = "Insurance / policy ID")] public string? InsuranceId { get; set; }

    public AdmissionStatus Status { get; set; } = AdmissionStatus.Pending;
    public string? Bed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? AdmittedAt { get; set; }
    public DateTime? DischargedAt { get; set; }

    public int? Age => DateOfBirth is null ? null :
        (int)((DateTime.Today - DateOfBirth.Value).TotalDays / 365.25);
}
