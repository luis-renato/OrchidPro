using Supabase;
using Supabase.Gotrue;
using OrchidPro.Config;

namespace OrchidPro.Services.Data;

public class SupabaseService
{
    public Supabase.Client? Client { get; private set; }

    public async Task InitializeAsync()
    {
        var options = new SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = true
        };

        Client = new Supabase.Client(AppSettings.SupabaseUrl, AppSettings.SupabaseAnonKey, options);
        await Client.InitializeAsync();
    }

    public async Task<bool> RestoreSessionAsync()
    {
        var sessionJson = Preferences.Get("supabase_session", null);
        if (string.IsNullOrEmpty(sessionJson))
            return false;

        try
        {
            var session = System.Text.Json.JsonSerializer.Deserialize<Session>(sessionJson);

            if (session == null ||
                string.IsNullOrEmpty(session.AccessToken) ||
                string.IsNullOrEmpty(session.RefreshToken) ||
                Client == null)
            {
                return false;
            }

            await Client.Auth.SetSession(session.AccessToken, session.RefreshToken);

            var user = await Client.Auth.GetUser(session.AccessToken);
            return user != null;
        }
        catch
        {
            return false;
        }
    }

    public void SaveSession()
    {
        var session = Client?.Auth.CurrentSession;
        if (session != null)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(session);
            Preferences.Set("supabase_session", json);
        }
    }

    public void Logout()
    {
        Preferences.Remove("supabase_session");
        Client?.Auth.SignOut();
    }

    // ADIÇÕES PARA SYNC - sem alterar login existente
    public bool IsInitialized => Client != null;

    public bool IsAuthenticated => Client?.Auth?.CurrentUser != null;

    public string? GetCurrentUserId() => Client?.Auth?.CurrentUser?.Id;

    public User? GetCurrentUser() => Client?.Auth?.CurrentUser;

    // Teste de conectividade específico para sincronização
    public async Task<bool> TestSyncConnectionAsync()
    {
        try
        {
            if (Client == null) return false;

            // Teste simples de conectividade
            var result = await Client.Rpc("now", new Dictionary<string, object>());
            return true;
        }
        catch
        {
            return false;
        }
    }
}