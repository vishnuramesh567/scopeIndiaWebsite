namespace ScopeIndiaWebsite.Models
{
    public class Course
    {
        public int Id { get; set; }
        public string CourseName { get; set; }
        public string Duration { get; set; }
        public decimal Fee { get; set; }
        public ICollection<StudentCourse> StudentCourses { get; set; }
    }

}
