namespace GoogleCalendarEvents.API.Model;

public class GoogleCalendarEvent
{
    public string Summary { get; set; } 
    public string Description { get; set; }
    public string Location { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public IEnumerable<EventAttachments>? Attachments { get; set; }

}
public class EventAttachments
{
    public string FileId { get; set; }
    public string FileUrl { get; set; }


}
