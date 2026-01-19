using BCrypt.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using MimeKit.Text;
using Org.BouncyCastle.Crypto.Generators;
using ScopeIndiaWebsite.Data;
using ScopeIndiaWebsite.Models;
using ScopeIndiaWebsite.ViewModels;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace ScopeIndiaWebsite.Controllers
{
    public class StudentController : Controller
    {
        private readonly MVCDbContext _dbContext;


        public StudentController(MVCDbContext dbContext)
        {
            _dbContext = dbContext;
            
        }

        public IActionResult Register()
        {
            var student = new Student();
            student.Gender = (Gender)(-1); // Ensure no default selection
            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Student std)
        {
            //if (ModelState.IsValid)
            //{
                var existingStudent = await _dbContext.RegistrationTable
            .FirstOrDefaultAsync(x => x.reg_email == std.reg_email);

            if (existingStudent != null)
            {
                ModelState.AddModelError("reg_email", "This email is already registered.");
                TempData["msg"] = "This email is already registered!";
                return View(std);
            }
            // 1️⃣ Save hobbies as comma-separated string
            std.reg_hobbies = (std.SelectedHobbies != null && std.SelectedHobbies.Count > 0)
                                    ? string.Join(",", std.SelectedHobbies)
                                    : "";

                // 2️⃣ Validate and save avatar
                if (std.AvatarFile != null && std.AvatarFile.Length > 0)
                {
                    // Optional: Check file type
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var ext = Path.GetExtension(std.AvatarFile.FileName).ToLower();
                    if (!allowedExtensions.Contains(ext))
                    {
                        ModelState.AddModelError("AvatarFile", "Only JPG, PNG, or GIF files are allowed.");
                        return View(std);
                    }

                    // Optional: Check file size (max 2MB)
                    if (std.AvatarFile.Length > 2 * 1024 * 1024)
                    {
                        ModelState.AddModelError("AvatarFile", "File size must be less than 2MB.");
                        return View(std);
                    }

                    using var ms = new MemoryStream();
                    await std.AvatarFile.CopyToAsync(ms);
                    std.Avatar = ms.ToArray();
                }

                // 3️⃣ Save student to database asynchronously
                await _dbContext.RegistrationTable.AddAsync(std);
                await _dbContext.SaveChangesAsync();

                // 4️⃣ Send confirmation email asynchronously
                await SendEmailAsync(std.reg_email, "Registration Successful", GenerateEmailBody(std));

                TempData["msg"] = "Registration successful!";
                return RedirectToAction("Register");
            }

            // If model validation fails, return the same view with errors
        //    return View(std);
        //}

        // Async email sending
        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("ScopeIndia", "vishnuramesh567@gmail.com"));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;
                message.Body = new TextPart("plain") { Text = body };

                using var smtpClient = new SmtpClient();
                await smtpClient.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                await smtpClient.AuthenticateAsync("vishnuramesh567@gmail.com", "cbyu irba msbs tfwf"); // Use Gmail App Password
                await smtpClient.SendAsync(message);
                await smtpClient.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Email sending failed: " + ex.Message);
            }
        }

        // Generate email body
        private string GenerateEmailBody(Student std)
        {
            return $"Hello {std.reg_first_name},\n\n" +
                   "Your registration has been successfully completed! Here are your details:\n\n" +
                   $"- Full Name: {std.reg_first_name}\n" +
                   $"- Email: {std.reg_email}\n" +
                   $"- Mobile: {std.reg_mobile_number}\n\n" +
                   "Thank you for registering!";
        }
        public IActionResult Login()
        {

            return View();
        }
        public IActionResult FirstTimeLogin ()
        {
            return View();
        }
        

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool keepMeLoggedIn = false)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.LoginError = "Email and password are required.";
                return View();
            }

            email = email.ToLower().Trim();

            var user = _dbContext.ChangePasswordTable
                .Where(u => u.Email != null && u.Password != null)
                .AsEnumerable()
                .FirstOrDefault(u => u.Email.ToLower().Trim() == email);

            var student = _dbContext.RegistrationTable
                .FirstOrDefault(s => s.reg_email.ToLower().Trim() == email);

            if (user == null || student == null || string.IsNullOrEmpty(user.Password))
            {
                ViewBag.LoginError = "Invalid email or password.";
                return View();
            }

            // Ensure hashed
            // if (user.Password.Length != 64 || !Regex.IsMatch(user.Password, "^[a-fA-F0-9]{64}$"))
            // {
            //    user.Password = HashPassword(user.Password ?? string.Empty);
            //    _dbContext.SaveChanges();
            // }

            bool isPasswordValid = false;
            bool needsMigration = false;

            try
            {
                if (BCrypt.Net.BCrypt.Verify(password, user.Password))
                {
                    isPasswordValid = true;
                }
            }
            catch (BCrypt.Net.SaltParseException)
            {
                // Handle legacy passwords (SHA-256 or Plain Text)
                if (user.Password == password) // Plain text check
                {
                    isPasswordValid = true;
                    needsMigration = true;
                }
                else
                {
                    // SHA-256 check
                    using (var sha256 = SHA256.Create())
                    {
                        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                        var builder = new StringBuilder();
                        foreach (var b in bytes)
                        {
                            builder.Append(b.ToString("x2"));
                        }
                        if (builder.ToString() == user.Password)
                        {
                            isPasswordValid = true;
                            needsMigration = true;
                        }
                    }
                }
            }

            if (!isPasswordValid)
            {
                ViewBag.LoginError = "Invalid email or password.";
                
                // Debug: Compute SHA256 of input to compare
                string computedHash = "";
                using (var sha256 = SHA256.Create())
                {
                    var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                    var builder = new StringBuilder();
                    foreach (var b in bytes) { builder.Append(b.ToString("x2")); }
                    computedHash = builder.ToString();
                }

                ViewBag.DebugHash = $"Stored: {user.Password} | Computed: {computedHash}"; 
                return View();
            }

            if (needsMigration)
            {
                user.Password = HashPassword(password);
                _dbContext.SaveChanges();
            }

            // ✅ Create claims
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, email),
        new Claim("StudentId", student.Id.ToString())
    };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = keepMeLoggedIn,
                ExpiresUtc = keepMeLoggedIn
                    ? DateTimeOffset.UtcNow.AddDays(30)
                    : DateTimeOffset.UtcNow.AddMinutes(30)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );

            return RedirectToAction("Dashboard");
        }
        [HttpPost]
        public IActionResult Email(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.EmailError = "Please enter a valid email address.";
                return View("FirstTimeLogin");
            }

            var existingUser = _dbContext.ChangePasswordTable
                .FirstOrDefault(u => u.Email.ToLower().Trim() == email.ToLower().Trim());

            if (existingUser != null)
            {
                ViewBag.EmailError = "Email already exists. Please login instead.";
                return View("FirstTimeLogin");
            }

            var otp = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("OTP_Email", email);
            HttpContext.Session.SetString("OTP", otp);

            SendEmail(email, "Your OTP for First Time Login", $"Your OTP is: {otp}");

            TempData["Message"] = "OTP sent to your email address.";
            return RedirectToAction("VerifyOtp");
        }

        public IActionResult VerifyOtp() => View();

        [HttpPost]
        public IActionResult VerifyOtp(string otpInput)
        {
            var sessionOtp = HttpContext.Session.GetString("OTP");
            var sessionEmail = HttpContext.Session.GetString("OTP_Email");

            if (string.IsNullOrEmpty(sessionOtp) || string.IsNullOrEmpty(sessionEmail))
            {
                ViewBag.Error = "Session expired. Please request a new OTP.";
                return RedirectToAction("FirstTimeLogin");
            }

            if (sessionOtp.Trim() == otpInput?.Trim())
            {
                HttpContext.Session.SetString("PasswordSetupEmail", sessionEmail);
                return RedirectToAction("PasswordSetup");
            }
            else
            {
                ViewBag.Error = "Invalid OTP. Please try again.";
                return View();
            }
        }

        public IActionResult PasswordSetup()
        {
            var email = HttpContext.Session.GetString("PasswordSetupEmail");
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("FirstTimeLogin");
            }
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        public IActionResult PasswordSetup(string password, string confirmPassword)
        {
            var email = HttpContext.Session.GetString("OTP_Email");

            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                ViewBag.Error = "Please fill in both password fields.";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View();
            }

            var changePasswordEntry = new ChangePassword { Email = email, Password = HashPassword(password) };
            _dbContext.ChangePasswordTable.Add(changePasswordEntry);
            _dbContext.SaveChanges();

            TempData["Message"] = "Password set successfully. Please log in.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult VerifyEmail()
        {
            return View();
        }
        [HttpPost]
        public IActionResult VerifyEmail(VerifyEmailViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = _dbContext.RegistrationTable
                .FirstOrDefault(u => u.reg_email.ToLower().Trim() == model.Email.ToLower().Trim());

            if (user == null)
            {
                ModelState.AddModelError("", "Email not found.");
                return View(model);
            }

            HttpContext.Session.SetString("VerifyEmail", user.reg_email);

            string otp = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("VerifyEmailOtp", otp);

            SendEmail(user.reg_email, "Password Reset OTP", $"Your OTP is: {otp}");

            TempData["Message"] = "OTP sent to your email address.";
            return RedirectToAction("VerifyOtpForEmail");
        }


        public IActionResult VerifyOtpForEmail() => View();

        [HttpPost]
        public IActionResult VerifyOtpForEmail(string otpInput)
        {
            var sessionOtp = HttpContext.Session.GetString("VerifyEmailOtp");
            var email = HttpContext.Session.GetString("VerifyEmail");

            if (string.IsNullOrEmpty(sessionOtp) || string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Session expired. Please try again.";
                return RedirectToAction("VerifyEmail");
            }

            if (sessionOtp.Trim() == otpInput?.Trim())
            {
                return RedirectToAction("ChangePassword", new { username = email });
            }
            else
            {
                ViewBag.Error = "Invalid OTP. Please try again.";
                return View();
            }
        }

        public IActionResult ChangePassword(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("VerifyEmail");
            }

            return View(new ChangePasswordViewModel { Email = username });
        }

        [HttpPost]
        public IActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _dbContext.ChangePasswordTable
                    .FirstOrDefault(cp => cp.Email.ToLower().Trim() == model.Email.ToLower().Trim());

                if (user != null)
                {
                    user.Password = HashPassword(model.NewPassword);
                    _dbContext.SaveChanges();

                    TempData["Message"] = "Password updated successfully. Please log in.";
                    return RedirectToAction("Login");
                }
                else
                {
                    ModelState.AddModelError("", "Email not found!");
                    return View(model);
                }
            }

            ModelState.AddModelError("", "Something went wrong. Try again.");
            return View(model);
        }
        private void SendEmail(string toEmail, string subject, string body)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse("vishnuramesh567@gmail.com"));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;
                message.Body = new TextPart(TextFormat.Text) { Text = body };

                using var smtp = new SmtpClient();
                smtp.Connect("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                smtp.Authenticate("vishnuramesh567@gmail.com", "cbyu irba msbs tfwf");
                smtp.Send(message);
                smtp.Disconnect(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Email Error: " + ex.Message);
            }
        }
        [Authorize]
        [HttpGet]
        public IActionResult EditProfile()
        {
            var studentIdClaim = User.FindFirst("StudentId")?.Value;

            if (string.IsNullOrEmpty(studentIdClaim) || !int.TryParse(studentIdClaim, out int studentId))
            {
                return RedirectToAction("Login");
            }

            var student = _dbContext.RegistrationTable.FirstOrDefault(s => s.Id == studentId);

            if (student == null) return NotFound();

            return View(student);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProfile(Student updatedStudent, IFormFile avatarFile, string[] Hobbies)
        {
            var existingStudent = _dbContext.RegistrationTable.FirstOrDefault(s => s.Id == updatedStudent.Id);

            if (existingStudent == null) return NotFound();

            existingStudent.reg_first_name = updatedStudent.reg_first_name;
            existingStudent.reg_last_name = updatedStudent.reg_last_name;
            existingStudent.Gender = updatedStudent.Gender;
            existingStudent.reg_date_of_birth = updatedStudent.reg_date_of_birth;
            existingStudent.reg_mobile_number = updatedStudent.reg_mobile_number;
            existingStudent.reg_country = updatedStudent.reg_country;
            existingStudent.reg_state = updatedStudent.reg_state;
            existingStudent.reg_city = updatedStudent.reg_city;
            existingStudent.reg_hobbies = Hobbies != null && Hobbies.Length > 0 ? string.Join(",", Hobbies) : "";

            if (avatarFile != null && avatarFile.Length > 0)
            {
                using var ms = new MemoryStream();
                avatarFile.CopyTo(ms);
                existingStudent.Avatar = ms.ToArray(); // byte[]
            }


            _dbContext.SaveChanges();
            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("EditProfile");
        }
        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
        [Authorize]
        public IActionResult Dashboard()
        {
            var email = User.Identity.Name;
            var student = _dbContext.RegistrationTable.FirstOrDefault(s => s.reg_email == email);
            return View(student);
        }
        public IActionResult SwitchPassword() => View();
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SwitchPassword(string OldPassword, string NewPassword, string ConfirmPassword)
        {
            var email = User.Identity?.Name;

            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "You must be logged in to change your password.";
                return RedirectToAction("Login");
            }

            var user = _dbContext.ChangePasswordTable.FirstOrDefault(u => u.Email == email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(OldPassword, user.Password))
            {
                TempData["ErrorMessage"] = "Old password is incorrect.";
                return View("SwitchPassword");
            }

            if (NewPassword != ConfirmPassword)
            {
                TempData["ErrorMessage"] = "New password and confirm password do not match.";
                return View("SwitchPassword");
            }

            user.Password = HashPassword(NewPassword);
            _dbContext.SaveChanges();

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["SuccessMessage"] = "Password changed successfully! Please log in again.";
            return RedirectToAction("Login");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        

       

    }

}
