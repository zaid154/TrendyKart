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
using Microsoft.AspNetCore.Hosting;

namespace TrendyKart.Controllers
{

    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AccountController(ApplicationDbContext context, IEmailService emailService, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _emailService = emailService;
            _webHostEnvironment = webHostEnvironment;
        }

        //LOGIN 
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Email and Password required.";
                return View();
            }

            // admin
            var dbAdmin = await _context.Admins.FirstOrDefaultAsync(a => a.Email == email);

            if (dbAdmin != null)
            {
                var hasher = new PasswordHasher<Admin>();
                var result = hasher.VerifyHashedPassword(dbAdmin, dbAdmin.PasswordHash, password);

                if (result == PasswordVerificationResult.Success)
                {
                    await SignInUser(dbAdmin.FullName, dbAdmin.Email, "Admin", dbAdmin.AdminID.ToString());
                    return RedirectToAction("Dashboard", "Admin");
                }

                ViewBag.Error = "Invalid password.";
                return View();
            }

            //  CUSTOMER LOGIN
            var user = await _context.Customers.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                ViewBag.Error = "Account not found.";
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

        // GOOGLE LOGIN
        [AllowAnonymous]
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

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GoogleResponse()
        {
            // Read the Google identity from the temporary "External" cookie.
            var result = await HttpContext.AuthenticateAsync("External");
            if (!result.Succeeded)
                return RedirectToAction("Login");

            var email = result.Principal?.FindFirst(ClaimTypes.Email)?.Value;
            var name = result.Principal?.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                await HttpContext.SignOutAsync("External");
                TempData["ErrorMessage"] = "Google account has no email. Please try again.";
                return RedirectToAction("Login");
            }

            var user = await _context.Customers.FirstOrDefaultAsync(x => x.Email == email);

            // SIGN UP — first-time Google users get an account created automatically.
            if (user == null)
            {
                user = new Customer
                {
                    FullName = string.IsNullOrWhiteSpace(name) ? email : name,
                    Email = email,
                    Phone = string.Empty,
                    PasswordHash = string.Empty,   // Google-only account, no local password
                    IsBlocked = false,
                    IsEmailVerified = true          // Google has already verified the email
                };

                _context.Customers.Add(user);
                await _context.SaveChangesAsync();
            }

            if (user.IsBlocked)
            {
                await HttpContext.SignOutAsync("External");
                TempData["ErrorMessage"] = "Account blocked.";
                return RedirectToAction("Login");
            }

            // Drop the temporary external cookie, then issue the real app cookie.
            await HttpContext.SignOutAsync("External");
            await SignInUser(user.FullName, user.Email, "Customer", user.CustomerID.ToString());
            return RedirectToAction("Index", "Home");
        }

        // Simple password rule: at least 6 chars, with one letter and one digit.
        private static bool IsValidPassword(string? password)
        {
            return !string.IsNullOrWhiteSpace(password)
                && password.Length >= 6
                && password.Any(char.IsLetter)
                && password.Any(char.IsDigit);
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

            if (role == "Customer" && !string.IsNullOrEmpty(userId))
            {
                claims.Add(new Claim("ProfileImage", "/images/Default_Profile.jpg"));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));
        }

        // LOGOUT 
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // REGISTRATION 
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [AllowAnonymous]
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

            if (!IsValidPassword(Password))
            {
                ViewBag.Error = "Password must be at least 6 characters and include a letter and a number.";
                return View();
            }

            if (await _context.Customers.AnyAsync(u => u.Email == Email))
            {
                ViewBag.Error = "Email already registered.";
                return View();
            }

            var otp = Random.Shared.Next(100000, 999999).ToString();

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

            HttpContext.Session.SetString("TempUser_FullName", FullName);
            HttpContext.Session.SetString("TempUser_Email", Email);
            HttpContext.Session.SetString("TempUser_Phone", Phone);
            HttpContext.Session.SetString("TempUser_PasswordHash", tempUser.PasswordHash);
            HttpContext.Session.SetString("TempUser_OTP", otp);
            HttpContext.Session.SetString("TempUser_OTPExpiry", tempUser.OTPExpiry.Value.ToString("o"));
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
                <p>Regards,<br/>TrendyKart Team</p>");

            // Store email for verification
            HttpContext.Session.SetString("VerifyEmail", Email);
            return RedirectToAction("VerifyOTP");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult VerifyOTP()
        {
            return View();
        }

        [AllowAnonymous]
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

            if (!DateTime.TryParse(tempOTPExpiryStr, out var otpExpiry))
            {
                ViewBag.Error = "Invalid session data.";
                return RedirectToAction("Register");
            }

            if (tempOTPAttempts >= 3)
            {
                ViewBag.Error = "Too many wrong attempts. Please register again.";
                HttpContext.Session.Remove("VerifyEmail");
                return RedirectToAction("Register");
            }

            if (otpExpiry < DateTime.UtcNow)
            {
                ViewBag.Error = "OTP expired. Please register again.";
                HttpContext.Session.Remove("VerifyEmail");
                return RedirectToAction("Register");
            }

            if (!string.Equals(tempOTP, otp, StringComparison.Ordinal))
            {
                tempOTPAttempts++;
                HttpContext.Session.SetInt32("TempUser_OTPAttempts", tempOTPAttempts);
                ViewBag.Error = $"Invalid OTP. Attempts left: {3 - tempOTPAttempts}";
                return View();
            }

            var newCustomer = new Customer
            {
                FullName = tempFullName,
                Email = tempEmail,
                Phone = tempPhone,
                PasswordHash = tempPasswordHash,
                IsBlocked = false,
                IsEmailVerified = true,
                OTP = null,
                OTPExpiry = null,
                OTPAttempts = 0,
            };

            _context.Customers.Add(newCustomer);
            await _context.SaveChangesAsync();

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

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ResendOTP()
        {
            var email = HttpContext.Session.GetString("VerifyEmail");
            if (email == null) return RedirectToAction("Register");

            var fullName = HttpContext.Session.GetString("TempUser_FullName");

            var tempOTPExpiryStr = HttpContext.Session.GetString("TempUser_OTPExpiry");
            if (!string.IsNullOrEmpty(tempOTPExpiryStr) && DateTime.TryParse(tempOTPExpiryStr, out var otpExpiry))
            {
                if (otpExpiry > DateTime.UtcNow)
                {
                    ViewBag.Error = "You can request new OTP after expiry.";
                    return View("VerifyOTP");
                }
            }

            var newOtp = Random.Shared.Next(100000, 999999).ToString();
            var newExpiry = DateTime.UtcNow.AddMinutes(5);

            HttpContext.Session.SetString("TempUser_OTP", newOtp);
            HttpContext.Session.SetString("TempUser_OTPExpiry", newExpiry.ToString("o"));
            HttpContext.Session.SetInt32("TempUser_OTPAttempts", 0);

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
                <p>Regards,<br/>TrendyKart Team</p>");

            ViewBag.Success = "New OTP sent successfully.";
            return View("VerifyOTP");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _context.Customers.FirstOrDefaultAsync(x => x.Email == email);
            if (user == null)
            {
                ViewBag.Error = "Email not found.";
                return View();
            }

            var otp = Random.Shared.Next(100000, 999999).ToString();
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
                <p>If you did not request this, please ignore this email.</p>
                <p>Regards,<br/>TrendyKart Team</p>");

            HttpContext.Session.SetString("ResetEmail", email);
            return RedirectToAction("VerifyResetOTP");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult VerifyResetOTP()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> VerifyResetOTP(string otp)
        {
            var email = HttpContext.Session.GetString("ResetEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("ForgotPassword");

            var user = await _context.Customers.FirstOrDefaultAsync(x => x.Email == email);
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

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ResetPassword()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ResetPassword(string newPassword)
        {
            var email = HttpContext.Session.GetString("ResetConfirmed");
            if (email == null) return RedirectToAction("Login");

            if (!IsValidPassword(newPassword))
            {
                ViewBag.Error = "Password must be at least 6 characters and include a letter and a number.";
                return View();
            }

            var user = await _context.Customers.FirstOrDefaultAsync(x => x.Email == email);
            if (user == null)
            {
                HttpContext.Session.Clear();
                TempData["ErrorMessage"] = "Account not found. Please try again.";
                return RedirectToAction("Login");
            }

            user.PasswordHash = new PasswordHasher<Customer>().HashPassword(user, newPassword);
            user.OTP = null;
            user.OTPExpiry = null;
            user.OTPAttempts = 0;
            await _context.SaveChangesAsync();

            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Password reset successful.";
            return RedirectToAction("Login");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrEmpty(userIdClaim))
                return RedirectToAction("Login");

            if (!int.TryParse(userIdClaim, out int userId))
                return RedirectToAction("Login");

            if (role == "Admin")
            {
                var admin = await _context.Admins.FindAsync(userId);

                if (admin == null)
                {
                    ViewBag.Error = "Customer not found";
                    return View("Profile", null);
                }

                return View("Profile", admin);
            }

            var customer = await _context.Customers.FindAsync(userId);

            if (customer == null)
                return RedirectToAction("Login");

            return View("Profile", customer);
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SendUpdateOTP([FromBody] UpdateProfileRequest request)
        {
            var customerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var customer = await _context.Customers.FindAsync(customerId);

            if (customer == null)
                return Json(new { success = false, message = "User not found" });

            var finalEmail = string.IsNullOrEmpty(request.NewEmail) ? request.Email : request.NewEmail;

            bool isEmailChanged = finalEmail != customer.Email;
            bool isPasswordChanged = !string.IsNullOrEmpty(request.NewPassword);
            bool isPhoneChanged = request.Phone != customer.Phone;

            // DUPLICATE EMAIL CHECK
            if (isEmailChanged)
            {
                var exists = await _context.Customers
                    .AnyAsync(c => c.Email == finalEmail && c.CustomerID != customerId);

                if (exists)
                    return Json(new { success = false, message = "Email already exists" });
            }

            if (!isEmailChanged && !isPasswordChanged && !isPhoneChanged)
            {
                customer.FullName = request.FullName;
                customer.DefaultStreet = request.DefaultStreet;
                customer.DefaultCity = request.DefaultCity;
                customer.DefaultState = request.DefaultState;
                customer.DefaultPincode = request.DefaultPincode;
                customer.DefaultCountry = request.DefaultCountry;

                await _context.SaveChangesAsync();

                return Json(new { success = true, directUpdate = true, message = "Profile updated" });
            }

            var lastOtpTimeStr = HttpContext.Session.GetString("LastOtpTime");

            if (!string.IsNullOrEmpty(lastOtpTimeStr))
            {
                var lastOtpTime = DateTime.Parse(lastOtpTimeStr);

                if (DateTime.UtcNow < lastOtpTime.AddSeconds(60))
                {
                    return Json(new { success = false, message = "Wait 60 seconds before new OTP" });
                }
            }

            if (isPasswordChanged)
            {
                if (string.IsNullOrEmpty(request.CurrentPassword))
                    return Json(new { success = false, message = "Enter current password" });

                var hasher = new PasswordHasher<Customer>();
                var result = hasher.VerifyHashedPassword(customer, customer.PasswordHash, request.CurrentPassword);

                if (result != PasswordVerificationResult.Success)
                    return Json(new { success = false, message = "Wrong current password" });
            }

            var otp = Random.Shared.Next(100000, 999999).ToString();

            customer.OTP = otp;
            customer.OTPExpiry = DateTime.UtcNow.AddMinutes(5);
            customer.OTPAttempts = 0;

            HttpContext.Session.SetString("LastOtpTime", DateTime.UtcNow.ToString());
            HttpContext.Session.SetString("PendingUpdateData",
                System.Text.Json.JsonSerializer.Serialize(request));

            await _context.SaveChangesAsync();

            await _emailService.SendEmailAsync(
                customer.Email,
                "TrendyKart Profile Update Verification",
                $@"
                <div style='font-family:Arial,sans-serif;padding:20px;color:#333;'>
                    <h2 style='color:#4361ee;'>Profile Update Request</h2>
                    <p>Dear <b>{customer.FullName}</b>,</p>
                    <p>We received a request to update your account details on <b>TrendyKart</b>.</p>
                    <p>The following changes were requested:</p>
                    <ul>
                        {(request.NewEmail != null ? "<li>Email Address Change</li>" : "")}
                        {(request.Phone != customer.Phone ? "<li>Phone Number Change</li>" : "")}
                        {(request.NewPassword != null ? "<li>Password Change</li>" : "")}
                    </ul>
                    <p>Please use the OTP below to confirm these changes:</p>
                    <div style='font-size:22px;font-weight:bold;color:#4361ee;margin:15px 0;'>
                        OTP: {otp}
                    </div>
                    <p>This OTP is valid for 5 minutes.</p>
                    <p style='margin-top:20px;'>
                        If you did not request these changes, please ignore this email or contact our support team immediately.
                    </p>
                    <br/>
                    <p>Regards,<br/><b>TrendyKart Team</b></p>
                </div>
                "
            );

            return Json(new { success = true, message = "OTP sent successfully" });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> VerifyUpdateOTP([FromBody] VerifyOTPRequest request)
        {
            var customerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var customer = await _context.Customers.FindAsync(customerId);

            if (customer == null)
                return Json(new { success = false, message = "User not found" });

            if (string.IsNullOrEmpty(customer.OTP))
                return Json(new { success = false, message = "No OTP found" });

            if (customer.OTPExpiry == null || customer.OTPExpiry < DateTime.UtcNow)
            {
                customer.OTP = null;
                customer.OTPExpiry = null;
                await _context.SaveChangesAsync();

                return Json(new { success = false, message = "OTP expired" });
            }

            if (request.OTP != customer.OTP)
            {
                customer.OTPAttempts++;

                if (customer.OTPAttempts >= 3)
                {
                    customer.OTP = null;
                    customer.OTPExpiry = null;
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = false,
                    message = $"Invalid OTP. Attempts left: {3 - customer.OTPAttempts}"
                });
            }

            var data = HttpContext.Session.GetString("PendingUpdateData");

            if (string.IsNullOrEmpty(data))
                return Json(new { success = false, message = "Session expired. Try again" });

            var update = System.Text.Json.JsonSerializer.Deserialize<UpdateProfileRequest>(data);

            if (update == null)
                return Json(new { success = false, message = "Invalid data" });

            var finalEmail = string.IsNullOrEmpty(update.NewEmail) ? update.Email : update.NewEmail;

            bool emailChanged = finalEmail != customer.Email;

            customer.FullName = update.FullName;
            customer.Phone = update.Phone;
            customer.DefaultStreet = update.DefaultStreet;
            customer.DefaultCity = update.DefaultCity;
            customer.DefaultState = update.DefaultState;
            customer.DefaultPincode = update.DefaultPincode;
            customer.DefaultCountry = update.DefaultCountry;

            if (emailChanged)
            {
                customer.Email = finalEmail;
                customer.IsEmailVerified = false;
            }

            if (!string.IsNullOrEmpty(update.NewPassword))
            {
                var hasher = new PasswordHasher<Customer>();
                customer.PasswordHash = hasher.HashPassword(customer, update.NewPassword);
            }

            customer.OTP = null;
            customer.OTPExpiry = null;
            customer.OTPAttempts = 0;

            HttpContext.Session.Remove("PendingUpdateData");
            HttpContext.Session.Remove("LastOtpTime");

            await _context.SaveChangesAsync();

            if (emailChanged)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Json(new { success = true, emailChanged = true, message = "Email changed. Login again." });
            }

            return Json(new { success = true, message = "Profile updated successfully" });
        }

        public class UpdateProfileRequest
        {
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string? NewEmail { get; set; }
            public string Phone { get; set; } = string.Empty;
            public string? CurrentPassword { get; set; }
            public string? NewPassword { get; set; }
            public string? DefaultStreet { get; set; }
            public string? DefaultCity { get; set; }
            public string? DefaultState { get; set; }
            public string? DefaultPincode { get; set; }
            public string? DefaultCountry { get; set; }
        }

        public class VerifyOTPRequest
        {
            public string OTP { get; set; } = string.Empty;
        }

        // ADDRESS BOOK MANAGEMENT
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Addresses()
        {
            var customerIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(customerIdStr, out int customerId))
                return RedirectToAction("Login");

            var addresses = await _context.CustomerAddresses
                .Where(a => a.CustomerID == customerId)
                .OrderByDescending(a => a.IsDefault)
                .ToListAsync();

            return View(addresses);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddAddress(CustomerAddress address)
        {
            var customerIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(customerIdStr, out int customerId))
                return Json(new { success = false, message = "Please login." });

            if (string.IsNullOrWhiteSpace(address.FullName) || string.IsNullOrWhiteSpace(address.AddressLine1) ||
                string.IsNullOrWhiteSpace(address.City) || string.IsNullOrWhiteSpace(address.State) || string.IsNullOrWhiteSpace(address.Pincode))
            {
                return Json(new { success = false, message = "Please fill in all required address fields." });
            }

            address.CustomerID = customerId;

            // If this is the customer's first address, set as default
            bool hasAddresses = await _context.CustomerAddresses.AnyAsync(a => a.CustomerID == customerId);
            if (!hasAddresses)
            {
                address.IsDefault = true;
            }
            else if (address.IsDefault)
            {
                // Reset existing default
                var existingDefaults = await _context.CustomerAddresses.Where(a => a.CustomerID == customerId && a.IsDefault).ToListAsync();
                foreach (var ex in existingDefaults) ex.IsDefault = false;
            }

            _context.CustomerAddresses.Add(address);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Address saved successfully!" });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SetDefaultAddress(int id)
        {
            var customerIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(customerIdStr, out int customerId))
                return Json(new { success = false, message = "Please login." });

            var addresses = await _context.CustomerAddresses.Where(a => a.CustomerID == customerId).ToListAsync();
            foreach (var addr in addresses)
            {
                addr.IsDefault = (addr.AddressID == id);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Default address updated." });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var customerIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(customerIdStr, out int customerId))
                return Json(new { success = false, message = "Please login." });

            var address = await _context.CustomerAddresses.FirstOrDefaultAsync(a => a.AddressID == id && a.CustomerID == customerId);
            if (address != null)
            {
                _context.CustomerAddresses.Remove(address);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Address deleted." });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetSavedAddresses()
        {
            var customerIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(customerIdStr, out int customerId))
                return Json(new List<object>());

            var addresses = await _context.CustomerAddresses
                .Where(a => a.CustomerID == customerId)
                .OrderByDescending(a => a.IsDefault)
                .Select(a => new
                {
                    a.AddressID,
                    a.FullName,
                    a.Phone,
                    a.AddressLine1,
                    a.AddressLine2,
                    a.City,
                    a.State,
                    a.Pincode,
                    a.AddressType,
                    a.IsDefault
                })
                .ToListAsync();

            return Json(addresses);
        }
    }
}