using Microsoft.AspNetCore.Mvc;
using PatientAdmissionPortal.Models;
using PatientAdmissionPortal.Services;

namespace PatientAdmissionPortal.Controllers;

public class AdmissionsController : Controller
{
    private readonly AdmissionStore _store;
    public AdmissionsController(AdmissionStore store) => _store = store;

    public IActionResult Index(string? q, AdmissionStatus? status)
    {
        ViewBag.Q = q; ViewBag.Status = status;
        return View(_store.Search(q, status));
    }

    public IActionResult Create() => View(new Admission());

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(Admission model)
    {
        if (model.DateOfBirth > DateTime.Today)
            ModelState.AddModelError(nameof(model.DateOfBirth), "Date of birth cannot be in the future.");
        if (!ModelState.IsValid) return View(model);

        model.Status = AdmissionStatus.Pending;
        model.CreatedAt = DateTime.Now;
        _store.Add(model);
        TempData["Msg"] = $"Admission request #{model.Id} submitted for {model.FullName}.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    public IActionResult Details(int id)
    {
        var a = _store.Get(id);
        return a is null ? NotFound() : View(a);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Admit(int id, string bed)
    {
        if (string.IsNullOrWhiteSpace(bed)) TempData["Err"] = "Please enter a bed/ward.";
        else if (_store.Admit(id, bed.Trim())) TempData["Msg"] = "Patient admitted.";
        else TempData["Err"] = "Could not admit this patient.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Discharge(int id)
    {
        var ok = _store.Discharge(id);
        TempData[ok ? "Msg" : "Err"] = ok ? "Patient discharged." : "Could not discharge this patient.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
