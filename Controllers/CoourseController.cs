using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScopeIndiaWebsite.Data;
using ScopeIndiaWebsite.Models;
using System.Linq;
using System.Security.Claims;

public class CoourseController : Controller
{
    private readonly MVCDbContext _context;

    public CoourseController(MVCDbContext context)
    {
        _context = context;
    }

    // Public - anyone can see courses
    [AllowAnonymous]
    public IActionResult Index(string searchTerm)
    {
        var courses = _context.Courses
            .Where(c => string.IsNullOrEmpty(searchTerm) || c.CourseName.Contains(searchTerm))
            .ToList();

        return View(courses);
    }

    // Only logged-in users can sign up
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public IActionResult SignUp(int courseId)
    {
        var studentIdClaim = User.FindFirst("StudentId")?.Value;

        if (string.IsNullOrEmpty(studentIdClaim))
        {
            TempData["Error"] = "Please login to sign up for a course.";
            return RedirectToAction("Login", "ViewPages");
        }

        int studentId = int.Parse(studentIdClaim);

        var existing = _context.StudentCourses
            .FirstOrDefault(sc => sc.StudentId == studentId && sc.CourseId == courseId);

        if (existing == null)
        {
            var studentCourse = new StudentCourse
            {
                StudentId = studentId,
                CourseId = courseId
            };

            _context.StudentCourses.Add(studentCourse);
            _context.SaveChanges();

            TempData["Success"] = "Successfully signed up for the course!";
        }
        else
        {
            TempData["Info"] = "You have already signed up for this course.";
        }

        return RedirectToAction("Index");
    }

    // Only logged-in users can see their enrolled courses
    [Authorize]
    public IActionResult MyCourses()
    {
        var studentIdClaim = User.FindFirst("StudentId")?.Value;

        if (string.IsNullOrEmpty(studentIdClaim))
        {
            TempData["Error"] = "Please login to view your courses.";
            return RedirectToAction("Login", "ViewPages");
        }

        int studentId = int.Parse(studentIdClaim);

        var myCourses = _context.StudentCourses
            .Where(sc => sc.StudentId == studentId)
            .Select(sc => sc.Course)
            .ToList();

        return View(myCourses);
    }
}
