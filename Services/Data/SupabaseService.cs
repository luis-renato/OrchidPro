using Supabase;
using Supabase.Gotrue;
using OrchidPro.Config;
using OrchidPro.Extensions;
using OrchidPro.Services.Contracts;
using System.Diagnostics;
using System.Text.Json;
using SupabaseClient = Supabase.Client;
using OrchidPro.Services.Infrastructure.Supabase.Models;

namespace OrchidPro.Services.Data;

/// <summary>
/// Provides centralized Supabase backend connectivity with enterprise error handling and configuration management
/// </summary>
public class SupabaseService
{
    public SupabaseClient? Client { get; private set; }

    private bool? _lastConnectionState = null;
    private DateTime? _lastConnectionTest = null;
    private readonly TimeSpan _connectionCacheTime = TimeSpan.FromMinutes(1);

    #region Configuration Properties - FIXED CA1822

    /// <summary>
    /// Connection timeout configured from application settings
    /// PERFORMANCE OPTIMIZED: Static property for nonvirtual call sites
    /// </summary>
    public static TimeSpan ConnectionTimeout => TimeSpan.FromSeconds(AppSettings.NetworkTimeoutSeconds);

    /// <summary>
    /// Maximum retry attempts from application settings
    /// PERFORMANCE OPTIMIZED: Static property for nonvirtual call sites
    /// </summary>
    public static int MaxRetryAttempts => AppSettings.MaxRetryAttempts;

    /// <summary>
    /// Current connection status - instance-specific
    /// </summary>
    public bool IsConnected => Client is not null && IsInitialized;

