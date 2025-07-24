using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models.Base;
using OrchidPro.Services.Navigation;
using OrchidPro.Services;
using System.Diagnostics;

namespace OrchidPro.ViewModels.Base;

/// <summary>
/// ✅ CORRIGIDO: BaseEditViewModel com todas as propriedades observáveis
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

    #region Abstract Properties

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

    /// <summary>
    /// ✅ NOVO: Propriedade para acessar o NavigationService
    /// </summary>
    public INavigationService NavigationService => _navigationService;

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

    #region ✅ CORRIGIDA: Navigation and Query Attributes

    /// <summary>
    /// ✅ CORRIGIDO: Aplica parâmetros de navegação de forma flexível
    /// </summary>
    public virtual void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        try
        {
            Debug.WriteLine($"🔍 [BASE_EDIT_VM] ApplyQueryAttributes for {EntityName} with {query.Count} parameters");

            // ✅ CORRIGIDO: Verificar múltiplas variações de chave
            Guid? foundId = null;
            string[] possibleKeys = { $"{EntityName}Id", "Id", "id", $"{EntityName.ToLower()}Id" };

            foreach (var key in possibleKeys)
            {
                if (query.TryGetValue(key, out var idObj))
                {
                    foundId = ConvertToGuid(idObj);
                    if (foundId.HasValue)
                    {
                        Debug.WriteLine($"📝 [BASE_EDIT_VM] Found ID with key '{key}': {foundId}");
                        break;
                    }
                }
            }

            if (foundId.HasValue && foundId.Value != Guid.Empty)
            {
                EntityId = foundId;
                _isEditMode = true;
                SaveButtonText = "Update";
                Debug.WriteLine($"✅ [BASE_EDIT_VM] EDIT MODE for {EntityName}: {foundId}");
            }
            else
            {
                EntityId = null;
                _isEditMode = false;
                SaveButtonText = "Create";
                Debug.WriteLine($"✅ [BASE_EDIT_VM] CREATE MODE for {EntityName}");
            }

            Title = PageTitle;
            Subtitle = PageSubtitle;
            UpdateSaveButton();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_EDIT_VM] ApplyQueryAttributes error: {ex.Message}");
            // Em caso de erro, assumir modo criação
            _isEditMode = false;
            EntityId = null;
            SaveButtonText = "Create";
        }
    }

    /// <summary>
    /// ✅ NOVO: Converte object para Guid de forma segura
    /// </summary>
    protected Guid? ConvertToGuid(object obj)
    {
        if (obj == null) return null;

        if (obj is Guid guid)
            return guid;

        if (obj is string str && Guid.TryParse(str, out var parsedGuid))
            return parsedGuid;

        Debug.WriteLine($"⚠️ [BASE_EDIT_VM] Cannot convert {obj} ({obj.GetType().Name}) to Guid");
        return null;
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Inicialização da ViewModel
    /// </summary>
    protected override async Task InitializeAsync()
    {
        try
        {
            Debug.WriteLine($"🔄 [BASE_EDIT_VM] InitializeAsync - Edit mode: {_isEditMode}, EntityId: {EntityId}");

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

    /// <summary>
    /// Carrega entidade para edição
    /// </summary>
    protected virtual async Task LoadEntityAsync()
    {
        try
        {
            if (!EntityId.HasValue) return;

            LoadingMessage = $"Loading {EntityName.ToLower()}...";
            IsBusy = true;

            var entity = await _repository.GetByIdAsync(EntityId.Value);
            if (entity != null)
            {
                _originalEntity = entity;
                await PopulateFromEntityAsync(entity);
                Debug.WriteLine($"✅ [BASE_EDIT_VM] Loaded {EntityName}: {Name}");
            }
            else
            {
                Debug.WriteLine($"❌ [BASE_EDIT_VM] {EntityName} not found: {EntityId}");
                await ShowErrorAsync("Not Found", $"{EntityName} not found");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_EDIT_VM] LoadEntity error: {ex.Message}");
            await ShowErrorAsync("Load Error", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Popula propriedades a partir da entidade
    /// </summary>
    protected virtual async Task PopulateFromEntityAsync(T entity)
    {
        Name = entity.Name;
        Description = entity.Description ?? string.Empty;
        IsActive = entity.IsActive;
        IsSystemDefault = entity.IsSystemDefault;
        CreatedAt = entity.CreatedAt;
        UpdatedAt = entity.UpdatedAt;

        await Task.CompletedTask;
    }

    /// <summary>
    /// Configura nova entidade
    /// </summary>
    protected virtual void SetupNewEntity()
    {
        Name = string.Empty;
        Description = string.Empty;
        IsActive = true;
        IsSystemDefault = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        Debug.WriteLine($"✅ [BASE_EDIT_VM] Setup new {EntityName}");
    }

    #endregion

    #region Validation

    /// <summary>
    /// Configura validação de propriedades
    /// </summary>
    protected virtual void SetupValidation()
    {
        PropertyChanged += (s, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(Name):
                    ValidateName();
                    break;
                case nameof(Description):
                    ValidateDescription();
                    break;
            }

            UpdateSaveButton();
            CheckForUnsavedChanges();
        };
    }

    protected virtual void ValidateName()
    {
        IsNameValid = !string.IsNullOrWhiteSpace(Name);
        NameError = IsNameValid ? string.Empty : $"{EntityName} name is required";
    }

    protected virtual void ValidateDescription()
    {
        IsDescriptionValid = true; // Descrição é opcional por padrão
        // Subclasses podem implementar validação específica
    }

    protected virtual void UpdateSaveButton()
    {
        CanSave = IsNameValid && IsDescriptionValid && !IsBusy && !IsSaving;
        SaveButtonColor = CanSave ? Colors.Green : Colors.Gray;
    }

    protected virtual void CheckForUnsavedChanges()
    {
        if (_originalEntity == null)
        {
            HasUnsavedChanges = !string.IsNullOrWhiteSpace(Name) || !string.IsNullOrWhiteSpace(Description);
        }
        else
        {
            HasUnsavedChanges =
                Name != _originalEntity.Name ||
                Description != (_originalEntity.Description ?? string.Empty) ||
                IsActive != _originalEntity.IsActive;
        }
    }

    #endregion

    #region Commands

    [RelayCommand]
    protected virtual async Task SaveAsync()
    {
        try
        {
            if (!CanSave) return;

            ValidateName();
            ValidateDescription();

            if (!IsNameValid || !IsDescriptionValid)
            {
                await ShowErrorAsync("Validation Error", "Please fix the validation errors");
                return;
            }

            IsSaving = true;
            LoadingMessage = _isEditMode ? $"Updating {EntityName.ToLower()}..." : $"Creating {EntityName.ToLower()}...";

            var entity = await CreateEntityFromPropertiesAsync();

            T result;
            if (_isEditMode)
            {
                result = await _repository.UpdateAsync(entity);
                Debug.WriteLine($"✅ [BASE_EDIT_VM] Updated {EntityName}: {result.Name}");
            }
            else
            {
                result = await _repository.CreateAsync(entity);
                Debug.WriteLine($"✅ [BASE_EDIT_VM] Created {EntityName}: {result.Name}");
            }

            await OnSaveSuccessAsync(result);
            await _navigationService.GoBackAsync();

        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_EDIT_VM] Save error: {ex.Message}");
            await ShowErrorAsync("Save Error", ex.Message);
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    protected virtual async Task DeleteAsync()
    {
        try
        {
            if (!EntityId.HasValue || IsSystemDefault) return;

            var confirmed = await Application.Current?.MainPage?.DisplayAlert(
                "Confirm Delete",
                $"Delete this {EntityName.ToLower()}? This action cannot be undone.",
                "Delete",
                "Cancel");

            if (confirmed != true) return;

            IsBusy = true;
            LoadingMessage = $"Deleting {EntityName.ToLower()}...";

            await _repository.DeleteAsync(EntityId.Value);
            Debug.WriteLine($"✅ [BASE_EDIT_VM] Deleted {EntityName}: {EntityId}");

            await _navigationService.GoBackAsync();

        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_EDIT_VM] Delete error: {ex.Message}");
            await ShowErrorAsync("Delete Error", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    protected virtual async Task CancelAsync()
    {
        if (HasUnsavedChanges)
        {
            var canDiscard = await Application.Current?.MainPage?.DisplayAlert(
                "Unsaved Changes",
                "You have unsaved changes. Discard them?",
                "Discard",
                "Continue Editing");

            if (canDiscard != true) return;
        }

        await _navigationService.GoBackAsync();
    }

    #endregion

    #region Abstract Methods

    /// <summary>
    /// Cria entidade a partir das propriedades atuais
    /// </summary>
    protected virtual async Task<T> CreateEntityFromPropertiesAsync()
    {
        var entity = new T
        {
            Id = EntityId ?? Guid.NewGuid(),
            Name = Name.Trim(),
            Description = Description?.Trim(),
            IsActive = IsActive,
            CreatedAt = _isEditMode ? CreatedAt : DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return await Task.FromResult(entity);
    }

    /// <summary>
    /// Chamado após save bem-sucedido
    /// </summary>
    protected virtual async Task OnSaveSuccessAsync(T entity)
    {
        await Task.CompletedTask;
    }

    #endregion

    #region Connectivity

    protected virtual async Task TestConnectionInBackgroundAsync()
    {
        try
        {
            await Task.Delay(100); // Simular teste
            IsConnected = true;
            ConnectionStatus = "Connected";
            ConnectionStatusColor = Colors.Green;
        }
        catch
        {
            IsConnected = false;
            ConnectionStatus = "Offline";
            ConnectionStatusColor = Colors.Orange;
        }
    }

    #endregion
}