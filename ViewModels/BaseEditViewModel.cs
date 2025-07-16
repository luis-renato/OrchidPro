using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.Services.Navigation;
using System.Diagnostics;

namespace OrchidPro.ViewModels;

/// <summary>
/// PASSO 4: BaseEditViewModel genérico baseado no padrão de FamilyEditViewModel
/// Extrai toda funcionalidade comum de CRUD: conectividade, validação, salvar, etc.
/// </summary>
public abstract partial class BaseEditViewModel<T> : BaseViewModel, IQueryAttributable
    where T : class, IBaseEntity, new()
{
    protected readonly IBaseRepository<T> _repository;
    protected readonly INavigationService _navigationService;

    private T? _originalEntity;
    private bool _isEditMode;

    #region Observable Properties

    [ObservableProperty]
    private Guid? entityId;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private bool isActive = true;

    [ObservableProperty]
    private bool isSystemDefault;

    [ObservableProperty]
    private DateTime createdAt;

    [ObservableProperty]
    private DateTime updatedAt;

    [ObservableProperty]
    private bool hasUnsavedChanges;

    [ObservableProperty]
    private bool isNameValid = true;

    [ObservableProperty]
    private bool isDescriptionValid = true;

    [ObservableProperty]
    private string nameError = string.Empty;

    [ObservableProperty]
    private string descriptionError = string.Empty;

    [ObservableProperty]
    private bool canSave;

    [ObservableProperty]
    private bool canDelete;

    [ObservableProperty]
    private bool isNameFocused;

    [ObservableProperty]
    private bool isDescriptionFocused;

    [ObservableProperty]
    private string saveButtonText = "Save";

    [ObservableProperty]
    private Color saveButtonColor;

    [ObservableProperty]
    private string connectionStatus = "Connected";

    [ObservableProperty]
    private Color connectionStatusColor = Colors.Green;

    [ObservableProperty]
    private bool isConnected = true;

    [ObservableProperty]
    private string connectionTestResult = "";

    [ObservableProperty]
    private string loadingMessage = "Loading...";

    [ObservableProperty]
    private bool isSaving;

    #endregion

    #region Abstract Properties - Deve ser implementado pela classe filha

    /// <summary>
    /// Nome da entidade (ex: "Family", "Species")
    /// </summary>
    public abstract string EntityName { get; }

    /// <summary>
    /// Nome da entidade no plural (ex: "Families", "Species")
    /// </summary>
    public abstract string EntityNamePlural { get; }

    #endregion

    #region Computed Properties

    public string PageTitle => _isEditMode ? $"Edit {EntityName}" : $"Add {EntityName}";
    public string PageSubtitle => _isEditMode ? $"Modify {EntityName.ToLower()} information" : $"Create a new {EntityName.ToLower()}";

    #endregion

    #region Constructor

    protected BaseEditViewModel(IBaseRepository<T> repository, INavigationService navigationService)
    {
        _repository = repository;
        _navigationService = navigationService;

        Title = $"{EntityName} Details";
        SaveButtonColor = Colors.Green;

        // Assume conectado inicialmente
        IsConnected = true;
        ConnectionStatus = "Connected";
        ConnectionStatusColor = Colors.Green;

        SetupValidation();

        Debug.WriteLine($"✅ [BASE_EDIT_VM] Initialized for {EntityName}");
    }

    #endregion

    #region Navigation and Initialization

    /// <summary>
    /// Aplica parâmetros de navegação
    /// </summary>
    public virtual void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue($"{EntityName}Id", out var entityIdObj) && entityIdObj is Guid id)
        {
            EntityId = id;
            _isEditMode = true;
            Debug.WriteLine($"📝 [BASE_EDIT_VM] Edit mode for {EntityName}: {id}");
        }
        else
        {
            _isEditMode = false;
            EntityId = null;
            Debug.WriteLine($"➕ [BASE_EDIT_VM] Create mode for {EntityName}");
        }

        Title = PageTitle;
        Subtitle = PageSubtitle;
    }

    /// <summary>
    /// Inicialização da ViewModel
    /// </summary>
    protected override async Task InitializeAsync()
    {
        try
        {
            if (_isEditMode && EntityId.HasValue)
            {
                await LoadEntityAsync();
            }
            else
            {
                SetupNewEntity();
            }

            // Teste de conectividade em background
            _ = TestConnectionInBackgroundAsync();

            UpdateSaveButton();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_EDIT_VM] Initialize error: {ex.Message}");
            await ShowErrorAsync("Initialization Error", $"Failed to load {EntityName.ToLower()} data");
        }
    }

    #endregion

    #region Entity Loading and Setup

    /// <summary>
    /// Carrega entidade para edição
    /// </summary>
    [RelayCommand]
    private async Task LoadEntityAsync()
    {
        if (!EntityId.HasValue) return;

        try
        {
            LoadingMessage = $"Loading {EntityName.ToLower()}...";
            IsBusy = true;

            Debug.WriteLine($"📥 [BASE_EDIT_VM] Loading {EntityName}: {EntityId}");

            var entity = await _repository.GetByIdAsync(EntityId.Value);
            if (entity != null)
            {
                _originalEntity = (T)entity.Clone();
                PopulateFromEntity(entity);
                CanDelete = !entity.IsSystemDefault;

                Debug.WriteLine($"✅ [BASE_EDIT_VM] Loaded {EntityName}: {entity.Name}");
            }
            else
            {
                Debug.WriteLine($"❌ [BASE_EDIT_VM] {EntityName} not found: {EntityId}");
                await ShowErrorAsync($"{EntityName} Not Found", $"The requested {EntityName.ToLower()} could not be found.");
                await _navigationService.GoBackAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_EDIT_VM] Load error: {ex.Message}");

            if (ex.Message.Contains("connection") || ex.Message.Contains("network") || ex.Message.Contains("timeout"))
            {
                UpdateConnectionStatus(false);
                await ShowErrorAsync("Connection Error", $"Failed to load {EntityName.ToLower()} data. Check your connection.");
            }
            else
            {
                await ShowErrorAsync("Load Error", $"Failed to load {EntityName.ToLower()} data.");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Configura nova entidade
    /// </summary>
    private void SetupNewEntity()
    {
        var now = DateTime.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
        IsActive = true;
        IsSystemDefault = false;
        CanDelete = false;

        IsNameFocused = true;

        Debug.WriteLine($"📝 [BASE_EDIT_VM] Set up new {EntityName} form");
    }

    /// <summary>
    /// Popula campos da entidade
    /// </summary>
    private void PopulateFromEntity(T entity)
    {
        Name = entity.Name;
        Description = entity.Description ?? string.Empty;
        IsActive = entity.IsActive;
        IsSystemDefault = entity.IsSystemDefault;
        CreatedAt = entity.CreatedAt;
        UpdatedAt = entity.UpdatedAt;

        HasUnsavedChanges = false;
    }

    #endregion

    #region Save and Delete Operations

    /// <summary>
    /// Salva a entidade
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!await ValidateFormAsync()) return;

        try
        {
            IsSaving = true;
            LoadingMessage = $"Saving {EntityName.ToLower()}...";
            IsBusy = true;
            SaveButtonText = "Saving...";
            SaveButtonColor = Colors.Orange;

            var isCurrentlyConnected = await _repository.TestConnectionAsync();
            if (!isCurrentlyConnected)
            {
                UpdateConnectionStatus(false);
                await ShowErrorAsync("No Connection", $"Cannot save without internet connection. Please check your connection and try again.");
                return;
            }

            T entity;

            if (_isEditMode && _originalEntity != null)
            {
                entity = (T)_originalEntity.Clone();
                PopulateEntity(entity);

                Debug.WriteLine($"📝 [BASE_EDIT_VM] Updating {EntityName}: {entity.Name}");

                entity = await _repository.UpdateAsync(entity);
                await ShowSuccessAsync($"{EntityName} updated successfully!");

                Debug.WriteLine($"✅ [BASE_EDIT_VM] Updated {EntityName}: {entity.Name}");
            }
            else
            {
                entity = new T();
                PopulateEntity(entity);

                Debug.WriteLine($"➕ [BASE_EDIT_VM] Creating {EntityName}: {entity.Name}");

                entity = await _repository.CreateAsync(entity);
                await ShowSuccessAsync($"{EntityName} created successfully!");

                Debug.WriteLine($"✅ [BASE_EDIT_VM] Created {EntityName}: {entity.Name}");
            }

            HasUnsavedChanges = false;
            await _navigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_EDIT_VM] Save error: {ex.Message}");

            if (ex.Message.Contains("connection") || ex.Message.Contains("network") || ex.Message.Contains("timeout"))
            {
                UpdateConnectionStatus(false);
                await ShowErrorAsync("Connection Error", $"Failed to save {EntityName.ToLower()}. Check your connection and try again.");
            }
            else
            {
                await ShowErrorAsync("Save Error", $"Failed to save {EntityName.ToLower()}. Please try again.");
            }
        }
        finally
        {
            IsSaving = false;
            IsBusy = false;
            SaveButtonText = "Save";
            SaveButtonColor = Colors.Green;
        }
    }

    /// <summary>
    /// Popula entidade com dados do form
    /// </summary>
    private void PopulateEntity(T entity)
    {
        entity.Name = Name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
        entity.IsActive = IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        if (!_isEditMode)
        {
            entity.CreatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Exclui a entidade
    /// </summary>
    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (!_isEditMode || !EntityId.HasValue || IsSystemDefault) return;

        var isCurrentlyConnected = await _repository.TestConnectionAsync();
        if (!isCurrentlyConnected)
        {
            UpdateConnectionStatus(false);
            await ShowErrorAsync("No Connection", $"Cannot delete without internet connection.");
            return;
        }

        var confirmed = await ShowConfirmAsync(
            $"Delete {EntityName}",
            $"Are you sure you want to delete '{Name}'? This action cannot be undone.");

        if (!confirmed) return;

        try
        {
            LoadingMessage = $"Deleting {EntityName.ToLower()}...";
            IsBusy = true;

            Debug.WriteLine($"🗑️ [BASE_EDIT_VM] Deleting {EntityName}: {Name}");

            var success = await _repository.DeleteAsync(EntityId.Value);

            if (success)
            {
                await ShowSuccessAsync($"{EntityName} deleted successfully!");
                await _navigationService.GoBackAsync();

                Debug.WriteLine($"✅ [BASE_EDIT_VM] Deleted {EntityName}: {Name}");
            }
            else
            {
                await ShowErrorAsync("Delete Error", $"Failed to delete {EntityName.ToLower()}. It may be protected or in use.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_EDIT_VM] Delete error: {ex.Message}");

            if (ex.Message.Contains("connection") || ex.Message.Contains("network") || ex.Message.Contains("timeout"))
            {
                UpdateConnectionStatus(false);
                await ShowErrorAsync("Connection Error", $"Failed to delete {EntityName.ToLower()}. Check your connection and try again.");
            }
            else
            {
                await ShowErrorAsync("Delete Error", $"Failed to delete {EntityName.ToLower()}. Please try again.");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Cancela edição
    /// </summary>
    [RelayCommand]
    private async Task CancelAsync()
    {
        if (HasUnsavedChanges)
        {
            var confirmed = await ShowConfirmAsync(
                "Discard Changes",
                "You have unsaved changes. Are you sure you want to discard them?");

            if (!confirmed) return;
        }

        Debug.WriteLine($"🚫 [BASE_EDIT_VM] Cancelled editing {EntityName}");
        await _navigationService.GoBackAsync();
    }

    #endregion

    #region Validation

    /// <summary>
    /// Valida nome
    /// </summary>
    [RelayCommand]
    private async Task ValidateNameAsync()
    {
        try
        {
            var trimmedName = Name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                NameError = $"{EntityName} name is required";
                IsNameValid = false;
                return;
            }

            if (trimmedName.Length < 2)
            {
                NameError = $"{EntityName} name must be at least 2 characters";
                IsNameValid = false;
                return;
            }

            if (trimmedName.Length > 255)
            {
                NameError = $"{EntityName} name cannot exceed 255 characters";
                IsNameValid = false;
                return;
            }

            // Só verifica duplicatas se conectado
            if (IsConnected)
            {
                var excludeId = _isEditMode ? EntityId : null;
                var nameExists = await _repository.NameExistsAsync(trimmedName, excludeId);

                if (nameExists)
                {
                    NameError = $"A {EntityName.ToLower()} with this name already exists";
                    IsNameValid = false;
                    return;
                }
            }

            NameError = string.Empty;
            IsNameValid = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_EDIT_VM] Name validation error: {ex.Message}");

            if (ex.Message.Contains("connection") || ex.Message.Contains("network"))
            {
                UpdateConnectionStatus(false);
                NameError = "Cannot verify uniqueness - offline";
                IsNameValid = !string.IsNullOrWhiteSpace(Name?.Trim()) && Name.Trim().Length >= 2;
            }
            else
            {
                NameError = "Error validating name";
                IsNameValid = false;
            }
        }
        finally
        {
            UpdateSaveButton();
        }
    }

    /// <summary>
    /// Valida descrição
    /// </summary>
    [RelayCommand]
    private void ValidateDescription()
    {
        var trimmedDescription = Description?.Trim() ?? string.Empty;

        if (trimmedDescription.Length > 2000)
        {
            DescriptionError = "Description cannot exceed 2000 characters";
            IsDescriptionValid = false;
        }
        else
        {
            DescriptionError = string.Empty;
            IsDescriptionValid = true;
        }

        UpdateSaveButton();
    }

    /// <summary>
    /// Valida todo o formulário
    /// </summary>
    private async Task<bool> ValidateFormAsync()
    {
        await ValidateNameAsync();
        ValidateDescription();

        return IsNameValid && IsDescriptionValid;
    }

    #endregion

    #region Connectivity

    /// <summary>
    /// Teste de conectividade em background
    /// </summary>
    private async Task TestConnectionInBackgroundAsync()
    {
        try
        {
            await Task.Delay(100);

            Debug.WriteLine($"🔍 [BASE_EDIT_VM] Testing connection in background for {EntityName}...");

            var connected = await _repository.TestConnectionAsync();

            if (!connected)
            {
                Debug.WriteLine($"📡 [BASE_EDIT_VM] Connection failed for {EntityName} - updating UI");
                UpdateConnectionStatus(false);
            }
            else
            {
                Debug.WriteLine($"✅ [BASE_EDIT_VM] Connection confirmed for {EntityName}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_EDIT_VM] Background connection test failed for {EntityName}: {ex.Message}");
            UpdateConnectionStatus(false);
        }
    }

    /// <summary>
    /// Teste manual de conectividade
    /// </summary>
    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        try
        {
            LoadingMessage = "Testing connection...";
            IsBusy = true;

            Debug.WriteLine($"🔍 [BASE_EDIT_VM] Manual connection test for {EntityName}...");

            var connected = await _repository.TestConnectionAsync();
            UpdateConnectionStatus(connected);

            var resultMessage = connected ? "✅ Connected to server" : "❌ Connection failed";
            ConnectionTestResult = resultMessage;

            Debug.WriteLine($"🔍 [BASE_EDIT_VM] Manual test result for {EntityName}: {connected}");

            _ = Task.Run(async () =>
            {
                await Task.Delay(3000);
                ConnectionTestResult = "";
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_EDIT_VM] Manual connection test error for {EntityName}: {ex.Message}");
            UpdateConnectionStatus(false);
            ConnectionTestResult = "❌ Connection error";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Atualiza status de conectividade
    /// </summary>
    private void UpdateConnectionStatus(bool connected)
    {
        IsConnected = connected;

        if (connected)
        {
            ConnectionStatus = "Connected";
            ConnectionStatusColor = Colors.Green;
        }
        else
        {
            ConnectionStatus = "Disconnected";
            ConnectionStatusColor = Colors.Red;
        }

        UpdateSaveButton();
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(PageSubtitle));
    }

    #endregion

    #region UI State Management

    /// <summary>
    /// Atualiza estado do botão salvar
    /// </summary>
    private void UpdateSaveButton()
    {
        CanSave = !string.IsNullOrWhiteSpace(Name) &&
                  IsNameValid &&
                  IsDescriptionValid &&
                  !IsSaving;
    }

    /// <summary>
    /// Configura validação automática
    /// </summary>
    private void SetupValidation()
    {
        PropertyChanged += async (sender, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(Name):
                    HasUnsavedChanges = true;
                    await Task.Delay(300);
                    if (e.PropertyName == nameof(Name))
                    {
                        await ValidateNameAsync();
                    }
                    break;

                case nameof(Description):
                    HasUnsavedChanges = true;
                    ValidateDescription();
                    break;

                case nameof(IsActive):
                    HasUnsavedChanges = true;
                    break;
            }
        };
    }

    #endregion

    #region Commands for UI Events

    [RelayCommand] private void OnNameFocused() => IsNameFocused = true;
    [RelayCommand] private async Task OnNameUnfocusedAsync() { IsNameFocused = false; await ValidateNameAsync(); }
    [RelayCommand] private async Task OnNameChangedAsync() => await ValidateNameAsync();
    [RelayCommand] private void OnDescriptionFocused() => IsDescriptionFocused = true;
    [RelayCommand] private void OnDescriptionUnfocused() { IsDescriptionFocused = false; ValidateDescription(); }
    [RelayCommand] private void OnDescriptionChanged() => ValidateDescription();
    [RelayCommand] private void ToggleActiveStatus() { IsActive = !IsActive; HasUnsavedChanges = true; }
    [RelayCommand] private void ClearDescription() => Description = string.Empty;

    [RelayCommand]
    private async Task ShowInfoAsync()
    {
        var message = _isEditMode
            ? $"{EntityName} ID: {EntityId}\nCreated: {CreatedAt:F}\nLast Updated: {UpdatedAt:F}\nConnection: {ConnectionStatus}"
            : $"This will create a new {EntityName.ToLower()} in your collection.\nConnection: {ConnectionStatus}";

        await ShowErrorAsync($"{EntityName} Information", message);
    }

    #endregion
}