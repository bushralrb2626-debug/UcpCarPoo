using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UcpCarPool.Data;
using UcpCarPool.Models;
using UcpCarPool.Services;
using UcpCarPool.ViewModels;

namespace UcpCarPool.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly EmailService _emailService;

        private const string UcpEmailDomain = "@ucp.edu.pk";
        private const string OtpPurposeRegistration = "Registration";
        private const string OtpPurposePasswordReset = "PasswordReset";

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            EmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
        }


        // =========================================================
        // REGISTER - GET
        // =========================================================

        [HttpGet]
        public IActionResult Register()
        {
            if (_signInManager.IsSignedIn(User))
                return RedirectToAction("Index", "Home");

            return View(new RegisterViewModel());
        }


        // =========================================================
        // REGISTER - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var email = model.Email.Trim();

            // Only UCP email addresses are allowed
            if (!email.EndsWith(
                UcpEmailDomain,
                StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    "",
                    "Only UCP email addresses (@ucp.edu.pk) are allowed.");

                return View(model);
            }

            var existingUser =
                await _userManager.FindByEmailAsync(email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "",
                    "An account with this email already exists.");

                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = model.FullName,
                UniversityId = model.UniversityId,
                Batch = model.Batch,
                Department = model.Department,
                Gender = model.Gender,
                IsUcpVerified = true,
                EmailConfirmed = false,
                CreatedAt = DateTime.Now
            };

            var result = await _userManager.CreateAsync(
                user,
                model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                return View(model);
            }

            await _userManager.AddToRoleAsync(
                user,
                Roles.Member);

            // Generate OTP
            var otp = GenerateAndAttachOtp(user);

            await _userManager.UpdateAsync(user);

            // Send OTP through Brevo
            await _emailService.SendEmailAsync(
                user.Email!,
                "UcpCarPool - Email Verification Code",
                $"""
                <h2>UcpCarPool Email Verification</h2>

                <p>Hello {user.FullName},</p>

                <p>Your UcpCarPool verification code is:</p>

                <h1>{otp}</h1>

                <p>This code will expire in 10 minutes.</p>

                <p>If you did not create this account, please ignore this email.</p>

                <p>Regards,<br>UcpCarPool Team</p>
                """);

            return RedirectToAction(
                nameof(VerifyOtp),
                new
                {
                    email = user.Email,
                    purpose = OtpPurposeRegistration
                });
        }


        // =========================================================
        // VERIFY OTP - GET
        // =========================================================

        [HttpGet]
        public IActionResult VerifyOtp(
            string email,
            string purpose = OtpPurposeRegistration)
        {
            return View(
                new VerifyOtpViewModel
                {
                    Email = email,
                    Purpose = purpose
                });
        }


        // =========================================================
        // VERIFY OTP - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(
            VerifyOtpViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var email = model.Email.Trim();

            var isPasswordReset =
                string.Equals(
                    model.Purpose,
                    OtpPurposePasswordReset,
                    StringComparison.OrdinalIgnoreCase);

            var user =
                await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "Account not found.");

                return View(model);
            }

            if (!isPasswordReset && user.EmailConfirmed)
            {
                TempData["Info"] =
                    "Your email is already verified. Please log in.";

                return RedirectToAction(nameof(Login));
            }

            if (string.IsNullOrWhiteSpace(user.OtpCode) ||
                user.OtpExpiresAt == null)
            {
                ModelState.AddModelError(
                    "",
                    "No verification code is available. Please request a new code.");

                return View(model);
            }

            if (user.OtpExpiresAt < DateTime.Now)
            {
                ModelState.AddModelError(
                    "",
                    "This verification code has expired. Please request a new one.");

                return View(model);
            }

            if (user.OtpCode != model.Code.Trim())
            {
                ModelState.AddModelError(
                    "",
                    "Incorrect verification code. Please try again.");

                return View(model);
            }

            // Clear OTP after successful verification
            user.OtpCode = null;
            user.OtpExpiresAt = null;

            await _userManager.UpdateAsync(user);

            // Password reset verification
            if (isPasswordReset)
            {
                var token =
                    await _userManager.GeneratePasswordResetTokenAsync(user);

                TempData["Success"] =
                    "Verification successful. Please set your new password.";

                return RedirectToAction(
                    nameof(ResetPassword),
                    new
                    {
                        email = user.Email,
                        token
                    });
            }

            // Registration verification
            user.EmailConfirmed = true;

            await _userManager.UpdateAsync(user);

            await _signInManager.SignInAsync(
                user,
                isPersistent: false);

            TempData["Success"] =
                "Email verified successfully! Welcome to UcpCarPool.";

            return await RedirectByRoleAsync(user);
        }


        // =========================================================
        // RESEND OTP
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendOtp(
            string email,
            string purpose = OtpPurposeRegistration)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction(nameof(Register));
            }

            email = email.Trim();

            var isPasswordReset =
                string.Equals(
                    purpose,
                    OtpPurposePasswordReset,
                    StringComparison.OrdinalIgnoreCase);

            var user =
                await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                TempData["Error"] =
                    "Account not found.";

                return RedirectToAction(nameof(Login));
            }

            if (!isPasswordReset && user.EmailConfirmed)
            {
                TempData["Info"] =
                    "Your email is already verified. Please log in.";

                return RedirectToAction(nameof(Login));
            }

            var otp = GenerateAndAttachOtp(user);

            await _userManager.UpdateAsync(user);

            // Send new OTP through Brevo
            var subject = isPasswordReset
                ? "UcpCarPool - Password Reset Code"
                : "UcpCarPool - New Email Verification Code";

            var body = isPasswordReset
                ? $"""
                <h2>UcpCarPool Password Reset</h2>

                <p>Hello {user.FullName},</p>

                <p>Your password reset verification code is:</p>

                <h1>{otp}</h1>

                <p>This code will expire in 10 minutes.</p>

                <p>If you did not request a password reset, please ignore this email.</p>

                <p>Regards,<br>UcpCarPool Team</p>
                """
                : $"""
                <h2>UcpCarPool Email Verification</h2>

                <p>Hello {user.FullName},</p>

                <p>Your new verification code is:</p>

                <h1>{otp}</h1>

                <p>This code will expire in 10 minutes.</p>

                <p>Regards,<br>UcpCarPool Team</p>
                """;

            await _emailService.SendEmailAsync(
                user.Email!,
                subject,
                body);

            TempData["Info"] =
                "A new verification code has been sent to your email.";

            return RedirectToAction(
                nameof(VerifyOtp),
                new
                {
                    email = user.Email,
                    purpose
                });
        }


        // =========================================================
        // LOGIN - GET
        // =========================================================

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (_signInManager.IsSignedIn(User))
                return RedirectToAction(
                    "Index",
                    "Home");

            ViewData["ReturnUrl"] = returnUrl;

            return View(new LoginViewModel());
        }


        // =========================================================
        // LOGIN - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model,
            string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            var email = model.Email.Trim();

            var user =
                await _userManager.FindByEmailAsync(email);

            if (user != null && !user.EmailConfirmed)
            {
                var passwordOk =
                    await _userManager.CheckPasswordAsync(
                        user,
                        model.Password);

                if (passwordOk)
                {
                    var otp = GenerateAndAttachOtp(user);

                    await _userManager.UpdateAsync(user);

                    // Send OTP through Brevo
                    await _emailService.SendEmailAsync(
                        user.Email!,
                        "UcpCarPool - Email Verification Code",
                        $"""
                        <h2>UcpCarPool Email Verification</h2>

                        <p>Hello {user.FullName},</p>

                        <p>Your email verification code is:</p>

                        <h1>{otp}</h1>

                        <p>This code will expire in 10 minutes.</p>

                        <p>Regards,<br>UcpCarPool Team</p>
                        """);

                    TempData["Info"] =
                        "A verification code has been sent to your email.";

                    return RedirectToAction(
                        nameof(VerifyOtp),
                        new
                        {
                            email = user.Email,
                            purpose = OtpPurposeRegistration
                        });
                }
            }

            var result =
                await _signInManager.PasswordSignInAsync(
                    email,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: false);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) &&
                    Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                if (user != null)
                    return await RedirectByRoleAsync(user);

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            ModelState.AddModelError(
                "",
                "Invalid email or password.");

            return View(model);
        }


        // =========================================================
        // LOGOUT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction(
                "Index",
                "Home");
        }


        // =========================================================
        // FORGOT PASSWORD - GET
        // =========================================================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(
                new ForgotPasswordViewModel());
        }


        // =========================================================
        // FORGOT PASSWORD - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var email = model.Email.Trim();

            var user =
                await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "No account found with this email address.");

                return View(model);
            }

            var otp = GenerateAndAttachOtp(user);

            await _userManager.UpdateAsync(user);

            // Send password reset OTP through Brevo
            await _emailService.SendEmailAsync(
                user.Email!,
                "UcpCarPool - Password Reset Code",
                $"""
                <h2>UcpCarPool Password Reset</h2>

                <p>Hello {user.FullName},</p>

                <p>Your password reset verification code is:</p>

                <h1>{otp}</h1>

                <p>This code will expire in 10 minutes.</p>

                <p>If you did not request a password reset, please ignore this email.</p>

                <p>Regards,<br>UcpCarPool Team</p>
                """);

            TempData["Info"] =
                "A password reset verification code has been sent to your email.";

            return RedirectToAction(
                nameof(VerifyOtp),
                new
                {
                    email = user.Email,
                    purpose = OtpPurposePasswordReset
                });
        }


        // =========================================================
        // FORGOT PASSWORD CONFIRMATION
        // =========================================================

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }


        // =========================================================
        // RESET PASSWORD - GET
        // =========================================================

        [HttpGet]
        public IActionResult ResetPassword(
            string? email,
            string? token)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(token))
            {
                TempData["Error"] =
                    "Invalid or expired password reset link. Please start the forgot password process again.";

                return RedirectToAction(
                    nameof(ForgotPassword));
            }

            return View(
                new ResetPasswordViewModel
                {
                    Email = email,
                    Token = token
                });
        }


        // =========================================================
        // RESET PASSWORD - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user =
                await _userManager.FindByEmailAsync(
                    model.Email);

            if (user == null)
            {
                return RedirectToAction(
                    nameof(ResetPasswordConfirmation));
            }

            var result =
                await _userManager.ResetPasswordAsync(
                    user,
                    model.Token,
                    model.Password);

            if (result.Succeeded)
            {
                TempData["Success"] =
                    "Your password has been reset successfully. You can now log in.";

                return RedirectToAction(
                    nameof(ResetPasswordConfirmation));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    "",
                    error.Description);
            }

            return View(model);
        }


        // =========================================================
        // RESET PASSWORD CONFIRMATION
        // =========================================================

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }


        // =========================================================
        // ACCESS DENIED
        // =========================================================

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }


        // =========================================================
        // GENERATE OTP
        // =========================================================

        private static string GenerateAndAttachOtp(
            ApplicationUser user)
        {
            var otp =
                Random.Shared
                    .Next(100000, 1000000)
                    .ToString();

            user.OtpCode = otp;

            user.OtpExpiresAt =
                DateTime.Now.AddMinutes(10);

            return otp;
        }


        // =========================================================
        // ROLE REDIRECTION
        // =========================================================

        private async Task<IActionResult> RedirectByRoleAsync(
            ApplicationUser user)
        {
            if (await _userManager.IsInRoleAsync(
                    user,
                    Roles.Admin))
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }

            return RedirectToAction(
                "Index",
                "Home");
        }
    }
}
