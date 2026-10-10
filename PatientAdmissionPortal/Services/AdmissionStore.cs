using Microsoft.EntityFrameworkCore;
using PatientAdmissionPortal.Data;
using PatientAdmissionPortal.Models;

namespace PatientAdmissionPortal.Services;

// Same public methods as the old in-memory version, now backed by SQL Server.
public class AdmissionStore
{
    private readonly AppDbContext _db;
    public AdmissionStore(AppDbContext db) => _db = db;

    public List<Admission> Search(string? q, AdmissionStatus? status)
    {
        var query = _db.Admissions.AsNoTracking().AsQueryable();

        if (status is not null)
            query = query.Where(a => a.Status == status);

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = int.TryParse(q, out var id)
                ? query.Where(a => a.Id == id || a.FullName.Contains(q))
                : query.Where(a => a.FullName.Contains(q));
        }

        // pending emergencies first, then newest
        return query
            .OrderBy(a => a.Type == AdmissionType.Emergency && a.Status == AdmissionStatus.Pending ? 0 : 1)
            .ThenByDescending(a => a.CreatedAt)
            .ToList();
    }

    public Admission? Get(int id) =>
        _db.Admissions.AsNoTracking().FirstOrDefault(a => a.Id == id);

    public void Add(Admission a)
    {
        _db.Admissions.Add(a);
        _db.SaveChanges();
    }

    public bool Admit(int id, string bed)
    {
        var a = _db.Admissions.Find(id);
        if (a is null || a.Status != AdmissionStatus.Pending) return false;

        a.Status = AdmissionStatus.Admitted;
        a.Bed = bed;
        a.AdmittedAt = DateTime.Now;
        _db.SaveChanges();
        return true;
    }

    public bool Discharge(int id)
    {
        var a = _db.Admissions.Find(id);
        if (a is null || a.Status != AdmissionStatus.Admitted) return false;

        a.Status = AdmissionStatus.Discharged;
        a.DischargedAt = DateTime.Now;
        _db.SaveChanges();
        return true;
    }
}
