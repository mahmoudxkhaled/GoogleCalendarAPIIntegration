using Google;
using Google.Apis.Calendar.v3.Data;
using GoogleCalendarEvents.API.Dto;
using GoogleCalendarEvents.API.Model;
using System.Net;

namespace GoogleCalendarEvents.API.Helper;

public class GoogleCalendarEventsManager
{

    #region Create Event

    public static async Task<Event> CreateGoogleCalendarEvent(GoogleCalendarEvent GoogleCalendarEventDto)
    {

        var services = GoogleCredintial.CreateCredintial();

        var newEvent = new Event();

        newEvent.Summary = GoogleCalendarEventDto.Summary;
        newEvent.Location = GoogleCalendarEventDto.Location;
        newEvent.Description = GoogleCalendarEventDto.Description;

        EventDateTime start = new EventDateTime();
        start.DateTime = Convert.ToDateTime(GoogleCalendarEventDto.Start);
        EventDateTime end = new EventDateTime();
        end.DateTime = Convert.ToDateTime(GoogleCalendarEventDto.End);
        newEvent.Start = start;
        newEvent.End = end;

        if (GoogleCalendarEventDto.Attachments != null)
        {
            newEvent.Attachments = GoogleCalendarEventDto.Attachments
                .Select(attachment => new EventAttachment
                {
                    FileId = attachment.FileId,
                    FileUrl = attachment.FileUrl,
                })
                .ToList();
        }

        var calendarId = "primary";

        var requestEvent = services.Events.Insert(newEvent, calendarId);

        var createdEvent = await requestEvent.ExecuteAsync();

        //I can send Email using send grids 
        //var LastEvent = await GetLastGoogleCalendarEvent();
        //NotificationsManager.SendEmail(LastEvent.Organizer.Email, LastEvent.Summary);
        return createdEvent;

    }
    #endregion

    #region Update Event

    public static async Task<Event> UpdateGoogleCalendarEvent(GoogleCalendarEventDto updatedEvent, Event eventRequested, string eventId)
    {

        var services = GoogleCredintial.CreateCredintial();

        eventRequested.Summary = updatedEvent?.Summary ?? eventRequested.Summary;
        eventRequested.Location = updatedEvent?.Location ?? eventRequested.Location;
        eventRequested.Start.DateTime = updatedEvent?.Start ?? eventRequested.Start.DateTime;
        eventRequested.End.DateTime = updatedEvent?.End ?? eventRequested.End.DateTime;
        eventRequested.Description = updatedEvent?.Description ?? eventRequested.Description;
        var eventUpdated = services.Events.Update(eventRequested, "primary", eventId);
        var requestUpdated = await eventUpdated.ExecuteAsync();
        return requestUpdated;


    }
    #endregion

    #region Delete Event

    public static async Task<string> DeleteGoogleCalendarEvent(string eventId)
    {
        var services = GoogleCredintial.CreateCredintial();
        string response;

        try
        {
            if (string.IsNullOrEmpty(eventId))
            {
                response = "eventId is required.";
            }
            else
            {
                await services.Events.Delete("primary", eventId).ExecuteAsync();
                response = "Event deleted successfully";
            }
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            response = "Event not found";
        }
        catch (GoogleApiException ex)
        {
            response = "An error occurred while deleting the event: " + ex.Message;
        }

        return response;
    }


    #endregion

    #region Get All Events

    public static async Task<List<Event>> GetGoogleCalendarEvents()
    {
        var services = GoogleCredintial.CreateCredintial();

        var allEvents = await services.Events.List("primary").ExecuteAsync();
        var selectedEvents = allEvents.Items.ToList();
        return selectedEvents;
    }

    #endregion

    #region Get Event

    public static async Task<Event> GetGoogleCalendarEvent(string eventId)
    {
        var services = GoogleCredintial.CreateCredintial();
        var allEvents = await GetGoogleCalendarEvents();
        var requestedEvent = allEvents.Where(e => e.Id == eventId).FirstOrDefault();
        if (requestedEvent == null)
        { return null!; }
            return requestedEvent;
    }
    #endregion

    #region Get Last Event

    public static async Task<Event> GetLastGoogleCalendarEvent()
    {
        var services = GoogleCredintial.CreateCredintial();

        var allEvents = await services.Events.List("primary").ExecuteAsync();
        var lastEventCreated = allEvents.Items.ToList().LastOrDefault();
        return lastEventCreated;
    }
    #endregion

    #region Search Event

    public static async Task<List<Event>> SearchGoogleCalendarEvent(string? summary = null, string? description = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        var services = GoogleCredintial.CreateCredintial();

        var request = services.Events.List("primary");

        if (startDate.HasValue)
        {
            request.TimeMin = startDate;
        }

        if (endDate.HasValue)
        {
            request.TimeMax = endDate;
        }

        var events = await request.ExecuteAsync();
        var selectedEvents = events.Items.ToList();

        if (!string.IsNullOrEmpty(summary))
        {
            selectedEvents = selectedEvents
                .Where(e => e.Summary?.Equals(summary, StringComparison.OrdinalIgnoreCase) ?? false)
                .ToList();
        }

        if (!string.IsNullOrEmpty(description))
        {
            selectedEvents = selectedEvents
                .Where(e => e.Description?.Equals(description, StringComparison.OrdinalIgnoreCase) ?? false)
                .ToList();
        }

        return selectedEvents;
    }
#endregion







}
