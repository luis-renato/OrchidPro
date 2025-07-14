using System.ComponentModel;
using OrchidPro.Services.Data;
using OrchidPro.Services;
using OrchidPro.Models;
using System.Diagnostics;

namespace OrchidPro
{
    /// <summary>
    /// Main page of the OrchidPro application.
    /// Provides dashboard functionality with quick actions and overview statistics.
    /// ADICIONADO: Testes de sincronização integrados
    /// </summary>
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        #region Private Fields

        /// <summary>
        /// Debug counter for development testing purposes
        /// </summary>
        private int _debugClickCount = 0;

        /// <summary>
        /// Total number of orchids in the collection
        /// </summary>
        private int _totalOrchidCount = 0;

        /// <summary>
        /// Number of healthy orchids in the collection
        /// </summary>
        private int _healthyOrchidCount = 0;

        /// <summary>
        /// Number of orchids that need attention
        /// </summary>
        private int _attentionNeededCount = 0;

        // ADICIONADO: Services para teste de sync
        private readonly SupabaseService _supabaseService;
        private readonly IFamilyRepository _familyRepository;
        private readonly SupabaseFamilySync _syncService;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the total number of orchids in the collection
        /// </summary>
        public int TotalOrchidCount
        {
            get => _totalOrchidCount;
            set
            {
                if (_totalOrchidCount != value)
                {
                    _totalOrchidCount = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the number of healthy orchids in the collection
        /// </summary>
        public int HealthyOrchidCount
        {
            get => _healthyOrchidCount;
            set
            {
                if (_healthyOrchidCount != value)
                {
                    _healthyOrchidCount = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the number of orchids that need attention
        /// </summary>
        public int AttentionNeededCount
        {
            get => _attentionNeededCount;
            set
            {
                if (_attentionNeededCount != value)
                {
                    _attentionNeededCount = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MainPage class
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;

            // ADICIONADO: Obter services para teste
            try
            {
                var services = MauiProgram.CreateMauiApp().Services;
                _supabaseService = services.GetRequiredService<SupabaseService>();
                _familyRepository = services.GetRequiredService<IFamilyRepository>();
                _syncService = services.GetRequiredService<SupabaseFamilySync>();

                LogTest("✅ Services loaded successfully");
            }
            catch (Exception ex)
            {
                LogTest($"❌ Error loading services: {ex.Message}");
            }

            // Initialize dashboard data
            LoadDashboardData();
        }

        #endregion

        #region Lifecycle Methods

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Perform fade in animation - MUITO MAIS DRAMÁTICO
            await PerformEntranceAnimation();
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();

            // Perform fade out animation
            await PerformExitAnimation();
        }

        /// <summary>
        /// Performs entrance animation with DRAMATIC fade in + scale
        /// </summary>
        private async Task PerformEntranceAnimation()
        {
            // ANIMAÇÃO SUPER DRAMÁTICA - múltiplos efeitos

            // Configurar estado inicial MUITO visível
            this.Opacity = 0;
            this.Scale = 0.9;

            // FADE + SCALE simultaneamente (muito mais perceptível)
            var fadeTask = this.FadeTo(1, 800, Easing.CubicOut);
            var scaleTask = this.ScaleTo(1, 800, Easing.SpringOut);

            await Task.WhenAll(fadeTask, scaleTask);
        }

        /// <summary>
        /// Performs exit animation with dramatic fade out
        /// </summary>
        private async Task PerformExitAnimation()
        {
            // FADE + SCALE para saída também
            var fadeTask = this.FadeTo(0, 400, Easing.CubicIn);
            var scaleTask = this.ScaleTo(0.95, 400, Easing.CubicIn);

            await Task.WhenAll(fadeTask, scaleTask);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Loads initial dashboard data and statistics
        /// </summary>
        private void LoadDashboardData()
        {
            // TODO: Replace with actual data service calls
            // This is mock data for demonstration purposes
            TotalOrchidCount = 12;
            HealthyOrchidCount = 9;
            AttentionNeededCount = 3;
        }

        /// <summary>
        /// Updates the debug counter button text based on click count
        /// </summary>
        private void UpdateDebugCounterText()
        {
            var clickText = _debugClickCount == 1 ? "click" : "clicks";
            DebugCounterButton.Text = $"Debug: {_debugClickCount} {clickText}";
        }

        #endregion

        #region Event Handlers (Original)

        /// <summary>
        /// Handles the My Orchids button click event
        /// Navigates to the orchid collection view
        /// </summary>
        private async void OnMyOrchidsClicked(object? sender, EventArgs e)
        {
            try
            {
                // TODO: Navigate to orchid collection page
                await DisplayAlert("Navigation", "Navigate to My Orchids page", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to navigate: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Handles the Add Orchid button click event
        /// Navigates to the add orchid form
        /// </summary>
        private async void OnAddOrchidClicked(object? sender, EventArgs e)
        {
            try
            {
                // TODO: Navigate to add orchid page
                await DisplayAlert("Navigation", "Navigate to Add Orchid page", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to navigate: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Handles the Calendar button click event
        /// Navigates to the care schedule calendar
        /// </summary>
        private async void OnCalendarClicked(object? sender, EventArgs e)
        {
            try
            {
                // TODO: Navigate to calendar page
                await DisplayAlert("Navigation", "Navigate to Calendar page", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to navigate: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// ADICIONADO: Navigate to Families page
        /// </summary>
        private async void OnFamiliesClicked(object? sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync("families");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to navigate to families: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Handles the debug counter button click event
        /// Used for development testing purposes only
        /// </summary>
        private void OnDebugCounterClicked(object? sender, EventArgs e)
        {
            // Increment debug counter
            _debugClickCount++;

            // Update button text
            UpdateDebugCounterText();

            // Announce to screen reader for accessibility
            SemanticScreenReader.Announce(DebugCounterButton.Text);
        }

        #endregion

        #region SYNC TESTING METHODS (ADICIONADO)

        /// <summary>
        /// ADICIONADO: Testa conexão Supabase COMPLETA
        /// </summary>
        private async void OnTestSupabaseClicked(object? sender, EventArgs e)
        {
            try
            {
                LogTest("🧪 Testing Supabase connection...");

                // Test 1: Initialize
                await _supabaseService.InitializeAsync();
                LogTest("✅ Supabase initialized");

                // Test 2: Basic connection
                var connected = await _supabaseService.TestSyncConnectionAsync();
                LogTest($"✅ Basic connection: {connected}");

                // Test 3: User status
                var isAuth = _supabaseService.IsAuthenticated;
                var userId = _supabaseService.GetCurrentUserId();
                LogTest($"✅ Authenticated: {isAuth}");
                LogTest($"✅ User ID: {userId ?? "None"}");

                // Test 4: Families table access
                var familiesConnected = await _syncService.TestConnectionAsync();
                LogTest($"✅ Families table: {familiesConnected}");

                // Test 5: NOVO - Insert/Delete capability
                if (isAuth)
                {
                    var canInsert = await _syncService.TestInsertAsync();
                    LogTest($"✅ Insert capability: {canInsert}");
                }
                else
                {
                    LogTest("⚠️ Skipping insert test (not authenticated)");
                }

            }
            catch (Exception ex)
            {
                LogTest($"❌ Supabase test error: {ex.Message}");
            }
        }

        /// <summary>
        /// ADICIONADO: Testa operações de famílias
        /// </summary>
        private async void OnTestFamiliesClicked(object? sender, EventArgs e)
        {
            try
            {
                LogTest("🧪 Testing families...");

                // Test 1: Get local families
                var families = await _familyRepository.GetAllAsync();
                LogTest($"✅ Local families: {families.Count}");

                foreach (var family in families.Take(3))
                {
                    LogTest($"  - {family.Name} ({family.SyncStatus})");
                }

                // Test 2: Download from server
                var serverFamilies = await _syncService.DownloadFamiliesAsync();
                LogTest($"✅ Server families: {serverFamilies.Count}");

                foreach (var family in serverFamilies.Take(3))
                {
                    LogTest($"  - {family.Name} (from server)");
                }

                // Test 3: Statistics
                var stats = await _familyRepository.GetStatisticsAsync();
                LogTest($"✅ Stats - Total: {stats.TotalCount}, Synced: {stats.SyncedCount}");
                LogTest($"   Local: {stats.LocalCount}, Pending: {stats.PendingCount}");

            }
            catch (Exception ex)
            {
                LogTest($"❌ Families test error: {ex.Message}");
            }
        }

        /// <summary>
        /// ADICIONADO: Cria família de teste
        /// </summary>
        private async void OnCreateTestFamilyClicked(object? sender, EventArgs e)
        {
            try
            {
                LogTest("🧪 Creating test family...");

                var testFamily = new Family
                {
                    Name = $"Test Family {DateTime.Now:HH:mm:ss}",
                    Description = "Family created for testing sync",
                    IsActive = true
                };

                var created = await _familyRepository.CreateAsync(testFamily);
                LogTest($"✅ Created: {created.Name}");
                LogTest($"   Status: {created.SyncStatus}");
                LogTest($"   ID: {created.Id}");

                // Wait and check sync status
                LogTest("⏳ Waiting 3s for auto-sync...");
                await Task.Delay(3000);

                var updated = await _familyRepository.GetByIdAsync(created.Id);
                if (updated != null)
                {
                    LogTest($"   After 3s: {updated.SyncStatus}");
                    if (updated.LastSyncAt.HasValue)
                    {
                        LogTest($"   Synced at: {updated.LastSyncAt:HH:mm:ss}");
                    }
                }

            }
            catch (Exception ex)
            {
                LogTest($"❌ Create test error: {ex.Message}");
            }
        }

        /// <summary>
        /// ADICIONADO: Força sincronização completa
        /// </summary>
        private async void OnForceFullSyncClicked(object? sender, EventArgs e)
        {
            try
            {
                LogTest("🔄 Starting manual full sync...");

                var result = await _familyRepository.ForceFullSyncAsync();

                LogTest($"✅ Sync completed:");
                LogTest($"   Processed: {result.TotalProcessed}");
                LogTest($"   Successful: {result.Successful}");
                LogTest($"   Failed: {result.Failed}");
                LogTest($"   Duration: {result.Duration.TotalSeconds:F1}s");

                if (result.ErrorMessages.Any())
                {
                    LogTest($"   Errors: {string.Join(", ", result.ErrorMessages)}");
                }

                // Refresh family count
                var families = await _familyRepository.GetAllAsync();
                LogTest($"📊 Total families after sync: {families.Count}");

            }
            catch (Exception ex)
            {
                LogTest($"❌ Force sync error: {ex.Message}");
            }
        }

        /// <summary>
        /// ADICIONADO: Limpa log de teste
        /// </summary>
        private void OnClearTestLogClicked(object? sender, EventArgs e)
        {
            TestLogLabel.Text = "Log cleared at " + DateTime.Now.ToString("HH:mm:ss") + "\n";
        }

        /// <summary>
        /// ADICIONADO: Helper para adicionar logs de teste
        /// </summary>
        private void LogTest(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            TestLogLabel.Text += $"[{timestamp}] {message}\n";

            // Auto-scroll to bottom
            TestLogScrollView.ScrollToAsync(TestLogLabel, ScrollToPosition.End, false);

            // Also log to debug console
            Debug.WriteLine($"[MainPage] {message}");
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        /// <summary>
        /// Occurs when a property value changes
        /// </summary>
        public new event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event
        /// </summary>
        /// <param name="propertyName">Name of the property that changed</param>
        protected new void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}