using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using ScopeIndiaWebsite.Models;
using System.Diagnostics;

namespace ScopeIndiaWebsite.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }
        public IActionResult Contact()
        {
            return View();
        }
        [HttpGet]
        public IActionResult ContactUs()
        {
            return View();
        }
        [ValidateAntiForgeryToken]

        [HttpPost]
        public IActionResult ContactUs(Contact contact)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse("vishnuramesh567@gmail.com")); 
                message.To.Add(MailboxAddress.Parse(contact.Email));
                message.Subject = $"{contact.Subject}";
                message.Body = new TextPart() { Text = $"Hello {contact.Name}, I have a greeting message as {contact.Message}" };

                using var smtpclient = new SmtpClient();
                smtpclient.Connect("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                smtpclient.Authenticate("vishnuramesh567@gmail.com", "cbyu irba msbs tfwf");
                smtpclient.Send(message);
                smtpclient.Disconnect(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            // 👇 Send model directly to the view
            return View("viewContact", contact);
        }
        public IActionResult viewContact()
        {
            return View();
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
