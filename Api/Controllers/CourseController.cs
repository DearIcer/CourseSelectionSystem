using Api.Data;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
    [Route("api/[controller]/[action]")]
    public class CourseController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly CourseSelectionService _courseSelectionService;
        private readonly IRedisService _redisService;

        public CourseController(
            ApplicationDbContext context,
            CourseSelectionService courseSelectionService,
            IRedisService redisService)
        {
            _context = context;
            _courseSelectionService = courseSelectionService;
            _redisService = redisService;
        }

        // GET: api/Course
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Course>>> GetCourses()
        {
            return await _context.Courses.ToListAsync();
        }

        // GET: api/Course/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Course>> GetCourse(int id)
        {
            var course = await _context.Courses.FindAsync(id);

            if (course == null)
            {
                return NotFound();
            }

            return course;
        }

        // POST: api/Course
        [HttpPost]
        public async Task<ActionResult<Course>> CreateCourse(Course course)
        {
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            // 初始化Redis中的课程库存
            await _courseSelectionService.InitializeCourseStockAsync(course.Id);

            return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, course);
        }

        // PUT: api/Course/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCourse(int id, Course course)
        {
            if (id != course.Id)
            {
                return BadRequest();
            }

            _context.Entry(course).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                
                // 更新Redis中的课程库存
                await _redisService.SetCourseStockAsync(id, course.AvailableSeats);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CourseExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Course/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            // 从Redis中删除课程库存
            await _redisService.RemoveKeyAsync($"course:{id}:stock");

            return NoContent();
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }

        // 初始化所有课程库存
        [HttpPost("initialize-stocks")]
        public async Task<IActionResult> InitializeAllCourseStocks()
        {
            var courses = await _context.Courses.ToListAsync();
            foreach (var course in courses)
            {
                await _courseSelectionService.InitializeCourseStockAsync(course.Id);
            }
            return Ok("所有课程库存已初始化");
        }
    }