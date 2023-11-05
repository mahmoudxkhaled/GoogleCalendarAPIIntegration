using Google.Apis.Calendar.v3.Data;
using GoogleCalendarEvents.API.Dto;
using GoogleCalendarEvents.API.Helper;
using GoogleCalendarEvents.API.Model;
using Microsoft.AspNetCore.Mvc;

namespace GoogleCalendarEvents.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class eventsController : ControllerBase
{
    private readonly ILogger<eventsController> _logger;

    public eventsController(ILogger<eventsController> logger)
    {
        _logger = logger;
    }

    #region CreateEvent

    [HttpPost]
    public async Task<ActionResult<GoogleCalendarEventDto>> CreateEvent(GoogleCalendarEvent newEvent)
    {

        try
        {

            if (string.IsNullOrEmpty(newEvent.Summary) || newEvent.Start == null || newEvent.End == null)
            {
                return BadRequest( "Required fields are missing or formatted incorrectly.");
            }

            if (IsEventInPast(newEvent) || IsEventOnWeekend(newEvent))
            {
                return BadRequest("Events cannot be created in the past or on weekends (Friday or Saturday).");
            }

            var googleCalendarEvent = await GoogleCalendarEventsManager.CreateGoogleCalendarEvent(newEvent);

            GoogleCalendarEventDto events = new GoogleCalendarEventDto
            {

                Id = googleCalendarEvent.Id,
                Summary = googleCalendarEvent.Summary,
                Description = googleCalendarEvent.Description,
                Location = googleCalendarEvent.Location,
                Start = (DateTime)googleCalendarEvent.Start.DateTime,
                End = (DateTime)googleCalendarEvent.End.DateTime,
                Attachments = googleCalendarEvent.Attachments?.Select(attachment => new EventAttachmentDto
                {
                    FileId = attachment.FileId,
                    FileUrl = attachment.FileUrl
                }).ToList(),
            };
            return CreatedAtAction("GetEvent", new { eventId = events.Id }, events);
        }
        catch (Exception ex)

        {
            return StatusCode(500, "An error occurred while creating the event: " + ex.Message);
        }

    }

    // Check if the event date is in the past
    private bool IsEventInPast(GoogleCalendarEvent Event)
    {
        return Event.End < DateTime.UtcNow;
    }

    // Check if the event start date falls on a Friday or Saturday
    private bool IsEventOnWeekend(GoogleCalendarEvent Event)
    {
        return Event.Start.DayOfWeek == DayOfWeek.Friday || Event.Start.DayOfWeek == DayOfWeek.Saturday;
    }

    #endregion

    #region GetEvent

    [Route("{eventId}")]
    [HttpGet]
    public async Task<ActionResult<GoogleCalendarEventDto>> GetEvent(string eventId)
    {
        try
        {
            var existingEvent = await GoogleCalendarEventsManager.GetGoogleCalendarEvent(eventId);

            if (existingEvent == null)
            {
                return NotFound();
            }

            var googleCalendarEventDto = new GoogleCalendarEventDto
            {
                Id = existingEvent.Id,
                Summary = existingEvent.Summary,
                Description = existingEvent.Description,
                Location = existingEvent.Location,
                Start = (DateTime)existingEvent.Start.DateTime,
                End = (DateTime)existingEvent.End.DateTime,
                Attachments = existingEvent.Attachments?.Select(attachment => new EventAttachmentDto
                {
                    FileId = attachment.FileId,
                    FileUrl = attachment.FileUrl
                }).ToList(),
            };

            return Ok(googleCalendarEventDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "An error occurred while fetching the event: " + ex.Message);
        }
    }

    #endregion

    #region DeleteEvent

    [Route("{eventId}")]
    [HttpDelete]
    public async Task<ActionResult> DeleteEvent(string eventId)
    {
        try
        {
            if (string.IsNullOrEmpty(eventId))
            {
                //if eventId is missing
                return BadRequest("eventId is required.");
            }

            string response = await GoogleCalendarEventsManager.DeleteGoogleCalendarEvent(eventId);

            if (response == "Event deleted successfully")
            {
                // Event was deleted successfully, return a 204 No Content status
                return NoContent();
            }
            else if (response == "This event already deleted or does not exist")
            {
                // Event was not found or was already deleted, return a 404 Not Found status
                return NotFound();
            }
            else
            {
                return StatusCode(500, response);
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, "An error occurred while deleting the event: " + ex.Message);
        }
    }


    #endregion

    #region GetAllEventsByFilters

