using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PatientAdmissionPortal.Models;
using PatientAdmissionPortal.Services;
using PatientAdmissionPortal.ViewModels;

namespace PatientAdmissionPortal.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly EmailVerificationService _verification;

    public AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn,
        EmailVerificationService verification)
    {
        _users = users;
        _signIn = signIn;
        _verification = verification;
    }

    // ---------- Register ----------

    [HttpGet]
    public IActionResult Register() =>
        User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Home") : View();

    // Called by the "Send code" button on the registration page.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SendRegistrationCode(string? email, string? fullName)
    {
        email = email?.Trim() ?? "";

        if (!new EmailAddressAttribute().IsValid(email))
            return Json(new { ok = false, message = "Enter a valid email address." });

        if (await _users.FindByEmailAsync(email) is not null)
            return Json(new { ok = false, message = "This email is already registered. Please log in." });

        var result = await _verification.SendCodeAsync(email, fullName);
        return result switch
        {
            SendResult.Sent => Json(new { ok = true, message = $"We've sent a 6-digit code to {email}. It expires in 10 minutes." }),
            SendResult.TooSoon => Json(new { ok = false, retryAfter = 60, message = "A code was just sent. Please wait a minute before requesting another." }),
            _ => Json(new { ok = false, message = "We couldn't send the email. Please check the address and try again." })
        };
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var email = model.Email.Trim();

        if (await _users.FindByEmailAsync(email) is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "This email is already registered. Please log in.");
            return View(model);
        }

        // The code must have been sent to this exact email address.
        var check = await _verification.VerifyAsync(email, model.Code);
        if (check != VerifyResult.Success)
        {
            ModelState.AddModelError(nameof(model.Code), check switch
            {
                VerifyResult.Expired => "No valid code for this email. Click 'Send code' to get a new one.",
                VerifyResult.TooManyAttempts => "Too many incorrect attempts. Click 'Send code' for a new code.",
                _ => "That code is not correct."
            });
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = model.FullName.Trim(),
            EmailConfirmed = true            // proven by the code
        };

        var result = await _users.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);              // the code stays valid, so the user can fix the password and retry
        }

        await _verification.ConsumeAsync(email);
        await _signIn.SignInAsync(user, isPersistent: false);
        TempData["Msg"] = $"Welcome, {user.FullName}! Your email is verified and your account is ready.";
        return RedirectToAction("Index", "Home");
    }

    // ---------- Login / Logout ----------

    [HttpGet]
    public IActionResult Login(string? returnUrl = null) =>
        User.Identity?.IsAuthenticated == true
            ? RedirectToAction("Index", "Home")
            : View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _signIn.PasswordSignInAsync(
            model.Email.Trim(), model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
            return Url.IsLocalUrl(model.ReturnUrl) ? Redirect(model.ReturnUrl!) : RedirectToAction("Index", "Home");

        ModelState.AddModelError(string.Empty,
            result.IsLockedOut
                ? "Too many failed attempts. Please try again in a few minutes."
                : "Invalid email or password.");
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }
}
