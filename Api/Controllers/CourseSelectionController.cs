using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class CourseSelectionController : ControllerBase
{
    private readonly CourseSelectionService _courseSelectionService;

    public CourseSelectionController(CourseSelectionService courseSelectionService)
    {
        _courseSelectionService = courseSelectionService;
    }

    // POST: api/CourseSelection/select
    [HttpPost("select")]
    public async Task<IActionResult> SelectCourse(int studentId, int courseId)
    {
        var result = await _courseSelectionService.TrySelectCourseAsync(studentId, courseId);
        if (result.Success)
        {
            return Ok(result.Message);
        }
        else
        {
            return BadRequest(result.Message);
        }
    }

    // POST: api/CourseSelection/cancel
    [HttpPost("cancel")]
    public async Task<IActionResult> CancelCourseSelection(int studentId, int courseId)
    {
        var result = await _courseSelectionService.CancelCourseSelectionAsync(studentId, courseId);
        if (result.Success)
        {
            return Ok(result.Message);
        }
        else
        {
            return BadRequest(result.Message);
        }
    }
}