using Microsoft.EntityFrameworkCore;
using ScopeIndiaWebsite.Models;

namespace ScopeIndiaWebsite.Data
{
    public class MVCDbContext : DbContext
    {
        public MVCDbContext(DbContextOptions<MVCDbContext> options) : base(options) { }

        public DbSet<Student> RegistrationTable { get; set; }
        public DbSet<ChangePassword> ChangePasswordTable { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<StudentCourse> StudentCourses { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Course>()
                .Property(c => c.Fee)
                .HasPrecision(10, 2); // decimal(10,2)
        }

    }

   
}
