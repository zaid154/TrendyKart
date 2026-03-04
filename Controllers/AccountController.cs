using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TrendyKart.Data;
using TrendyKart.Models;
using TrendyKart.Services;

namespace TrendyKart.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        public AccountController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // login for both admin and customer with email and password

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Email and Password required.";
                return View();
            }
            // Admin Login
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Email == email);
            if (admin != null)
            {
                var hasher = new PasswordHasher<Admin>();
                var result = hasher.VerifyHashedPassword(admin, admin.PasswordHash, password);
                if (result == PasswordVerificationResult.Success)
                {
                    await SignInUser(admin.FullName, admin.Email, "Admin", admin.AdminID.ToString());
                    return RedirectToAction("Dashboard", "Admin");
                }
                ViewBag.Error = "Invalid password.";
                return View();
            }
            // Customer Login
            var user = await _context.Customers.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                ViewBag.Error = "Account not found.";
                return View();
            }
            // Auto-delete expired unverified accounts
            if (!user.IsEmailVerified && user.OTPExpiry.HasValue && user.OTPExpiry < DateTime.UtcNow)
            {
                _context.Customers.Remove(user);
                await _context.SaveChangesAsync();

                ViewBag.Error = "Registration expired. Please register again.";
                return View();
            }
            if (user.IsBlocked)
            {
                ViewBag.Error = "Account blocked.";
                return View();
            }
            if (!user.IsEmailVerified)
            {
                ViewBag.Error = "Please verify your email first.";
                return View();
            }
            var customerHasher = new PasswordHasher<Customer>();
            var customerResult = customerHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (customerResult != PasswordVerificationResult.Success)
            {
                ViewBag.Error = "Invalid password.";
                return View();
            }
            await SignInUser(user.FullName, user.Email, "Customer", user.CustomerID.ToString());
            return RedirectToAction("Index", "Home");
        }
        // google login only for customers and checks if email is registered and not blocked before allowing login
        [HttpGet]
        public IActionResult GoogleLogin()
        {
            var redirectUrl = Url.Action("GoogleResponse", "Account");
            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUrl
            };
            return Challenge(properties, "Google");
        }
        [HttpGet]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync("Google");
            if (!result.Succeeded)
                return RedirectToAction("Login");
            var email = result.Principal?
                .FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login");
            var user = await _context.Customers
                .FirstOrDefaultAsync(x => x.Email == email);
            // not registered if try to login
            if (user == null)
            {
                await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["ErrorMessage"] = "User not registered. Please register first.";
                return RedirectToAction("Login");
            }
            // user blocked
            if (user.IsBlocked)
            {
                await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["ErrorMessage"] = "Account blocked.";
                return RedirectToAction("Login");
            }

            // login success
            await SignInUser(user.FullName, user.Email, "Customer",
            user.CustomerID.ToString());
            return RedirectToAction("Index", "Home");
        }
        private async Task SignInUser(string name, string email, string role, string? userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, name ?? ""),
                new Claim(ClaimTypes.Email, email ?? ""),
                new Claim(ClaimTypes.Role, role)
            };
            if (!string.IsNullOrEmpty(userId))
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

            var identity = new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));
        }
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // registration with email verification using OTP
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Register(string FullName, string Email, string Phone, string Password)
        {
            if (string.IsNullOrWhiteSpace(FullName) ||
                string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Phone) ||
                string.IsNullOrWhiteSpace(Password))
            {
                ViewBag.Error = "All fields are required.";
                return View();
            }
            // Check if email already exists 
            if (await _context.Customers.AnyAsync(u => u.Email == Email))
            {
                ViewBag.Error = "Email already registered.";
                return View();
            }
            // Generate OTP
            var otp = new Random().Next(100000, 999999).ToString();
            // Create temp user object 
            var tempUser = new Customer
            {
                FullName = FullName,
                Email = Email,
                Phone = Phone,
                PasswordHash = new PasswordHasher<Customer>().HashPassword(null, Password),
                IsBlocked = false,
                IsEmailVerified = false,
                OTP = otp,
                OTPExpiry = DateTime.UtcNow.AddMinutes(5),
                OTPAttempts = 0
            };
            // ✅ Store in Session
            HttpContext.Session.SetString("TempUser_FullName", FullName);
            HttpContext.Session.SetString("TempUser_Email", Email);
            HttpContext.Session.SetString("TempUser_Phone", Phone);
            HttpContext.Session.SetString("TempUser_PasswordHash", tempUser.PasswordHash);
            HttpContext.Session.SetString("TempUser_OTP", otp);
            HttpContext.Session.SetString("TempUser_OTPExpiry", tempUser.OTPExpiry.Value.ToString("o")); // ISO format
            HttpContext.Session.SetInt32("TempUser_OTPAttempts", 0);

            // Send OTP email
            await _emailService.SendEmailAsync(
    Email,
    "Verify Your Email - TrendyKart",
    $@"
    <p style='font-size:14px;font-weight:600;margin-bottom:15px;'>
        Account Email Verification
    </p>

    <p>Dear {FullName},</p>

    <p>Thank you for creating an account with <b>TrendyKart</b>.</p>

    <p>Please use the OTP below to verify your email and activate your account:</p>

    <p style='font-size:20px;font-weight:bold;color:#4361ee;'>
        OTP: {otp}
    </p>

    <p>This OTP is valid for 5 minutes.</p>

    <p>Regards,<br/>TrendyKart Team</p>
    "
);

            //Store email for verification
            HttpContext.Session.SetString("VerifyEmail", Email);
            return RedirectToAction("VerifyOTP");
        }
        // verify OTP page 
        // saves to DB when OTP is correct
        [HttpGet]
        public IActionResult VerifyOTP()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> VerifyOTP(string otp)
        {
            var email = HttpContext.Session.GetString("VerifyEmail");
            if (email == null) return RedirectToAction("Register");
            // Get temp user data from Session
            var tempFullName = HttpContext.Session.GetString("TempUser_FullName");
            var tempEmail = HttpContext.Session.GetString("TempUser_Email");
            var tempPhone = HttpContext.Session.GetString("TempUser_Phone");
            var tempPasswordHash = HttpContext.Session.GetString("TempUser_PasswordHash");
            var tempOTP = HttpContext.Session.GetString("TempUser_OTP");
            var tempOTPExpiryStr = HttpContext.Session.GetString("TempUser_OTPExpiry");
            var tempOTPAttempts = HttpContext.Session.GetInt32("TempUser_OTPAttempts") ?? 0;
            // Validate session data
            if (string.IsNullOrEmpty(tempFullName) || string.IsNullOrEmpty(tempEmail) ||
                string.IsNullOrEmpty(tempOTP) || string.IsNullOrEmpty(tempOTPExpiryStr))
            {
                HttpContext.Session.Remove("VerifyEmail");
                ViewBag.Error = "Registration session expired. Please register again.";
                return RedirectToAction("Register");
            }
            // Parse expiry
            if (!DateTime.TryParse(tempOTPExpiryStr, out var otpExpiry))
            {
                ViewBag.Error = "Invalid session data.";
                return RedirectToAction("Register");
            }
            // Check attempts
            if (tempOTPAttempts >= 3)
            {
                ViewBag.Error = "Too many wrong attempts. Please register again.";
                HttpContext.Session.Remove("VerifyEmail");
                return RedirectToAction("Register");
            }
            // Check expiry
            if (otpExpiry < DateTime.UtcNow)
            {
                ViewBag.Error = "OTP expired. Please register again.";
                HttpContext.Session.Remove("VerifyEmail");
                return RedirectToAction("Register");
            }
            // Verify OTP
            if (!string.Equals(tempOTP, otp, StringComparison.Ordinal))
            {
                // Increment attempts
                tempOTPAttempts++;
                HttpContext.Session.SetInt32("TempUser_OTPAttempts", tempOTPAttempts);

                ViewBag.Error = $"Invalid OTP. Attempts left: {3 - tempOTPAttempts}";
                return View();
            }

            // otp correct - create user in DB
            var newCustomer = new Customer
            {
                FullName = tempFullName,
                Email = tempEmail,
                Phone = tempPhone,
                PasswordHash = tempPasswordHash,
                IsBlocked = false,
                IsEmailVerified = true,  // Verified now
                OTP = null,               // Clear OTP
                OTPExpiry = null,
                OTPAttempts = 0,
            };

            _context.Customers.Add(newCustomer);
            await _context.SaveChangesAsync();

            // delete temp session data
            HttpContext.Session.Remove("VerifyEmail");
            HttpContext.Session.Remove("TempUser_FullName");
            HttpContext.Session.Remove("TempUser_Email");
            HttpContext.Session.Remove("TempUser_Phone");
            HttpContext.Session.Remove("TempUser_PasswordHash");
            HttpContext.Session.Remove("TempUser_OTP");
            HttpContext.Session.Remove("TempUser_OTPExpiry");
            HttpContext.Session.Remove("TempUser_OTPAttempts");

            TempData["SuccessMessage"] = "Registration successful! Please login.";
            return RedirectToAction("Login");
        }

        // resend OTP  new OTP and updates session data

        [HttpPost]
        public async Task<IActionResult> ResendOTP()
        {
            var email = HttpContext.Session.GetString("VerifyEmail");
            if (email == null) return RedirectToAction("Register");
            var fullName = HttpContext.Session.GetString("TempUser_FullName");
            // Get temp data from session
            var tempOTPExpiryStr = HttpContext.Session.GetString("TempUser_OTPExpiry");
            if (!string.IsNullOrEmpty(tempOTPExpiryStr) && DateTime.TryParse(tempOTPExpiryStr, out var otpExpiry))
            {
                if (otpExpiry > DateTime.UtcNow)
                {
                    ViewBag.Error = "You can request new OTP after expiry.";
                    return View("VerifyOTP");
                }
            }
            // Generate new OTP
            var newOtp = new Random().Next(100000, 999999).ToString();
            var newExpiry = DateTime.UtcNow.AddMinutes(5);
            // Update session
            HttpContext.Session.SetString("TempUser_OTP", newOtp);
            HttpContext.Session.SetString("TempUser_OTPExpiry", newExpiry.ToString("o"));
            HttpContext.Session.SetInt32("TempUser_OTPAttempts", 0);
            // Send email
            await _emailService.SendEmailAsync(
            email,
            "New OTP - Email Verification | TrendyKart",
            $@"
            <p style='font-size:14px;font-weight:600;margin-bottom:15px;'>
            New Email Verification OTP
            </p>
            <p>Dear {fullName},</p>
            <p>As requested, here is your new OTP to verify your email address.</p>
            <p style='font-size:20px;font-weight:bold;color:#4361ee;'>
            OTP: {newOtp}
            </p>
            <p>This OTP is valid for 5 minutes.</p>
            <p>If you did not request this, please ignore this email.</p>
            <p>Regards,<br/>TrendyKart Team</p>
            ");
            ViewBag.Success = "New OTP sent successfully.";
            return View("VerifyOTP");
        }
        //forgot password with OTP verification
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _context.Customers.FirstOrDefaultAsync(x => x.Email == email);
            if (user == null)
            {
                ViewBag.Error = "Email not found.";
                return View();
            }
            var otp = new Random().Next(100000, 999999).ToString();
            user.OTP = otp;
            user.OTPExpiry = DateTime.UtcNow.AddMinutes(5);
            user.OTPAttempts = 0;
            await _context.SaveChangesAsync();
            await _emailService.SendEmailAsync(
                email,
                "Reset Your Password - TrendyKart",
                $@"
                <p style='font-size:14px;font-weight:600;margin-bottom:15px;'>
                Password Reset Request
                </p>
                <p>Dear {user.FullName},</p>
                <p>We received a request to reset your TrendyKart account password.</p>
                <p>Please use the OTP below to proceed:</p>
                <p style='font-size:20px;font-weight:bold;color:#4361ee;'>
                 OTP: {otp}
                </p>
                <p>This OTP is valid for 5 minutes.</p>
                <p>If you did not request this, please ignore this email.</p><p>Regards,<br/>TrendyKart Team</p>
                  ");
            HttpContext.Session.SetString("ResetEmail", email);
            return RedirectToAction("VerifyResetOTP");
        }
        // verify OTP for password reset and allow user to set new password if OTP is correct
        [HttpGet]
        public IActionResult VerifyResetOTP()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> VerifyResetOTP(string otp)
        {
            var email = HttpContext.Session.GetString("ResetEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("ForgotPassword");
            var user = await _context.Customers
                .FirstOrDefaultAsync(x => x.Email == email);
            if (user == null)
                return RedirectToAction("ForgotPassword");
            if (string.IsNullOrEmpty(user.OTP))
            {
                ViewBag.Error = "OTP not found. Please request again.";
                return View();
            }
            if (user.OTPExpiry == null || user.OTPExpiry < DateTime.UtcNow)
            {
                ViewBag.Error = "OTP Expired. Please request again.";
                return View();
            }
            if (!string.Equals(user.OTP.Trim(), otp?.Trim(), StringComparison.Ordinal))
            {
                user.OTPAttempts++;
                if (user.OTPAttempts >= 3)
                {
                    user.OTP = null;
                    user.OTPExpiry = null;
                    await _context.SaveChangesAsync();
                    ViewBag.Error = "Too many wrong attempts. Please request OTP again.";
                    return View();
                }
                await _context.SaveChangesAsync();
                ViewBag.Error = $"Invalid OTP. Attempts left: {3 - user.OTPAttempts}";
                return View();
            }
            HttpContext.Session.SetString("ResetConfirmed", email);
            return RedirectToAction("ResetPassword");
        }
        //reset password page where user can set new password after OTP verification
        [HttpGet]
        public IActionResult ResetPassword()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ResetPassword(string newPassword)
        {
            var email = HttpContext.Session.GetString("ResetConfirmed");
            if (email == null) return RedirectToAction("Login");
            var user = await _context.Customers.FirstOrDefaultAsync(x => x.Email == email);
            user.PasswordHash = new PasswordHasher<Customer>()
            .HashPassword(user, newPassword);
            user.OTP = null;
            user.OTPExpiry = null;
            user.OTPAttempts = 0;
            await _context.SaveChangesAsync();
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Password reset successful.";
            return RedirectToAction("Login");
        }
    }
}