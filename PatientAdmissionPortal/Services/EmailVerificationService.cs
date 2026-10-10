using Microsoft.EntityFrameworkCore;
using PatientAdmissionPortal.Data;
using PatientAdmissionPortal.Models;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace PatientAdmissionPortal.Services;

public enum SendResult { Sent, TooSoon, Failed }
public enum VerifyResult { Success, Invalid, Expired, TooManyAttempts }

// One-time codes for registration. Works on the email address alone: no user account is needed.
public class EmailVerificationService
{
    public static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
    private const int MaxAttempts = 5;

    private readonly AppDbContext _db;
    private readonly IAppEmailSender _email;
    private readonly ILogger<EmailVerificationService> _log;

    public EmailVerificationService(AppDbContext db, IAppEmailSender email, ILogger<EmailVerificationService> log)
    {
        _db = db; _email = email; _log = log;
    }

    private static string Normalize(string email) => email.Trim().ToUpperInvariant();

    public async Task<SendResult> SendCodeAsync(string email, string? name)
    {
        var key = Normalize(email);
        var old = await _db.EmailVerificationCodes.Where(c => c.Email == key).ToListAsync();
        var now = DateTime.UtcNow;

        if (old.Any(c => now - c.CreatedAt < ResendCooldown))
            return SendResult.TooSoon;

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        _db.EmailVerificationCodes.RemoveRange(old); // a new code replaces any earlier one
        var record = new EmailVerificationCode
        {
            Email = key,
            CodeHash = Hash(key, code),
            CreatedAt = now,
            ExpiresAt = now + CodeLifetime
        };
        _db.EmailVerificationCodes.Add(record);
        await _db.SaveChangesAsync();

        var greeting = string.IsNullOrWhiteSpace(name) ? "Hello," : $"Hi {WebUtility.HtmlEncode(name.Trim())},";
        var body = $"""
            <div style="font-family:Arial,sans-serif;max-width:420px">
              <h2>Verify your email</h2>
              <p>{greeting}</p>
              <p>Use this code to finish creating your account:</p>
              <p style="font-size:32px;font-weight:bold;letter-spacing:6px">{code}</p>
              <p>This code expires in {(int)CodeLifetime.TotalMinutes} minutes.
                 If you did not request it, you can ignore this email.</p>
            </div>
            """;

        try
        {
            await _email.SendAsync(email.Trim(), "Your verification code", body);
            return SendResult.Sent;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to send verification email to {Email}", email);
            _db.EmailVerificationCodes.Remove(record); // let the user retry immediately
            await _db.SaveChangesAsync();
            return SendResult.Failed;
        }
    }

    // Checks the code without using it up, so a failed registration (e.g. weak password) doesn't waste it.
    public async Task<VerifyResult> VerifyAsync(string email, string code)
    {
        var key = Normalize(email);
        var record = await _db.EmailVerificationCodes.FirstOrDefaultAsync(c => c.Email == key);
        if (record is null) return VerifyResult.Expired; // no code requested for this address

        if (record.ExpiresAt < DateTime.UtcNow)
        {
            _db.EmailVerificationCodes.Remove(record);
            await _db.SaveChangesAsync();
            return VerifyResult.Expired;
        }

        if (record.Attempts >= MaxAttempts) return VerifyResult.TooManyAttempts;

        var supplied = Encoding.UTF8.GetBytes(Hash(key, code.Trim()));
        var stored = Encoding.UTF8.GetBytes(record.CodeHash);
        if (!CryptographicOperations.FixedTimeEquals(supplied, stored))
        {
            record.Attempts++;
            await _db.SaveChangesAsync();
            return VerifyResult.Invalid;
        }

        return VerifyResult.Success;
    }

    // Call after the account has been created, so the code can't be reused.
    public async Task ConsumeAsync(string email)
    {
        var key = Normalize(email);
        var records = await _db.EmailVerificationCodes.Where(c => c.Email == key).ToListAsync();
        _db.EmailVerificationCodes.RemoveRange(records);
        await _db.SaveChangesAsync();
    }

    private static string Hash(string key, string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{key}:{code}")));
}
