using System.ComponentModel;

namespace OrchidPro
{
    /// <summary>
    /// Main page of the OrchidPro application.
    /// Provides dashboard functionality with quick actions and overview statistics.
    /// LIMPO: Testes removidos, ficam apenas no TestSyncPage
    /// </summary>
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        #region Private Fields

        private int _debugClickCount = 0;
        private int _totalOrchidCount = 0;
        private int _healthyOrchidCount = 0;
        private int _attentionNeededCount = 0;

        #endregion

        #region Public Properties

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

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;
            LoadDashboardData();
        }

        #endregion

        #region Lifecycle Methods

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await PerformEntranceAnimation();
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            await PerformExitAnimation();
        }

        private async Task PerformEntranceAnimation()
        {
            this.Opacity = 0;
            this.Scale = 0.9;

            var fadeTask = this.FadeTo(1, 800, Easing.CubicOut);
            var scaleTask = this.ScaleTo(1, 800, Easing.SpringOut);

            await Task.WhenAll(fadeTask, scaleTask);
        }

        private async Task PerformExitAnimation()
        {
            var fadeTask = this.FadeTo(0, 400, Easing.CubicIn);
            var scaleTask = this.ScaleTo(0.95, 400, Easing.CubicIn);

            await Task.WhenAll(fadeTask, scaleTask);
        }

        #endregion

        #region Private Methods

        private void LoadDashboardData()
        {
            TotalOrchidCount = 12;
            HealthyOrchidCount = 9;
            AttentionNeededCount = 3;
        }

        private void UpdateDebugCounterText()
        {
            var clickText = _debugClickCount == 1 ? "click" : "clicks";
            DebugCounterButton.Text = $"Debug: {_debugClickCount} {clickText}";
        }

        #endregion

        #region Event Handlers

        private async void OnMyOrchidsClicked(object? sender, EventArgs e)
        {
            try
            {
                await DisplayAlert("Navigation", "Navigate to My Orchids page", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to navigate: {ex.Message}", "OK");
            }
        }

        private async void OnAddOrchidClicked(object? sender, EventArgs e)
        {
            try
            {
                await DisplayAlert("Navigation", "Navigate to Add Orchid page", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to navigate: {ex.Message}", "OK");
            }
        }

        private async void OnCalendarClicked(object? sender, EventArgs e)
        {
            try
            {
                await DisplayAlert("Navigation", "Navigate to Calendar page", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to navigate: {ex.Message}", "OK");
            }
        }

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

        private void OnDebugCounterClicked(object? sender, EventArgs e)
        {
            _debugClickCount++;
            UpdateDebugCounterText();
            SemanticScreenReader.Announce(DebugCounterButton.Text);
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected new void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}