using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using Google.Apis.Util;
using Google.Apis.Util.Store;
namespace GoogleCalendarEvents.API.Helper;

public static class GoogleCredintial
{
    public static CalendarService CreateCredintial()
    {

        string[] scopes = {    "https://www.googleapis.com/auth/calendar",
    "https://www.googleapis.com/auth/drive" };
        string applicationName = "Events";
        UserCredential credential;
        using (var stream = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "GoogleAuthention", "ClientCredintial.json"), FileMode.Open, FileAccess.Read))
        {
            string credentialPath = "token.json";
            credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
            GoogleClientSecrets.Load(stream).
               Secrets,
                scopes,
                "user",
                 CancellationToken.None,
                 new FileDataStore(credentialPath, true)).Result;

        }
        if (credential.Token.IsExpired(SystemClock.Default))
        {
            if (credential.RefreshTokenAsync(CancellationToken.None).Result)
            {
                // Token refresh is handled by the FileDataStore, so there's no need to manually save the token.
            }
            else
            {
                throw new Exception("Token refresh failed.");
            }
        }

        var services = new CalendarService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = applicationName,


        });

        return services;
    }
}

