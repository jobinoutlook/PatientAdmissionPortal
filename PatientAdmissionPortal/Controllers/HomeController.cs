using Microsoft.AspNetCore.Mvc;
using PatientAdmissionPortal.Models;
using PatientAdmissionPortal.Services;

namespace PatientAdmissionPortal.Controllers;

public class HomeController : Controller
{
    private readonly AdmissionStore _store;
    public HomeController(AdmissionStore store) => _store = store;

    public IActionResult Index()
    {
        var all = _store.Search(null, null);
        ViewBag.Pending = all.Count(a => a.Status == AdmissionStatus.Pending);
        ViewBag.Admitted = all.Count(a => a.Status == AdmissionStatus.Admitted);
        ViewBag.Discharged = all.Count(a => a.Status == AdmissionStatus.Discharged);
        ViewBag.Emergency = all.Count(a => a.Type == AdmissionType.Emergency && a.Status == AdmissionStatus.Pending);
        return View();
    }
}