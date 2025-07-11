using System.ComponentModel;

namespace OrchidPro
{
    /// <summary>
    /// Main page of the OrchidPro application.
    /// Provides dashboard functionality with quick actions and overview statistics.
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

        #region Event Handlers

        /// <summary>
        /// Handles the My Orchids button click event
        /// Navigates to the orchid collection view
        /// </summary>
        /// <param name="sender">The button that triggered the event</param>
        /// <param name="e">Event arguments</param>
        private async void OnMyOrchidsClicked(object? sender, EventArgs e)
        {
            try
            {
                // TODO: Navigate to orchid collection page
                await DisplayAlert("Navigation", "Navigate to My Orchids page", "OK");
            }
            catch (Exception ex)
            {
                // TODO: Implement proper error handling/logging
                await DisplayAlert("Error", $"Failed to navigate: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Handles the Add Orchid button click event
        /// Navigates to the add orchid form
        /// </summary>
        /// <param name="sender">The button that triggered the event</param>
        /// <param name="e">Event arguments</param>
        private async void OnAddOrchidClicked(object? sender, EventArgs e)
        {
            try
            {
                // TODO: Navigate to add orchid page
                await DisplayAlert("Navigation", "Navigate to Add Orchid page", "OK");
            }
            catch (Exception ex)
            {
                // TODO: Implement proper error handling/logging
                await DisplayAlert("Error", $"Failed to navigate: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Handles the Calendar button click event
        /// Navigates to the care schedule calendar
        /// </summary>
        /// <param name="sender">The button that triggered the event</param>
        /// <param name="e">Event arguments</param>
        private async void OnCalendarClicked(object? sender, EventArgs e)
        {
            try
            {
                // TODO: Navigate to calendar page
                await DisplayAlert("Navigation", "Navigate to Calendar page", "OK");
            }
            catch (Exception ex)
            {
                // TODO: Implement proper error handling/logging
                await DisplayAlert("Error", $"Failed to navigate: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Handles the Reports button click event
        /// Navigates to the analytics and reports section
        /// </summary>
        /// <param name="sender">The button that triggered the event</param>
        /// <param name="e">Event arguments</param>
        private async void OnReportsClicked(object? sender, EventArgs e)
        {
            try
            {
                // TODO: Navigate to reports page
                await DisplayAlert("Navigation", "Navigate to Reports page", "OK");
            }
            catch (Exception ex)
            {
                // TODO: Implement proper error handling/logging
                await DisplayAlert("Error", $"Failed to navigate: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Handles the debug counter button click event
        /// Used for development testing purposes only
        /// </summary>
        /// <param name="sender">The button that triggered the event</param>
        /// <param name="e">Event arguments</param>
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