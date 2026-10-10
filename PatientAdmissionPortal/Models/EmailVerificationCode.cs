namespace PatientAdmissionPortal.Models;

// A pending one-time code for an email address that is registering.
// Only a hash of the code is stored. No user account exists yet at this point.
public class EmailVerificationCode
{
    public int Id { get; set; }
    public string Email { get; set; } = "";      // normalized (trimmed, upper-case)
    public string CodeHash { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int Attempts { get; set; }
}
