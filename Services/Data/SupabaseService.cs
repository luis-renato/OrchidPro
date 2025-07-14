using Supabase;
using Supabase.Gotrue;
using OrchidPro.Config;
using System.Diagnostics;
using System.Text.Json;

namespace OrchidPro.Services.Data;

/// <summary>
/// SupabaseService - SIMPLIFICADO para schema public (padrão Supabase)
/// </summary>
public class SupabaseService
{
    public Supabase.Client? Client { get; private set; }

    /// <summary>
    /// Initializes Supabase client - SIMPLIFICADO para schema public
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            Debug.WriteLine("🔄 Initializing Supabase (public schema)...");
            Debug.WriteLine($"🔗 URL: {AppSettings.SupabaseUrl}");
            Debug.WriteLine($"🔑 Key: {AppSettings.SupabaseAnonKey[..20]}...");

            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false // Desabilitar realtime para performance
            };

            Client = new Supabase.Client(AppSettings.SupabaseUrl, AppSettings.SupabaseAnonKey, options);
            await Client.InitializeAsync();

            Debug.WriteLine("✅ Supabase client initialized successfully");
            Debug.WriteLine("🏗️ Using standard public schema (no special configuration needed)");

            // Tentar restaurar sessão existente automaticamente
            await TryRestoreSessionAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Supabase initialization failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Tenta restaurar sessão com logs detalhados
    /// </summary>
    private async Task<bool> TryRestoreSessionAsync()
    {
        try
        {
            Debug.WriteLine("🔄 Attempting to restore session...");

            var sessionJson = Preferences.Get("supabase_session", null);
            if (string.IsNullOrEmpty(sessionJson))
            {
                Debug.WriteLine("❌ No saved session found");
                return false;
            }

            Debug.WriteLine("📱 Found saved session, attempting restore...");

            var session = JsonSerializer.Deserialize<Session>(sessionJson);
            if (session == null)
            {
                Debug.WriteLine("❌ Failed to deserialize session");
                return false;
            }

            if (string.IsNullOrEmpty(session.AccessToken) || string.IsNullOrEmpty(session.RefreshToken))
            {
                Debug.WriteLine("❌ Session missing tokens");
                return false;
            }

            if (Client == null)
            {
                Debug.WriteLine("❌ Client not initialized");
                return false;
            }

            Debug.WriteLine($"🔑 Restoring session for user: {session.User?.Email}");

            // Tentar definir a sessão
            await Client.Auth.SetSession(session.AccessToken, session.RefreshToken);

            // Verificar se funcionou
            var currentUser = Client.Auth.CurrentUser;
            if (currentUser != null)
            {
                Debug.WriteLine($"✅ Session restored successfully");
                Debug.WriteLine($"✅ Current user: {currentUser.Email}");
                Debug.WriteLine($"✅ User ID: {currentUser.Id}");
                return true;
            }
            else
            {
                Debug.WriteLine("❌ Session restore failed - no current user");
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Session restore error: {ex.Message}");

            // Limpar sessão corrompida
            Preferences.Remove("supabase_session");
            return false;
        }
    }

    /// <summary>
    /// Restaura sessão (método público)
    /// </summary>
    public async Task<bool> RestoreSessionAsync()
    {
        return await TryRestoreSessionAsync();
    }

    /// <summary>
    /// Salva sessão com logs detalhados
    /// </summary>
    public void SaveSession()
    {
        try
        {
            var session = Client?.Auth.CurrentSession;
            if (session != null)
            {
                Debug.WriteLine("💾 Saving session...");
                Debug.WriteLine($"💾 User: {session.User?.Email}");
                Debug.WriteLine($"💾 User ID: {session.User?.Id}");
                Debug.WriteLine($"💾 Access Token: {session.AccessToken?[..20]}...");
                Debug.WriteLine($"💾 Expires at: {session.ExpiresAt}");

                var json = JsonSerializer.Serialize(session);
                Preferences.Set("supabase_session", json);

                Debug.WriteLine("✅ Session saved successfully");

                // Verificar se foi salvo
                var saved = Preferences.Get("supabase_session", null);
                Debug.WriteLine($"✅ Verification: {(!string.IsNullOrEmpty(saved) ? "SUCCESS" : "FAILED")}");
            }
            else
            {
                Debug.WriteLine("❌ No session to save");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Error saving session: {ex.Message}");
        }
    }

    /// <summary>
    /// Logout com logs
    /// </summary>
    public void Logout()
    {
        try
        {
            Debug.WriteLine("🚪 Logging out...");

            Preferences.Remove("supabase_session");
            Client?.Auth.SignOut();

            Debug.WriteLine("✅ Logout completed");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Logout error: {ex.Message}");
        }
    }

    // Propriedades para verificação de estado
    public bool IsInitialized => Client != null;

    public bool IsAuthenticated
    {
        get
        {
            var isAuth = Client?.Auth?.CurrentUser != null;
            Debug.WriteLine($"🔐 IsAuthenticated check: {isAuth}");
            if (isAuth)
            {
                Debug.WriteLine($"🔐 Current user: {Client?.Auth?.CurrentUser?.Email}");
            }
            return isAuth;
        }
    }

    public string? GetCurrentUserId()
    {
        var userId = Client?.Auth?.CurrentUser?.Id;
        Debug.WriteLine($"🆔 GetCurrentUserId: {userId ?? "null"}");
        return userId;
    }

    public User? GetCurrentUser() => Client?.Auth?.CurrentUser;

    /// <summary>
    /// Teste de conectividade com public.families
    /// </summary>
    public async Task<bool> TestSyncConnectionAsync()
    {
        try
        {
            Debug.WriteLine("🧪 === CONNECTION TEST (PUBLIC SCHEMA) ===");

            if (Client == null)
            {
                Debug.WriteLine("❌ Client is null");
                return false;
            }

            Debug.WriteLine("✅ Client exists");

            // Teste de autenticação
            try
            {
                Debug.WriteLine("🧪 Test 1: Auth status...");
                var user = Client.Auth?.CurrentUser;
                Debug.WriteLine($"🔐 Auth user exists: {user != null}");
                Debug.WriteLine($"🔐 Auth user email: {user?.Email ?? "null"}");
                Debug.WriteLine($"🔐 Auth user ID: {user?.Id ?? "null"}");

                var session = Client.Auth?.CurrentSession;
                Debug.WriteLine($"🔐 Session exists: {session != null}");
                Debug.WriteLine($"🔐 Access token exists: {!string.IsNullOrEmpty(session?.AccessToken)}");
            }
            catch (Exception ex1)
            {
                Debug.WriteLine($"❌ Auth test failed: {ex1.Message}");
            }

            Debug.WriteLine("✅ Standard public schema - no special configuration needed");
            Debug.WriteLine("✅ Connection test completed successfully");

            return Client != null && IsAuthenticated;

        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Connection test failed completely: {ex.Message}");
            Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Debug completo do estado atual
    /// </summary>
    public void DebugCurrentState()
    {
        Debug.WriteLine("🔍 === SUPABASE STATE DEBUG ===");
        Debug.WriteLine($"Client initialized: {IsInitialized}");
        Debug.WriteLine($"User authenticated: {IsAuthenticated}");
        Debug.WriteLine($"Current user ID: {GetCurrentUserId() ?? "null"}");
        Debug.WriteLine($"Current user email: {GetCurrentUser()?.Email ?? "null"}");

        var session = Client?.Auth?.CurrentSession;
        if (session != null)
        {
            Debug.WriteLine($"Session expires at: {session.ExpiresAt}");
            Debug.WriteLine($"Access token present: {!string.IsNullOrEmpty(session.AccessToken)}");
            Debug.WriteLine($"Refresh token present: {!string.IsNullOrEmpty(session.RefreshToken)}");
        }
        else
        {
            Debug.WriteLine("No current session");
        }

        var savedSession = Preferences.Get("supabase_session", null);
        Debug.WriteLine($"Saved session present: {!string.IsNullOrEmpty(savedSession)}");

        Debug.WriteLine("🏗️ Schema: public (standard Supabase)");
        Debug.WriteLine("🔍 === END DEBUG ===");
    }
}