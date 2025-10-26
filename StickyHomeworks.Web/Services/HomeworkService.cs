using Grpc.Core;
using StickyHomeworks.Core.Entities;
using StickyHomeworks.Web.Protos;
using Microsoft.EntityFrameworkCore;
using StickyHomeworks.Core.Context;

namespace StickyHomeworks.Web.Services;

public class HomeworkService : Protos.HomeworkService.HomeworkServiceBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<HomeworkService> _logger;

    public HomeworkService(AppDbContext context, ILogger<HomeworkService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public override async Task<HomeworkListReply> GetAllHomeworks(Empty request, ServerCallContext context)
    {
        try
        {
            var homeworks = await _context.Homeworks.ToListAsync();
            var reply = new HomeworkListReply { Success = true };

            foreach (var homework in homeworks)
            {
                reply.Homeworks.Add(new HomeworkModel
                {
                    Id = homework.Id,
                    Subject = homework.Subject ?? "",
                    Tags = homework.Tags ?? "",
                    Content = homework.Content ?? "",
                    EndTime = homework.EndTime?.ToString("o") ?? ""
                });
            }

            return reply;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all homeworks");
            return new HomeworkListReply
            {
                Success = false,
                Message = "Failed to retrieve homeworks"
            };
        }
    }

    public override async Task<HomeworkReply> GetHomework(GetHomeworkRequest request, ServerCallContext context)
    {
        try
        {
            var homework = await _context.Homeworks.FindAsync(request.Id);
            if (homework == null)
            {
                return new HomeworkReply
                {
                    Success = false,
                    Message = "Homework not found"
                };
            }

            return new HomeworkReply
            {
                Success = true,
                Homework = new HomeworkModel
                {
                    Id = homework.Id,
                    Subject = homework.Subject ?? "",
                    Tags = homework.Tags ?? "",
                    Content = homework.Content ?? "",
                    EndTime = homework.EndTime?.ToString("o") ?? ""
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting homework with id {Id}", request.Id);
            return new HomeworkReply
            {
                Success = false,
                Message = "Failed to retrieve homework"
            };
        }
    }

    public override async Task<HomeworkReply> CreateHomework(CreateHomeworkRequest request, ServerCallContext context)
    {
        try
        {
            var homework = new Homework
            {
                Subject = request.Subject,
                Tags = request.Tags,
                Content = request.Content,
                EndTime = string.IsNullOrEmpty(request.EndTime) ? null : DateTime.Parse(request.EndTime)
            };

            _context.Homeworks.Add(homework);
            await _context.SaveChangesAsync();

            return new HomeworkReply
            {
                Success = true,
                Message = "Homework created successfully",
                Homework = new HomeworkModel
                {
                    Id = homework.Id,
                    Subject = homework.Subject ?? "",
                    Tags = homework.Tags ?? "",
                    Content = homework.Content ?? "",
                    EndTime = homework.EndTime?.ToString("o") ?? ""
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating homework");
            return new HomeworkReply
            {
                Success = false,
                Message = "Failed to create homework"
            };
        }
    }

    public override async Task<HomeworkReply> UpdateHomework(UpdateHomeworkRequest request, ServerCallContext context)
    {
        try
        {
            var homework = await _context.Homeworks.FindAsync(request.Id);
            if (homework == null)
            {
                return new HomeworkReply
                {
                    Success = false,
                    Message = "Homework not found"
                };
            }

            homework.Subject = request.Subject;
            homework.Tags = request.Tags;
            homework.Content = request.Content;
            homework.EndTime = string.IsNullOrEmpty(request.EndTime) ? null : DateTime.Parse(request.EndTime);

            await _context.SaveChangesAsync();

            return new HomeworkReply
            {
                Success = true,
                Message = "Homework updated successfully",
                Homework = new HomeworkModel
                {
                    Id = homework.Id,
                    Subject = homework.Subject ?? "",
                    Tags = homework.Tags ?? "",
                    Content = homework.Content ?? "",
                    EndTime = homework.EndTime?.ToString("o") ?? ""
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating homework with id {Id}", request.Id);
            return new HomeworkReply
            {
                Success = false,
                Message = "Failed to update homework"
            };
        }
    }

    public override async Task<OperationReply> DeleteHomework(DeleteHomeworkRequest request, ServerCallContext context)
    {
        try
        {
            var homework = await _context.Homeworks.FindAsync(request.Id);
            if (homework == null)
            {
                return new OperationReply
                {
                    Success = false,
                    Message = "Homework not found"
                };
            }

            _context.Homeworks.Remove(homework);
            await _context.SaveChangesAsync();

            return new OperationReply
            {
                Success = true,
                Message = "Homework deleted successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting homework with id {Id}", request.Id);
            return new OperationReply
            {
                Success = false,
                Message = "Failed to delete homework"
            };
        }
    }
}