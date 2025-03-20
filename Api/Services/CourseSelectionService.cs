using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class CourseSelectionService
    {
        private readonly IRedisService _redisService;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<CourseSelectionService> _logger;
        private const string LockKeyPrefix = "lock:course:";
        private static readonly TimeSpan LockExpiry = TimeSpan.FromSeconds(10);

        public CourseSelectionService(
            IRedisService redisService,
            ApplicationDbContext dbContext,
            ILogger<CourseSelectionService> logger)
        {
            _redisService = redisService;
            _dbContext = dbContext;
            _logger = logger;
        }

        // 初始化课程库存到Redis
        public async Task InitializeCourseStockAsync(int courseId)
        {
            var course = await _dbContext.Courses.FindAsync(courseId);
            if (course != null)
            {
                await _redisService.SetCourseStockAsync(courseId, course.AvailableSeats);
                _logger.LogInformation($"课程 {courseId} 的库存已初始化为 {course.AvailableSeats}");
            }
        }

        // 尝试选课
        public async Task<(bool Success, string Message)> TrySelectCourseAsync(int studentId, int courseId)
        {
            // 检查课程是否存在
            var course = await _dbContext.Courses.FindAsync(courseId);
            if (course == null)
            {
                return (false, "课程不存在");
            }

            // 检查课程是否在选课时间范围内
            if (DateTime.Now < course.SelectionStartTime || DateTime.Now > course.SelectionEndTime)
            {
                return (false, "不在选课时间范围内");
            }

            // 检查学生是否已选择此课程
            var existingSelection = await _dbContext.StudentCourses
                .AnyAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId);
            if (existingSelection)
            {
                return (false, "您已经选择了这门课程");
            }

            // 生成分布式锁的标识
            string lockKey = $"{LockKeyPrefix}{courseId}";
            string lockValue = Guid.NewGuid().ToString();

            try
            {
                // 尝试获取分布式锁
                if (await _redisService.AcquireLockAsync(lockKey, lockValue, LockExpiry))
                {
                    try
                    {
                        // 检查库存
                        var stock = await _redisService.GetCourseStockAsync(courseId);
                        if (stock <= 0)
                        {
                            return (false, "课程已满");
                        }

                        // 减少库存
                        var newStock = await _redisService.DecrementCourseStockAsync(courseId);
                        if (newStock < 0)
                        {
                            // 如果库存小于0，恢复库存
                            await _redisService.IncrementAsync($"course:{courseId}:stock");
                            return (false, "课程已满");
                        }

                        // 添加选课记录
                        var studentCourse = new StudentCourse
                        {
                            StudentId = studentId,
                            CourseId = courseId,
                            EnrollmentDate = DateTime.Now
                        };

                        _dbContext.StudentCourses.Add(studentCourse);
                        await _dbContext.SaveChangesAsync();

                        // 减少数据库中的课程可用座位数
                        course.AvailableSeats--;
                        await _dbContext.SaveChangesAsync();

                        _logger.LogInformation($"学生 {studentId} 成功选择了课程 {courseId}");
                        return (true, "选课成功");
                    }
                    finally
                    {
                        // 释放分布式锁
                        await _redisService.ReleaseLockAsync(lockKey, lockValue);
                    }
                }
                else
                {
                    return (false, "系统繁忙，请稍后再试");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"选课过程中发生错误: {ex.Message}");
                return (false, "选课过程中发生错误");
            }
        }

        // 取消选课
        public async Task<(bool Success, string Message)> CancelCourseSelectionAsync(int studentId, int courseId)
        {
            // 生成分布式锁的标识
            string lockKey = $"{LockKeyPrefix}{courseId}";
            string lockValue = Guid.NewGuid().ToString();

            try
            {
                // 尝试获取分布式锁
                if (await _redisService.AcquireLockAsync(lockKey, lockValue, LockExpiry))
                {
                    try
                    {
                        // 查找选课记录
                        var enrollment = await _dbContext.StudentCourses
                            .FirstOrDefaultAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId);

                        if (enrollment == null)
                        {
                            return (false, "未找到选课记录");
                        }

                        // 删除选课记录
                        _dbContext.StudentCourses.Remove(enrollment);
                        await _dbContext.SaveChangesAsync();

                        // 增加Redis中的库存
                        await _redisService.IncrementAsync($"course:{courseId}:stock");

                        // 增加数据库中的课程可用座位数
                        var course = await _dbContext.Courses.FindAsync(courseId);
                        if (course != null)
                        {
                            course.AvailableSeats++;
                            await _dbContext.SaveChangesAsync();
                        }

                        _logger.LogInformation($"学生 {studentId} 成功取消了课程 {courseId} 的选择");
                        return (true, "取消选课成功");
                    }
                    finally
                    {
                        // 释放分布式锁
                        await _redisService.ReleaseLockAsync(lockKey, lockValue);
                    }
                }
                else
                {
                    return (false, "系统繁忙，请稍后再试");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"取消选课过程中发生错误: {ex.Message}");
                return (false, "取消选课过程中发生错误");
            }
        }
    }