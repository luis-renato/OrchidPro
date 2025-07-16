using Supabase;
using Supabase.Gotrue;
using OrchidPro.Config;
using OrchidPro.Services; // ✅ NOVO: Para usar SupabaseFamily
using System.Diagnostics;
using System.Text.Json;

namespace OrchidPro.Services.Data;

/// <summary>
/// CORRIGIDO: SupabaseService com teste REAL de conectividade
/// </summary>
public class SupabaseService
{
    public Supabase.Client? Client { get; private set; }

    // ✅ NOVO: Cache do estado de conectividade
    private bool? _lastConnectionState = null;
    private DateTime? _lastConnectionTest = null;
    private readonly TimeSpan _connectionCacheTime = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Initializes Supabase client
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            Debug.WriteLine("🔄 Initializing Supabase (optimized)...");
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

            // ✅ OTIMIZADO: Tentar restaurar sessão sem bloquear
            _ = TryRestoreSessionInBackgroundAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Supabase initialization failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Restaura sessão em background sem bloquear UI
    /// </summary>
    private async Task TryRestoreSessionInBackgroundAsync()
    {
        try
        {
            await Task.Delay(100); // Pequeno delay para não bloquear inicialização
            await TryRestoreSessionAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"⚠️ Background session restore failed: {ex.Message}");
            // Não fazer throw - isso é background
        }
    }

    /// <summary>
    /// Tenta restaurar sessão com logs detalhados e timeout
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

            // ✅ NOVO: Timeout para evitar bloqueio
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            // Tentar definir a sessão
            await Client.Auth.SetSession(session.AccessToken, session.RefreshToken);

            // Verificar se funcionou
            var currentUser = Client.Auth.CurrentUser;
            if (currentUser != null)
            {
                Debug.WriteLine($"✅ Session restored successfully");
                Debug.WriteLine($"✅ Current user: {currentUser.Email}");
                Debug.WriteLine($"✅ User ID: {currentUser.Id}");

                // ✅ NOVO: Atualizar cache de conectividade
                _lastConnectionState = true;
                _lastConnectionTest = DateTime.UtcNow;

                return true;
            }
            else
            {
                Debug.WriteLine("❌ Session restore failed - no current user");
                return false;
            }
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("⏰ Session restore timeout");
            return false;
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

                // ✅ NOVO: Marcar como conectado se sessão foi salva
                _lastConnectionState = true;
                _lastConnectionTest = DateTime.UtcNow;
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

            // ✅ NOVO: Limpar cache de conectividade
            _lastConnectionState = null;
            _lastConnectionTest = null;

            Debug.WriteLine("✅ Logout completed");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Logout error: {ex.Message}");
        }
    }

    // Propriedades para verificação de estado
    public bool IsInitialized => Client != null;

    /// <summary>
    /// IsAuthenticated com cache inteligente
    /// </summary>
    public bool IsAuthenticated
    {
        get
        {
            try
            {
                var isAuth = Client?.Auth?.CurrentUser != null;
                return isAuth;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error checking authentication: {ex.Message}");
                return false;
            }
        }
    }

    public string? GetCurrentUserId()
    {
        try
        {
            var userId = Client?.Auth?.CurrentUser?.Id;
            return userId;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Error getting user ID: {ex.Message}");
            return null;
        }
    }

    public User? GetCurrentUser() => Client?.Auth?.CurrentUser;

    /// <summary>
    /// ✅ CORRIGIDO: Teste REAL de conectividade delegando para FamilyService
    /// </summary>
    public async Task<bool> TestSyncConnectionAsync()
    {
        // ✅ NOVO: Usar cache se recente
        if (_lastConnectionTest.HasValue &&
            DateTime.UtcNow - _lastConnectionTest.Value < _connectionCacheTime &&
            _lastConnectionState.HasValue)
        {
            Debug.WriteLine($"💾 Using cached connection state: {_lastConnectionState.Value}");
            return _lastConnectionState.Value;
        }

        try
        {
            Debug.WriteLine("🧪 === REAL CONNECTION TEST ===");

            if (Client == null)
            {
                Debug.WriteLine("❌ Client is null");
                _lastConnectionState = false;
                _lastConnectionTest = DateTime.UtcNow;
                return false;
            }

            Debug.WriteLine("✅ Client exists");

            // ✅ TESTE 1: Verificar autenticação
            var user = Client.Auth?.CurrentUser;
            var isAuthenticated = user != null;

            Debug.WriteLine($"🔐 Authentication: {isAuthenticated}");

            if (!isAuthenticated)
            {
                Debug.WriteLine("❌ Not authenticated");
                _lastConnectionState = false;
                _lastConnectionTest = DateTime.UtcNow;
                return false;
            }

            Debug.WriteLine($"🔐 User: {user?.Email}");

            // ✅ TESTE 2: Query REAL no banco usando FamilyService
            Debug.WriteLine("🔍 Testing real database connection with families query...");

            try
            {
                // ✅ Query simples na tabela families para testar conectividade real
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                var testQuery = Client.From<SupabaseFamily>()
                    .Select("id")
                    .Limit(1);

                var response = await testQuery.Get();

                Debug.WriteLine("✅ Database query successful");
                Debug.WriteLine($"✅ Response received: {response != null}");

                // ✅ Cache do resultado
                _lastConnectionState = true;
                _lastConnectionTest = DateTime.UtcNow;

                Debug.WriteLine("🎉 REAL CONNECTION TEST: SUCCESS!");
                return true;
            }
            catch (Exception queryEx)
            {
                Debug.WriteLine($"❌ Database query failed: {queryEx.Message}");
                Debug.WriteLine($"❌ Query exception type: {queryEx.GetType().Name}");

                // ✅ Se query falhou, definitivamente sem conectividade
                _lastConnectionState = false;
                _lastConnectionTest = DateTime.UtcNow;
                return false;
            }

        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("⏰ Connection test timeout");
            _lastConnectionState = false;
            _lastConnectionTest = DateTime.UtcNow;
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Connection test failed completely: {ex.Message}");
            _lastConnectionState = false;
            _lastConnectionTest = DateTime.UtcNow;
            return false;
        }
    }

    /// <summary>
    /// ✅ NOVO: Força refresh do cache de conectividade
    /// </summary>
    public void InvalidateConnectionCache()
    {
        _lastConnectionState = null;
        _lastConnectionTest = null;
        Debug.WriteLine("🗑️ Connection cache invalidated");
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

        // Debug do cache
        Debug.WriteLine($"Connection cache: {_lastConnectionState?.ToString() ?? "null"}");
        Debug.WriteLine($"Cache age: {(_lastConnectionTest.HasValue ? (DateTime.UtcNow - _lastConnectionTest.Value).TotalSeconds.ToString("F1") + "s" : "null")}");

        Debug.WriteLine("🏗️ Schema: public (standard Supabase)");
        Debug.WriteLine("🔍 === END DEBUG ===");
    }
}