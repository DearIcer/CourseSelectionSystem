namespace Api.Models;

public class Student
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string StudentNumber { get; set; }
    public string Email { get; set; }
    public ICollection<StudentCourse> EnrolledCourses { get; set; } = new List<StudentCourse>();
}