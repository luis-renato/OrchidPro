using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using System.Diagnostics;

namespace OrchidPro.ViewModels;

/// <summary>
/// CORRIGIDO: ViewModel para criação e edição de Family com teste de conectividade otimizado
/// </summary>
public partial class FamilyEditViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IFamilyRepository _familyRepository;
    private readonly INavigationService _navigationService;

    private Family? _originalFamily;
    private bool _isEditMode;

    [ObservableProperty]
    private Guid? familyId;

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

    // CORRIGIDO: Conectividade inicializada como true, testa depois
    [ObservableProperty]
    private string connectionStatus = "🌐 Connected";

    [ObservableProperty]
    private Color connectionStatusColor = Colors.Green;

    [ObservableProperty]
    private bool isConnected = true; // ✅ Inicia como true

    [ObservableProperty]
    private string autoSaveStatus = "";

    [ObservableProperty]
    private Color autoSaveStatusColor = Colors.Green;

    [ObservableProperty]
    private bool showAutoSaveStatus = false;

    [ObservableProperty]
    private string autoSaveMessage = "";

    [ObservableProperty]
    private bool showAutoSaveSuccess = false;

    [ObservableProperty]
    private string connectionTestResult = "";

    [ObservableProperty]
    private string loadingMessage = "Loading...";

    [ObservableProperty]
    private bool isSaving;

    public string PageTitle => _isEditMode ? "Edit Family" : "Add Family";
    public string PageSubtitle => _isEditMode ? "Modify family information" : "Create a new botanical family";

    public FamilyEditViewModel(IFamilyRepository familyRepository, INavigationService navigationService)
    {
        _familyRepository = familyRepository;
        _navigationService = navigationService;

        Title = "Family Details";
        SaveButtonColor = Colors.Green;

        // ✅ CORRIGIDO: Assume conectado inicialmente
        IsConnected = true;
        ConnectionStatus = "🌐 Connected";
        ConnectionStatusColor = Colors.Green;

        // Set up validation
        SetupValidation();

        Debug.WriteLine("✅ [FAMILY_EDIT_VM] Initialized with optimistic connectivity");
    }

    /// <summary>
    /// Applies query attributes for navigation parameters
    /// </summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("FamilyId", out var familyIdObj) && familyIdObj is Guid id)
        {
            FamilyId = id;
            _isEditMode = true;
            Debug.WriteLine($"📝 [FAMILY_EDIT_VM] Edit mode for family: {id}");
        }
        else
        {
            _isEditMode = false;
            FamilyId = null;
            Debug.WriteLine("➕ [FAMILY_EDIT_VM] Create mode");
        }

        Title = PageTitle;
        Subtitle = PageSubtitle;
    }

    /// <summary>
    /// CORRIGIDO: Inicializa otimisticamente e testa conectividade em background
    /// </summary>
    protected override async Task InitializeAsync()
    {
        try
        {
            // ✅ Carregar dados imediatamente (assume conectado)
            if (_isEditMode && FamilyId.HasValue)
            {
                await LoadFamilyAsync();
            }
            else
            {
                SetupNewFamily();
            }

            // ✅ Testar conectividade em background (não bloqueia UI)
            _ = TestConnectionInBackgroundAsync();

            UpdateSaveButton();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] Initialize error: {ex.Message}");
            await ShowErrorAsync("Initialization Error", "Failed to load family data");
        }
    }

    /// <summary>
    /// NOVO: Teste de conectividade em background sem bloquear UI
    /// </summary>
    private async Task TestConnectionInBackgroundAsync()
    {
        try
        {
            await Task.Delay(100); // Pequeno delay para UI se renderizar primeiro

            Debug.WriteLine("🔍 [FAMILY_EDIT_VM] Testing connection in background...");

            var connected = await _familyRepository.TestConnectionAsync();

            // ✅ Só atualiza se realmente desconectado
            if (!connected)
            {
                Debug.WriteLine("📡 [FAMILY_EDIT_VM] Connection failed - updating UI");
                UpdateConnectionStatus(false);
            }
            else
            {
                Debug.WriteLine("✅ [FAMILY_EDIT_VM] Connection confirmed");
                // Mantém como conectado (já era)
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] Background connection test failed: {ex.Message}");
            // ✅ Só atualiza se houver erro real
            UpdateConnectionStatus(false);
        }
    }

    /// <summary>
    /// CORRIGIDO: Teste de conectividade manual que força atualização
    /// </summary>
    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        try
        {
            LoadingMessage = "Testing connection...";
            IsBusy = true;

            Debug.WriteLine("🔍 [FAMILY_EDIT_VM] Manual connection test...");

            var connected = await _familyRepository.TestConnectionAsync();
            UpdateConnectionStatus(connected);

            var resultMessage = connected ? "✅ Connected to server" : "❌ Connection failed";
            ConnectionTestResult = resultMessage;

            Debug.WriteLine($"🔍 [FAMILY_EDIT_VM] Manual test result: {connected}");

            // Mostrar resultado por 3 segundos
            _ = Task.Run(async () =>
            {
                await Task.Delay(3000);
                ConnectionTestResult = "";
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] Manual connection test error: {ex.Message}");
            UpdateConnectionStatus(false);
            ConnectionTestResult = "❌ Connection error";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Carrega dados da família para edição
    /// </summary>
    [RelayCommand]
    private async Task LoadFamilyAsync()
    {
        if (!FamilyId.HasValue) return;

        try
        {
            LoadingMessage = "Loading family...";
            IsBusy = true;

            Debug.WriteLine($"📥 [FAMILY_EDIT_VM] Loading family: {FamilyId}");

            var family = await _familyRepository.GetByIdAsync(FamilyId.Value);
            if (family != null)
            {
                _originalFamily = family.Clone();
                PopulateFromFamily(family);
                CanDelete = !family.IsSystemDefault;

                Debug.WriteLine($"✅ [FAMILY_EDIT_VM] Loaded family: {family.Name}");
            }
            else
            {
                Debug.WriteLine($"❌ [FAMILY_EDIT_VM] Family not found: {FamilyId}");
                await ShowErrorAsync("Family Not Found", "The requested family could not be found.");
                await _navigationService.GoBackAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] Load error: {ex.Message}");

            // ✅ Só marca como desconectado se for erro de rede
            if (ex.Message.Contains("connection") || ex.Message.Contains("network") || ex.Message.Contains("timeout"))
            {
                UpdateConnectionStatus(false);
                await ShowErrorAsync("Connection Error", "Failed to load family data. Check your connection.");
            }
            else
            {
                await ShowErrorAsync("Load Error", "Failed to load family data.");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Sets up a new family with default values
    /// </summary>
    private void SetupNewFamily()
    {
        var now = DateTime.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
        IsActive = true;
        IsSystemDefault = false;
        CanDelete = false;

        // Focus on name field for new entries
        IsNameFocused = true;

        Debug.WriteLine("📝 [FAMILY_EDIT_VM] Set up new family form");
    }

    /// <summary>
    /// Populates fields from a family entity
    /// </summary>
    private void PopulateFromFamily(Family family)
    {
        Name = family.Name;
        Description = family.Description ?? string.Empty;
        IsActive = family.IsActive;
        IsSystemDefault = family.IsSystemDefault;
        CreatedAt = family.CreatedAt;
        UpdatedAt = family.UpdatedAt;

        // Reset change tracking after loading
        HasUnsavedChanges = false;
    }

    /// <summary>
    /// Salva a família (criar ou atualizar)
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!await ValidateFormAsync()) return;

        // ✅ CORRIGIDO: Permite salvar se conectado OU offline (para cache local)
        // Pode implementar queue offline no futuro

        try
        {
            IsSaving = true;
            LoadingMessage = "Saving family...";
            IsBusy = true;
            SaveButtonText = "Saving...";
            SaveButtonColor = Colors.Orange;

            // ✅ Verificar conectividade antes de salvar
            var isCurrentlyConnected = await _familyRepository.TestConnectionAsync();
            if (!isCurrentlyConnected)
            {
                UpdateConnectionStatus(false);
                await ShowErrorAsync("No Connection", "Cannot save without internet connection. Please check your connection and try again.");
                return;
            }

            Family family;

            if (_isEditMode && _originalFamily != null)
            {
                // Update existing family
                family = _originalFamily.Clone();
                family.Name = Name.Trim();
                family.Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
                family.IsActive = IsActive;

                Debug.WriteLine($"📝 [FAMILY_EDIT_VM] Updating family: {family.Name}");

                family = await _familyRepository.UpdateAsync(family);
                await ShowSuccessAsync("Family updated successfully!");

                Debug.WriteLine($"✅ [FAMILY_EDIT_VM] Updated family: {family.Name}");
            }
            else
            {
                // Create new family
                family = new Family
                {
                    Name = Name.Trim(),
                    Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                    IsActive = IsActive
                };

                Debug.WriteLine($"➕ [FAMILY_EDIT_VM] Creating family: {family.Name}");

                family = await _familyRepository.CreateAsync(family);
                await ShowSuccessAsync("Family created successfully!");

                Debug.WriteLine($"✅ [FAMILY_EDIT_VM] Created family: {family.Name}");
            }

            // Reset change tracking
            HasUnsavedChanges = false;

            // Navigate back to list
            await _navigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] Save error: {ex.Message}");

            if (ex.Message.Contains("connection") || ex.Message.Contains("network") || ex.Message.Contains("timeout"))
            {
                UpdateConnectionStatus(false);
                await ShowErrorAsync("Connection Error", "Failed to save family. Check your connection and try again.");
            }
            else
            {
                await ShowErrorAsync("Save Error", "Failed to save family. Please try again.");
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
    /// Exclui a família
    /// </summary>
    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (!_isEditMode || !FamilyId.HasValue || IsSystemDefault) return;

        // ✅ Verificar conectividade antes de deletar
        var isCurrentlyConnected = await _familyRepository.TestConnectionAsync();
        if (!isCurrentlyConnected)
        {
            UpdateConnectionStatus(false);
            await ShowErrorAsync("No Connection", "Cannot delete without internet connection.");
            return;
        }

        var confirmed = await ShowConfirmAsync(
            "Delete Family",
            $"Are you sure you want to delete '{Name}'? This action cannot be undone.");

        if (!confirmed) return;

        try
        {
            LoadingMessage = "Deleting family...";
            IsBusy = true;

            Debug.WriteLine($"🗑️ [FAMILY_EDIT_VM] Deleting family: {Name}");

            var success = await _familyRepository.DeleteAsync(FamilyId.Value);

            if (success)
            {
                await ShowSuccessAsync("Family deleted successfully!");
                await _navigationService.GoBackAsync();

                Debug.WriteLine($"✅ [FAMILY_EDIT_VM] Deleted family: {Name}");
            }
            else
            {
                await ShowErrorAsync("Delete Error", "Failed to delete family. It may be protected or in use.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] Delete error: {ex.Message}");

            if (ex.Message.Contains("connection") || ex.Message.Contains("network") || ex.Message.Contains("timeout"))
            {
                UpdateConnectionStatus(false);
                await ShowErrorAsync("Connection Error", "Failed to delete family. Check your connection and try again.");
            }
            else
            {
                await ShowErrorAsync("Delete Error", "Failed to delete family. Please try again.");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Cancels editing and navigates back
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

        Debug.WriteLine("🚫 [FAMILY_EDIT_VM] Cancelled editing");
        await _navigationService.GoBackAsync();
    }

    /// <summary>
    /// Valida o campo nome
    /// </summary>
    [RelayCommand]
    private async Task ValidateNameAsync()
    {
        try
        {
            var trimmedName = Name?.Trim() ?? string.Empty;

            // Basic validation
            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                NameError = "Family name is required";
                IsNameValid = false;
                return;
            }

            if (trimmedName.Length < 2)
            {
                NameError = "Family name must be at least 2 characters";
                IsNameValid = false;
                return;
            }

            if (trimmedName.Length > 255)
            {
                NameError = "Family name cannot exceed 255 characters";
                IsNameValid = false;
                return;
            }

            // ✅ CORRIGIDO: Só verifica duplicatas se conectado
            if (IsConnected)
            {
                // Check for duplicates
                var excludeId = _isEditMode ? FamilyId : null;
                var nameExists = await _familyRepository.NameExistsAsync(trimmedName, excludeId);

                if (nameExists)
                {
                    NameError = "A family with this name already exists";
                    IsNameValid = false;
                    return;
                }
            }

            // All validations passed
            NameError = string.Empty;
            IsNameValid = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] Name validation error: {ex.Message}");

            // ✅ Se falhar validação, não bloqueia (pode ser offline)
            if (ex.Message.Contains("connection") || ex.Message.Contains("network"))
            {
                UpdateConnectionStatus(false);
                NameError = "Cannot verify uniqueness - offline";
                IsNameValid = !string.IsNullOrWhiteSpace(Name?.Trim()) && Name.Trim().Length >= 2; // Basic validation only
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
    /// Validates the description field
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
    /// Validates the entire form
    /// </summary>
    private async Task<bool> ValidateFormAsync()
    {
        await ValidateNameAsync();
        ValidateDescription();

        return IsNameValid && IsDescriptionValid;
    }

    /// <summary>
    /// CORRIGIDO: Updates the save button state (permite salvar offline com validação básica)
    /// </summary>
    private void UpdateSaveButton()
    {
        // ✅ Permite salvar se tem nome válido (mesmo offline)
        CanSave = !string.IsNullOrWhiteSpace(Name) &&
                  IsNameValid &&
                  IsDescriptionValid &&
                  !IsSaving;
    }

    /// <summary>
    /// Atualiza status de conectividade
    /// </summary>
    private void UpdateConnectionStatus(bool connected)
    {
        IsConnected = connected;

        if (connected)
        {
            ConnectionStatus = "🌐 Connected";
            ConnectionStatusColor = Colors.Green;
        }
        else
        {
            ConnectionStatus = "📡 Disconnected";
            ConnectionStatusColor = Colors.Red;
        }

        UpdateSaveButton();
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(PageSubtitle));
    }

    /// <summary>
    /// Sets up validation event handlers
    /// </summary>
    private void SetupValidation()
    {
        // Validate on property changes with debouncing
        PropertyChanged += async (sender, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(Name):
                    HasUnsavedChanges = true;
                    // Debounce validation
                    await Task.Delay(300);
                    if (e.PropertyName == nameof(Name)) // Still the same property
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
            ? $"Family ID: {FamilyId}\nCreated: {CreatedAt:F}\nLast Updated: {UpdatedAt:F}\nConnection: {ConnectionStatus}"
            : $"This will create a new botanical family in your collection.\nConnection: {ConnectionStatus}";

        await ShowErrorAsync("Family Information", message);
    }
}