    /// <summary>
    /// Environment information for debugging
    /// PERFORMANCE OPTIMIZED: Static property for nonvirtual call sites
    /// </summary>
    public static string Environment => AppSettings.Environment;

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes Supabase client with application configuration
    /// </summary>
    public async Task InitializeAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("Initializing Supabase with application configuration");
            this.LogInfo($"URL: {AppSettings.SupabaseUrl}");
            this.LogInfo($"Key: {AppSettings.SupabaseAnonKey[..20]}...");

            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = AppSettings.EnableRealTimeUpdates,
                Headers = new Dictionary<string, string>
                {
                    { "X-Client-Info", $"{AppSettings.ApplicationName}/{AppSettings.ApplicationVersion}" }
                }
            };

            Client = new SupabaseClient(AppSettings.SupabaseUrl, AppSettings.SupabaseAnonKey, options);
            await Client.InitializeAsync();

            this.LogSuccess("Supabase client initialized successfully");
            this.LogInfo($"Real-time updates: {AppSettings.EnableRealTimeUpdates}");
            this.LogInfo($"Connection timeout: {ConnectionTimeout.TotalSeconds}s");

            _ = TryRestoreSessionInBackgroundAsync();
        }, operationName: "InitializeSupabaseClient");
    }

    /// <summary>
    /// Restores user session in background without blocking UI
    /// </summary>
    private async Task TryRestoreSessionInBackgroundAsync()
    {
        try
        {
            await Task.Delay(100);
            await TryRestoreSessionAsync();
        }
        catch (Exception ex)
        {
            this.LogWarning($"Background session restore failed: {ex.Message}");
        }
    }

    #endregion

    #region Session Management

    /// <summary>
    /// Attempts to restore user session with detailed logging and timeout handling
    /// </summary>
    private async Task<bool> TryRestoreSessionAsync()
    {
        var result = await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("Attempting to restore session");

            var sessionJson = Preferences.Get("supabase_session", null);
            if (string.IsNullOrEmpty(sessionJson))
            {
                this.LogInfo("No saved session found");
                return false;
            }

            this.LogInfo("Found saved session, attempting restore");

            var session = JsonSerializer.Deserialize<Session>(sessionJson);
            if (session == null)
            {
                this.LogError("Failed to deserialize session");
                return false;
            }

            if (string.IsNullOrEmpty(session.AccessToken) || string.IsNullOrEmpty(session.RefreshToken))
            {
                this.LogError("Session missing tokens");
                return false;
            }

            if (Client == null)
            {
                this.LogError("Client not initialized");
                return false;
            }

            this.LogInfo($"Restoring session for user: {session.User?.Email}");

            using var cts = new CancellationTokenSource(ConnectionTimeout);

            await Client.Auth.SetSession(session.AccessToken, session.RefreshToken);

            var currentUser = Client.Auth.CurrentUser;
            if (currentUser != null)
            {
                this.LogSuccess("Session restored successfully");
                this.LogInfo($"Current user: {currentUser.Email}");
                this.LogInfo($"User ID: {currentUser.Id}");

                _lastConnectionState = true;
                _lastConnectionTest = DateTime.UtcNow;

                return true;
            }
            else
            {
                this.LogWarning("Session restore failed - no current user");
                return false;
            }
        }, operationName: "RestoreSession");

        return result;
    }

    /// <summary>
    /// Restores user session with public access
    /// </summary>
    public async Task<bool> RestoreSessionAsync()
    {
        return await TryRestoreSessionAsync();
    }

    /// <summary>
    /// Saves current user session with detailed logging
    /// </summary>
    public void SaveSession()
    {
        this.SafeExecute(() =>
        {
            var session = Client?.Auth.CurrentSession;
            if (session != null)
            {
                this.LogInfo("Saving session");
                this.LogInfo($"User: {session.User?.Email}");
                this.LogInfo($"User ID: {session.User?.Id}");
                this.LogInfo($"Access Token: {session.AccessToken?[..20]}...");
                this.LogInfo($"Expires at: {session.ExpiresAt}");

                var json = JsonSerializer.Serialize(session);
                Preferences.Set("supabase_session", json);

                this.LogSuccess("Session saved successfully");

                var saved = Preferences.Get("supabase_session", null);
                this.LogInfo($"Verification: {(!string.IsNullOrEmpty(saved) ? "SUCCESS" : "FAILED")}");

                _lastConnectionState = true;
                _lastConnectionTest = DateTime.UtcNow;
            }
            else
            {
                this.LogWarning("No session to save");
            }
        }, operationName: "SaveSession");
    }

    /// <summary>
    /// Logs out user with proper cleanup
    /// </summary>
    public void Logout()
    {
        this.SafeExecute(() =>
        {
            this.LogInfo("Logging out user");

            Preferences.Remove("supabase_session");
            Client?.Auth.SignOut();

            _lastConnectionState = null;
            _lastConnectionTest = null;

            this.LogSuccess("Logout completed");
        }, operationName: "Logout");
    }

    #endregion

    #region Connection Testing

    /// <summary>
    /// Tests real database connectivity with intelligent caching
    /// </summary>
    public async Task<bool> TestSyncConnectionAsync()
    {
        var result = await this.SafeNetworkExecuteAsync(async () =>
        {
            if (_lastConnectionTest.HasValue &&
                DateTime.UtcNow - _lastConnectionTest.Value < _connectionCacheTime &&
                _lastConnectionState.HasValue)
            {
                this.LogDebug($"Using cached connection state: {_lastConnectionState.Value}");
                return _lastConnectionState.Value;
            }

            this.LogInfo("Real connection test starting");

            if (Client == null)
            {
                this.LogError("Client is null");
                _lastConnectionState = false;
                _lastConnectionTest = DateTime.UtcNow;
                return false;
            }

            this.LogInfo("Client exists");

            var user = Client.Auth?.CurrentUser;
            var isAuthenticated = user != null;

            this.LogInfo($"Authentication: {isAuthenticated}");

            if (!isAuthenticated)
            {
                this.LogWarning("Not authenticated");
                _lastConnectionState = false;
                _lastConnectionTest = DateTime.UtcNow;
                return false;
            }

            this.LogInfo($"User: {user?.Email}");
            this.LogInfo("Testing real database connection with families query");

            try
            {
                using var cts = new CancellationTokenSource(ConnectionTimeout);

                var testQuery = Client.From<SupabaseFamily>()
                    .Select("id")
                    .Limit(1);

                var response = await testQuery.Get();

                this.LogSuccess("Database query successful");
                this.LogInfo($"Response received: {response != null}");

                _lastConnectionState = true;
                _lastConnectionTest = DateTime.UtcNow;

                this.LogSuccess("Real connection test successful");
                return true;
            }
            catch (Exception queryEx)
            {
                this.LogError(queryEx, "Database query failed");

                _lastConnectionState = false;
                _lastConnectionTest = DateTime.UtcNow;
                return false;
            }
        }, operationName: "TestSyncConnection");

        return result;
    }

    /// <summary>
    /// Forces refresh of connection cache
    /// </summary>
    public void InvalidateConnectionCache()
    {
        _lastConnectionState = null;
        _lastConnectionTest = null;
        this.LogInfo("Connection cache invalidated");
    }

    #endregion

    #region Status and Monitoring

    public bool IsInitialized => Client != null;

    /// <summary>
    /// Gets current user authentication status
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
                this.LogError(ex, "Error checking authentication");
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
            this.LogError(ex, "Error getting user ID");
            return null;
        }
    }

    public User? GetCurrentUser() => Client?.Auth?.CurrentUser;

    /// <summary>
    /// Gets comprehensive service status for monitoring
    /// </summary>
    public async Task<ServiceStatus> GetServiceStatusAsync()
    {
        var status = await this.SafeExecuteAsync(async () =>
        {
            var status = new ServiceStatus
            {
                IsInitialized = IsInitialized,
                HasClient = Client != null,
                Environment = Environment, // Using static property
                Version = AppSettings.ApplicationVersion,
                ConnectionTimeout = ConnectionTimeout, // Using static property
                MaxRetryAttempts = MaxRetryAttempts, // Using static property
                RealTimeEnabled = AppSettings.EnableRealTimeUpdates,
                LastChecked = DateTime.UtcNow
            };

            if (Client != null && IsInitialized)
            {
                status.IsConnected = await TestSyncConnectionAsync();
            }

            var session = Client?.Auth?.CurrentSession;
            status.HasActiveSession = session != null;
            status.SessionExpiresAt = session?.ExpiresAt();

            return status;
        }, operationName: "GetServiceStatus");

        return status ?? new ServiceStatus();
    }

    /// <summary>
    /// Outputs comprehensive service state for debugging
    /// </summary>
    public void DebugCurrentState()
    {
        this.SafeExecute(() =>
        {
            this.LogInfo("Supabase service state debug");
            this.LogInfo($"Environment: {Environment}"); // Using static property
            this.LogInfo($"Version: {AppSettings.ApplicationVersion}");
            this.LogInfo($"Client initialized: {IsInitialized}");
            this.LogInfo($"User authenticated: {IsAuthenticated}");
            this.LogInfo($"Current user ID: {GetCurrentUserId() ?? "null"}");
            this.LogInfo($"Current user email: {GetCurrentUser()?.Email ?? "null"}");

            var session = Client?.Auth?.CurrentSession;
            if (session != null)
            {
                this.LogInfo($"Session expires at: {session.ExpiresAt}");
                this.LogInfo($"Access token present: {!string.IsNullOrEmpty(session.AccessToken)}");
                this.LogInfo($"Refresh token present: {!string.IsNullOrEmpty(session.RefreshToken)}");
            }
            else
            {
                this.LogInfo("No current session");
            }

            var savedSession = Preferences.Get("supabase_session", null);
            this.LogInfo($"Saved session present: {!string.IsNullOrEmpty(savedSession)}");

            this.LogInfo($"Connection cache: {_lastConnectionState?.ToString() ?? "null"}");
            this.LogInfo("Cache age: " + (_lastConnectionTest.HasValue ? (DateTime.UtcNow - _lastConnectionTest.Value).TotalSeconds.ToString("F1") + "s" : "null"));

            this.LogInfo("Connection timeout: " + ConnectionTimeout.TotalSeconds + "s"); // Using static property
            this.LogInfo("Max retries: " + MaxRetryAttempts); // Using static property
            this.LogInfo("Real-time enabled: " + AppSettings.EnableRealTimeUpdates);
        }, operationName: "DebugCurrentState");
    }

    #endregion
}

#region Status Classes

/// <summary>
/// Service status information for monitoring and debugging
/// </summary>
public class ServiceStatus
{
    public bool IsInitialized { get; set; }
    public bool HasClient { get; set; }
    public bool IsConnected { get; set; }
    public bool HasActiveSession { get; set; }
    public DateTime? SessionExpiresAt { get; set; }
    public string Environment { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public TimeSpan ConnectionTimeout { get; set; }
    public int MaxRetryAttempts { get; set; }
    public bool RealTimeEnabled { get; set; }
    public DateTime LastChecked { get; set; }

    public override string ToString()
    {
        return $"Status: Init={IsInitialized}, Connected={IsConnected}, Session={HasActiveSession}, Env={Environment}";
    }
}

#endregion