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
        // REGISTER
        // =========================================================

        [HttpGet]
        public IActionResult Register()
        {
            if (_signInManager.IsSignedIn(User))
                return RedirectToAction("Index", "Home");

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var email = model.Email.Trim();

            var existingUser = await _userManager.FindByEmailAsync(email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "",
                    "An account with this email already exists.");

                return View(model);
            }

            var isUcpEmail = email.EndsWith(
                UcpEmailDomain,
                StringComparison.OrdinalIgnoreCase);

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = model.FullName,
                UniversityId = model.UniversityId,
                Batch = model.Batch,
                Department = model.Department,
                Gender = model.Gender,
                IsUcpVerified = isUcpEmail,
                EmailConfirmed = false,
                CreatedAt = DateTime.Now
            };

            var result = await _userManager.CreateAsync(
                user,
                model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(model);
            }

            await _userManager.AddToRoleAsync(
                user,
                Roles.Member);

            // Generate OTP
            var otp = GenerateAndAttachOtp(user);

            await _userManager.UpdateAsync(user);

            // Send OTP by email
            try
            {
                await SendOtpEmailAsync(
                    user.Email!,
                    user.FullName,
                    otp);
            }
            catch
            {
                // If email sending fails, remove the created account
                await _userManager.DeleteAsync(user);

                ModelState.AddModelError(
                    "",
                    "We could not send the verification email. Please check the email settings and try again.");

                return View(model);
            }

            TempData["Info"] =
                "We've sent a 6-digit verification code to your email.";

            return RedirectToAction(
                nameof(VerifyOtp),
                new { email = user.Email });
        }


        // =========================================================
        // VERIFY OTP - GET
        // =========================================================

        [HttpGet]
        public IActionResult VerifyOtp(string email)
        {
            return View(
                new VerifyOtpViewModel
                {
                    Email = email
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

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "Account not found.");

                return View(model);
            }

            if (user.EmailConfirmed)
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

            // =====================================================
            // OTP CORRECT
            // =====================================================

            user.EmailConfirmed = true;
            user.OtpCode = null;
            user.OtpExpiresAt = null;

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
            string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction(
                    nameof(Register));
            }

            email = email.Trim();

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                TempData["Error"] =
                    "Account not found.";

                return RedirectToAction(
                    nameof(Login));
            }

            if (user.EmailConfirmed)
            {
                TempData["Info"] =
                    "Your email is already verified. Please log in.";

                return RedirectToAction(
                    nameof(Login));
            }

            // Generate new OTP
            var otp = GenerateAndAttachOtp(user);

            await _userManager.UpdateAsync(user);

            // Send new OTP
            try
            {
                await SendOtpEmailAsync(
                    user.Email!,
                    user.FullName,
                    otp);
            }
            catch
            {
                TempData["Error"] =
                    "We could not send the verification email. Please try again.";

                return RedirectToAction(
                    nameof(VerifyOtp),
                    new { email = user.Email });
            }

            TempData["Info"] =
                "A new verification code has been sent to your email.";

            return RedirectToAction(
                nameof(VerifyOtp),
                new { email = user.Email });
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

            return View(
                new LoginViewModel());
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

            var user = await _userManager.FindByEmailAsync(email);

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

                    try
                    {
                        await SendOtpEmailAsync(
                            user.Email!,
                            user.FullName,
                            otp);
                    }
                    catch
                    {
                        ModelState.AddModelError(
                            "",
                            "We could not send the verification email. Please try again.");

                        return View(model);
                    }

                    TempData["Info"] =
                        "Please verify your email. A new verification code has been sent.";

                    return RedirectToAction(
                        nameof(VerifyOtp),
                        new { email = user.Email });
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
                return RedirectToAction(
                    nameof(ForgotPasswordConfirmation));
            }

            var token =
                await _userManager.GeneratePasswordResetTokenAsync(
                    user);

            var resetUrl =
                Url.Action(
                    nameof(ResetPassword),
                    "Account",
                    new
                    {
                        email = user.Email,
                        token = token
                    },
                    Request.Scheme);

            try
            {
                await SendPasswordResetEmailAsync(
                    user.Email!,
                    user.FullName,
                    resetUrl!);
            }
            catch
            {
                ModelState.AddModelError(
                    "",
                    "We could not send the password reset email. Please try again.");

                return View(model);
            }

            return RedirectToAction(
                nameof(ForgotPasswordConfirmation));
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
            string email,
            string token)
        {
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
        // SEND OTP EMAIL
        // =========================================================

        private async Task SendOtpEmailAsync(
            string email,
            string? fullName,
            string otp)
        {
            var name =
                string.IsNullOrWhiteSpace(fullName)
                    ? "UcpCarPool User"
                    : fullName;

            var subject =
                "UcpCarPool - Email Verification Code";

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
</head>

<body style='font-family: Arial, sans-serif; background:#f5f7fa; padding:30px;'>

    <div style='max-width:600px; margin:auto; background:white; padding:30px; border-radius:12px;'>

        <h2 style='margin-bottom:10px;'>
            UcpCarPool Email Verification
        </h2>

        <p>Hello <strong>{System.Net.WebUtility.HtmlEncode(name)}</strong>,</p>

        <p>
            Thank you for registering with UcpCarPool.
            Use the verification code below to verify your email address:
        </p>

        <div style='font-size:32px; font-weight:bold; letter-spacing:8px; text-align:center; padding:20px;'>
            {otp}
        </div>

        <p>
            This code will expire in <strong>10 minutes</strong>.
        </p>

        <p>
            If you did not create this account, you can safely ignore this email.
        </p>

        <hr>

        <p style='color:#777; font-size:13px;'>
            UcpCarPool
        </p>

    </div>

</body>
</html>";

            await _emailService.SendEmailAsync(
                email,
                subject,
                body);
        }


        // =========================================================
        // SEND PASSWORD RESET EMAIL
        // =========================================================

        private async Task SendPasswordResetEmailAsync(
            string email,
            string? fullName,
            string resetUrl)
        {
            var name =
                string.IsNullOrWhiteSpace(fullName)
                    ? "UcpCarPool User"
                    : fullName;

            var subject =
                "UcpCarPool - Reset Your Password";

            var safeName =
                System.Net.WebUtility.HtmlEncode(name);

            var safeUrl =
                System.Net.WebUtility.HtmlEncode(resetUrl);

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
</head>

<body style='font-family: Arial, sans-serif; background:#f5f7fa; padding:30px;'>

    <div style='max-width:600px; margin:auto; background:white; padding:30px; border-radius:12px;'>

        <h2>Reset Your UcpCarPool Password</h2>

        <p>Hello <strong>{safeName}</strong>,</p>

        <p>
            We received a request to reset your UcpCarPool password.
        </p>

        <p>
            Click the button below to create a new password:
        </p>

        <p>
            <a href='{safeUrl}'
               style='display:inline-block;
                      background:#0066b3;
                      color:white;
                      padding:12px 20px;
                      text-decoration:none;
                      border-radius:6px;'>
                Reset Password
            </a>
        </p>

        <p style='font-size:13px; color:#777;'>
            If you did not request a password reset, you can ignore this email.
        </p>

        <hr>

        <p style='color:#777; font-size:13px;'>
            UcpCarPool
        </p>

    </div>

</body>
</html>";

            await _emailService.SendEmailAsync(
                email,
                subject,
                body);
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