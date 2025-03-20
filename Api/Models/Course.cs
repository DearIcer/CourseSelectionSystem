namespace Api.Models;

public class Course
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public DateTime SelectionStartTime { get; set; }
    public DateTime SelectionEndTime { get; set; }
    public bool IsActive { get; set; }
}