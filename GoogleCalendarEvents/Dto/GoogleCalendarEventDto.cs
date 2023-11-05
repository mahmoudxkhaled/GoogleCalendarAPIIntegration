namespace GoogleCalendarEvents.API.Dto;

public class GoogleCalendarEventDto
{
    public string Id { get; set; }
    public string Summary { get; set; } 
    public string Description { get; set; } 
    public string Location { get; set; }
    public DateTime Start { get; set; } 
    public DateTime End { get; set; }
   public IEnumerable<EventAttachmentDto>? Attachments { get; set; }


}
    public class EventAttachmentDto
    {
        public string FileId { get; set; }
        public string FileUrl { get; set; }

    }