    [HttpGet]
    public async Task<ActionResult<List<GoogleCalendarEventDto>>> GetAllEventsByFilters(
    DateTime? startDate,
    DateTime? endDate,
    string? searchQuery
    // int page = 1, // can set dynamic pages numbers by user request
    //int pageSize = 10) // can set dynamic page size by user request
        )
    {
        try
        {
            var allEvents = await GoogleCalendarEventsManager.GetGoogleCalendarEvents();

            var eventDtos = allEvents
                .Select(e => new GoogleCalendarEventDto
                {
                    Id = e.Id,
                    Summary = e.Summary,
                    Description = e.Description,
                    Location = e.Location,
                    Start = (DateTime)e.Start.DateTime,
                    End = (DateTime)e.End.DateTime,
                    Attachments = e.Attachments?.Select(attachment => new EventAttachmentDto
                    {
                        FileId = attachment.FileId,
                        FileUrl = attachment.FileUrl
                    }).ToList(),

                }).ToList();

            // Apply optional filtering based on query parameters
            if (startDate != null)
            {
                eventDtos = eventDtos.Where(e => e.Start >= startDate).ToList();
            }
            //optional
            if (endDate != null)
            {
                eventDtos = eventDtos.Where(e => e.End <= endDate).ToList();
            }
            //optional
            if (!string.IsNullOrEmpty(searchQuery))
            {
                eventDtos = eventDtos
                    .Where(e => e.Summary.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Pagination Handeling
            // i setted here Default values for page and pageSize
            int page = 1;
            int pageSize = 5;
            int totalEvents = eventDtos.Count;
            int totalPages = (int)Math.Ceiling((double)totalEvents / pageSize);

            eventDtos = eventDtos
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // I Return a paginated result with pagination metadata
            var result = new
            {
                TotalEvents = totalEvents,
                TotalPages = totalPages,
                CurrentPage = page,
                Events = eventDtos
            };

            if (eventDtos.Count == 0)
            {
                // I Return all events if no events match the filter criteria.
                return Ok(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "An error occurred while fetching events: " + ex.Message);
        }
    }

    #endregion

    #region SearchEvent

    [HttpGet("Search")]
    public async Task<ActionResult<List<GoogleCalendarEventDto>>> SearchEvent(string? summary, string? description, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var events = await GoogleCalendarEventsManager.SearchGoogleCalendarEvent(summary, description, startDate, endDate);

            if (events == null || events.Count == 0)
            {
                return NotFound();
            }

            var eventDtos = new List<GoogleCalendarEventDto>();

            foreach (var Event in events)
            {
                var eventDto = new GoogleCalendarEventDto
                {
                    Id = Event.Id,
                    Summary = Event.Summary,
                    Description = Event.Description,
                    Location = Event.Location,
                    Start = (DateTime)Event.Start.DateTime,
                    End = (DateTime)Event.End.DateTime,
                    Attachments = Event.Attachments?.Select(attachment => new EventAttachmentDto
                    {
                        FileId = attachment.FileId,
                        FileUrl = attachment.FileUrl
                    }).ToList(),
                };

                eventDtos.Add(eventDto);
            }

            return Ok(eventDtos);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "An error occurred while searching for events: " + ex.Message);
        }
    }


    #endregion

    #region UpdateEvent
    [Route("{eventId}")]
    [HttpPut]
    public async Task<ActionResult<GoogleCalendarEventDto>> UpdateEvent(GoogleCalendarEventDto updatedEvent, string eventId)
    {
        try
        {
            var existingEvent = await GoogleCalendarEventsManager.GetGoogleCalendarEvent(eventId);

            if (existingEvent == null)
            {
                return NotFound();
            }

            // Check if the updated event falls on a weekend or in the past
            if (IsEventOnWeekend(updatedEvent) || IsEventInPast(updatedEvent))
            {
                return BadRequest("Events cannot be created on weekends (Friday or Saturday) or in the past.");
            }

            var updatedEventDto = new GoogleCalendarEventDto
            {
                Id = existingEvent.Id,
                Summary = updatedEvent.Summary,
                Description = updatedEvent.Description,
                Location = updatedEvent.Location,
                Start = updatedEvent.Start,
                End = updatedEvent.End
            };
            var _updatedEvent = await GoogleCalendarEventsManager.UpdateGoogleCalendarEvent(updatedEvent, existingEvent, eventId);

            return Ok(updatedEventDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "An error occurred while updating the event: " + ex.Message);
        }
    }


    private bool IsEventInPast(GoogleCalendarEventDto Event)
    {
        return Event.End < DateTime.UtcNow;
    }

    private bool IsEventOnWeekend(GoogleCalendarEventDto Event)
    {
        return Event.Start.DayOfWeek == DayOfWeek.Friday || Event.Start.DayOfWeek == DayOfWeek.Saturday;
    }
    #endregion


    // Just for Test Google Drive API

    #region Google Drive Event API

    // I tried here to Integrate with google drive separately but there is something missing because errors
    // i need more time to search and read documentations or i will make it in next version if needed after your review


    //[Route("mycolud")]
    //[HttpGet]
    //public async Task<IActionResult> GetFile(string fileName)
    //{
    //    var client = StorageClient.Create();
    //    var stream = new MemoryStream();
    //    var obj = await client.DownloadObjectAsync("myclouddrivefortest", fileName, stream);
    //    var val = Encoding.UTF8.GetBytes("Hello World");
    //    Console.WriteLine(val);
    //    stream.Position = 0;

    //    return File(stream, obj.ContentType, obj.Name);
    //}

    //public class FileUpload
    //{
    //    public string Name { get; set; }
    //    public string Type { get; set; }
    //    public byte[] File { get; set; }
    //}

    //[HttpPost]
    //public async Task<IActionResult> AddFile([FromBody] FileUpload fileUpload)
    //{
    //    var client = StorageClient.Create();
    //    var obj = await client.UploadObjectAsync(
    //        "myclouddrivefortest",
    //        fileUpload.Name,
    //        fileUpload.Type,
    //        new MemoryStream(fileUpload.File));

    //    return Ok();
    //}

    #endregion
}













