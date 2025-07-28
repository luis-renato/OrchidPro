using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models.Base;
using OrchidPro.Services.Navigation;
using OrchidPro.Services;
using System.Diagnostics;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace OrchidPro.ViewModels.Base;

/// <summary>
/// ✅ ENHANCED: BaseEditViewModel com todas as funcionalidades extraídas do Family
/// </summary>
public abstract partial class BaseEditViewModel<T> : BaseViewModel, IQueryAttributable
    where T : class, IBaseEntity, new()
{
    protected readonly IBaseRepository<T> _repository;
    protected readonly INavigationService _navigationService;

    private T? _originalEntity;
    protected bool _isEditMode; // ✅ CORRIGIDO: protected para classes filhas acessarem
    private Timer? _validationTimer;
    private readonly int _validationDelay = 800; // 800ms debounce padrão
    /// <summary>
    /// ✅ NOVO: Lista genérica para validação de nomes únicos
    /// </summary>
    private List<T> _allEntities = new();
    private bool _isInitializing = true;

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

    // ✅ NOVO: Validation framework genérico
    [ObservableProperty]
    private bool isNameValid = true;

    [ObservableProperty]
    private bool isDescriptionValid = true;

    [ObservableProperty]
    private string nameError = string.Empty;

    [ObservableProperty]
    private string descriptionError = string.Empty;

    [ObservableProperty]
    private string nameValidationMessage = string.Empty;

    [ObservableProperty]
    private bool isValidatingName;

    // ✅ NOVO: Form progress calculation genérico
    [ObservableProperty]
    private double formCompletionProgress;

    // ✅ NOVO: Save/Cancel framework genérico
    [ObservableProperty]
    private bool canSave;

    [ObservableProperty]
    private bool canDelete;

    [ObservableProperty]
    private bool isSaving;

    [ObservableProperty]
    private string saveButtonText = "Save";

    [ObservableProperty]
    private Color saveButtonColor = Colors.Green;

    // ✅ NOVO: Connection testing framework
    [ObservableProperty]
    private string connectionStatus = "Connected";

    [ObservableProperty]
    private Color connectionStatusColor = Colors.Green;

    [ObservableProperty]
    private bool isConnected = true;

    [ObservableProperty]
    private string connectionTestResult = "";

    // ✅ NOVO: Loading framework
    [ObservableProperty]
    private string loadingMessage = "Loading...";

    [ObservableProperty]
    private bool isNameFocused;

    [ObservableProperty]
    private bool isDescriptionFocused;

    // ✅ NOVO: IsFavorite genérico para todas entidades
    [ObservableProperty]
    private bool isFavorite;

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

    #region ✅ NOVO: Generic Commands Framework

    /// <summary>
    /// ✅ NOVO: Comando genérico de salvar - pode ser overridden
    /// </summary>
    [RelayCommand]
    public virtual async Task SaveAsync()
    {
        if (!CanSave || IsSaving) return;

        try
        {
            IsSaving = true;
            LoadingMessage = IsEditMode ? $"Updating {EntityName.ToLower()}..." : $"Creating {EntityName.ToLower()}...";
            IsBusy = true;

            // Validar antes de salvar
            if (!await ValidateEntityAsync())
            {
                await ShowValidationErrorsAsync();
                return;
            }

            // Criar/atualizar entidade
            T entity = await PrepareEntityForSaveAsync();
            T savedEntity;

            if (IsEditMode && EntityId.HasValue)
            {
                savedEntity = await _repository.UpdateAsync(entity);
                await ShowSuccessAsync("Success", $"{EntityName} updated successfully");
            }
            else
            {
                savedEntity = await _repository.CreateAsync(entity);
                await ShowSuccessAsync("Success", $"{EntityName} created successfully");
            }

            // Atualizar dados locais
            await UpdateLocalDataFromSavedEntity(savedEntity);

            // Marcar como salvo
            HasUnsavedChanges = false;
            _originalEntity = (T)savedEntity.Clone();

            // Notificar sucesso
            await OnSaveSuccessAsync(savedEntity);

            // Navegar de volta
            await NavigateBackAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_EDIT_VM] Save error: {ex.Message}");
            await ShowErrorAsync("Save Error", ex.Message);
        }
        finally
        {
            IsSaving = false;
            IsBusy = false;
        }
    }

    /// <summary>
    /// ✅ NOVO: Comando genérico de cancelar - pode ser overridden
    /// </summary>
    [RelayCommand]
    public virtual async Task CancelAsync()
    {
        try
        {
            Debug.WriteLine($"🔄 [BASE_EDIT_VM] Cancel command - HasUnsavedChanges: {HasUnsavedChanges}");

            if (HasUnsavedChanges)
            {
                bool shouldDiscard = await ShowConfirmationAsync(
                    "Discard Changes?",
                    "You have unsaved changes. Are you sure you want to discard them?",
                    "Discard",
                    "Keep Editing"
                );

                if (!shouldDiscard)
                {
                    Debug.WriteLine("✅ [BASE_EDIT_VM] User chose to keep editing");
                    return;
                }

                Debug.WriteLine("✅ [BASE_EDIT_VM] User chose to discard changes");
            }

            // Limpar mudanças não salvas
            HasUnsavedChanges = false;

            // Navegar de volta
            await NavigateBackAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_EDIT_VM] Cancel error: {ex.Message}");
            await ShowErrorAsync("Error", "An error occurred while canceling");
        }
    }

    /// <summary>
    /// ✅ NOVO: Comando de teste de conexão genérico
    /// </summary>
    [RelayCommand]
    public virtual async Task TestConnectionAsync()
    {
        try
        {
            ConnectionTestResult = "Testing...";
            IsConnected = false;
            ConnectionStatus = "Testing...";
            ConnectionStatusColor = Colors.Orange;

            // Simular teste de conexão
            await Task.Delay(1000);

            // Testar com repository
            var testResult = await _repository.GetAllAsync();

            IsConnected = true;
            ConnectionStatus = "Connected";
            ConnectionStatusColor = Colors.Green;
            ConnectionTestResult = "Connection successful";

            await ShowSuccessAsync("Connection", "Connection test successful");
        }
        catch (Exception ex)
        {
            IsConnected = false;
            ConnectionStatus = "Disconnected";
            ConnectionStatusColor = Colors.Red;
            ConnectionTestResult = $"Connection failed: {ex.Message}";

            await ShowErrorAsync("Connection Error", ex.Message);
        }
    }

    #endregion

    #region ✅ NOVO: Generic Validation Framework

    /// <summary>
    /// ✅ NOVO: Setup do framework de validação
    /// </summary>
    protected virtual void SetupValidation()
    {
        PropertyChanged += OnPropertyChangedForValidation;
        Debug.WriteLine($"✅ [BASE_EDIT_VM] Validation framework setup for {EntityName}");
    }

    /// <summary>
    /// ✅ CORRIGIDO: Handler de mudanças para validação + atualização do botão
    /// </summary>
    private void OnPropertyChangedForValidation(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_isInitializing) return;

        // Detectar mudanças e marcar como não salvo
        if (e.PropertyName is nameof(Name) or nameof(Description) or nameof(IsActive) or nameof(IsFavorite))
        {
            HasUnsavedChanges = true;
            UpdateFormCompletionProgress();

            // Validação com debounce para Name
            if (e.PropertyName == nameof(Name))
            {
                ScheduleNameValidation();
            }
            else
            {
                // ✅ IMPORTANTE: Para outras propriedades, atualizar botão imediatamente
                UpdateSaveButton();
            }
        }
    }

    /// <summary>
    /// ✅ NOVO: Agenda validação do nome com debounce
    /// </summary>
    protected virtual void ScheduleNameValidation()
    {
        _validationTimer?.Dispose();
        _validationTimer = new Timer(async _ => await ValidateNameWithDebounce(), null, _validationDelay, Timeout.Infinite);
    }

    /// <summary>
    /// ✅ CORRIGIDO: Validação do nome com debounce + atualização do botão
    /// </summary>
    private async Task ValidateNameWithDebounce()
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                IsValidatingName = true;
                UpdateSaveButton(); // ✅ Atualizar botão durante validação

                await ValidateNameAsync();

                IsValidatingName = false;
                UpdateSaveButton(); // ✅ Atualizar botão após validação
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_EDIT_VM] Name validation error: {ex.Message}");
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                IsValidatingName = false;
                IsNameValid = false;
                NameValidationMessage = "Validation error";
                UpdateSaveButton(); // ✅ Atualizar botão em caso de erro
            });
        }
    }

    /// <summary>
    /// ✅ GENÉRICO: Validação de nome único contra todas entidades + atualização botão
    /// </summary>
    protected virtual async Task ValidateNameAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            IsNameValid = false;
            NameValidationMessage = $"{EntityName} name is required";
            UpdateSaveButton();
            return;
        }

        if (Name.Length < 2)
        {
            IsNameValid = false;
            NameValidationMessage = $"{EntityName} name must be at least 2 characters";
            UpdateSaveButton();
            return;
        }

        // ✅ GENÉRICO: Verificar duplicação de nome
        var existingEntity = _allEntities.FirstOrDefault(e =>
            e.Name.Equals(Name.Trim(), StringComparison.OrdinalIgnoreCase) &&
            e.Id != EntityId);

        if (existingEntity != null)
        {
            IsNameValid = false;
            NameValidationMessage = $"A {EntityName.ToLower()} with this name already exists";
            UpdateSaveButton();
            return;
        }

        IsNameValid = true;
        NameValidationMessage = string.Empty;
        UpdateSaveButton(); // ✅ IMPORTANTE: Sempre atualizar botão após validação
        Debug.WriteLine($"✅ [BASE_EDIT_VM] Name validation passed: {Name}");
    }

    /// <summary>
    /// ✅ VIRTUAL: Validação completa da entidade
    /// </summary>
    protected virtual async Task<bool> ValidateEntityAsync()
    {
        await ValidateNameAsync();

        // Validação genérica básica
        bool isValid = IsNameValid && !string.IsNullOrWhiteSpace(Name);

        return isValid;
    }

    /// <summary>
    /// ✅ NOVO: Mostra erros de validação
    /// </summary>
    protected virtual async Task ShowValidationErrorsAsync()
    {
        if (!IsNameValid)
        {
            await ShowErrorAsync("Validation Error", NameValidationMessage);
        }
    }

    #endregion

    #region ✅ NOVO: Form Progress Framework

    /// <summary>
    /// ✅ NOVO: Atualiza progresso do formulário
    /// </summary>
    protected virtual void UpdateFormCompletionProgress()
    {
        int totalFields = GetTotalFormFields();
        int completedFields = GetCompletedFormFields();

        FormCompletionProgress = totalFields > 0 ? (double)completedFields / totalFields : 0;

        Debug.WriteLine($"📊 [BASE_EDIT_VM] Form progress: {completedFields}/{totalFields} = {FormCompletionProgress:P0}");
    }

    /// <summary>
    /// ✅ GENÉRICO: Total de campos do formulário padrão
    /// </summary>
    protected virtual int GetTotalFormFields()
    {
        return 3; // Name + Description + IsFavorite por padrão
    }

    /// <summary>
    /// ✅ GENÉRICO: Campos completados padrão
    /// </summary>
    protected virtual int GetCompletedFormFields()
    {
        int completed = 0;

        if (!string.IsNullOrWhiteSpace(Name)) completed++;
        if (!string.IsNullOrWhiteSpace(Description)) completed++;
        if (IsFavorite) completed++; // IsFavorite conta se estiver marcado

        return completed;
    }

    /// <summary>
    /// ✅ CORRIGIDO: Atualiza estado do botão salvar considerando TODAS as validações
    /// </summary>
    protected virtual void UpdateSaveButton()
    {
        // ✅ CORRIGIDO: CanSave deve considerar IsNameValid também
        CanSave = IsNameValid &&
                 IsDescriptionValid &&
                 !string.IsNullOrWhiteSpace(Name) &&
                 !IsSaving &&
                 !IsBusy &&
                 !IsValidatingName; // ✅ NOVO: Não permitir save durante validação

        SaveButtonColor = CanSave ? Colors.Green : Colors.Gray;
        SaveButtonText = IsEditMode ? "Update" : "Create";

        Debug.WriteLine($"🔘 [BASE_EDIT_VM] UpdateSaveButton - CanSave: {CanSave}");
        Debug.WriteLine($"    - IsNameValid: {IsNameValid}");
        Debug.WriteLine($"    - IsDescriptionValid: {IsDescriptionValid}");
        Debug.WriteLine($"    - Name not empty: {!string.IsNullOrWhiteSpace(Name)}");
        Debug.WriteLine($"    - Not saving: {!IsSaving}");
        Debug.WriteLine($"    - Not busy: {!IsBusy}");
        Debug.WriteLine($"    - Not validating: {!IsValidatingName}");
    }

    #endregion

    #region ✅ NOVO: Navigation Framework

    /// <summary>
    /// ✅ NOVO: Navega de volta
    /// </summary>
    protected virtual async Task NavigateBackAsync()
    {
        try
        {
            Debug.WriteLine($"🔙 [BASE_EDIT_VM] Navigating back from {EntityName}");
            await _navigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_EDIT_VM] Navigation error: {ex.Message}");
        }
    }

    #endregion

    #region Computed Properties

    public string PageTitle => IsEditMode ? $"Edit {EntityName}" : $"Add {EntityName}";
    public string PageSubtitle => IsEditMode ? $"Modify {EntityName.ToLower()} information" : $"Create a new {EntityName.ToLower()}";
    public bool IsEditMode => _isEditMode;

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

        // ✅ GENÉRICO: Carregar todas entidades para validação
        _ = LoadAllEntitiesForValidationAsync();

        SetupValidation();

        Debug.WriteLine($"✅ [BASE_EDIT_VM] Enhanced initialized for {EntityName}");
    }

    /// <summary>
    /// ✅ GENÉRICO: Carrega todas as entidades para validação de nome único
    /// </summary>
    protected virtual async Task LoadAllEntitiesForValidationAsync()
    {
        try
        {
            var entities = await _repository.GetAllAsync();
            _allEntities = entities?.ToList() ?? new List<T>();
            Debug.WriteLine($"📋 [BASE_EDIT_VM] Loaded {_allEntities.Count} {EntityNamePlural.ToLower()} for validation");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_EDIT_VM] LoadAllEntitiesForValidation error: {ex.Message}");
            _allEntities = new List<T>();
        }
    }

    #endregion

    #region ✅ Navigation and Query Attributes

    /// <summary>
    /// ✅ Aplica parâmetros de navegação de forma flexível
    /// </summary>
    public virtual void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        try
        {
            Debug.WriteLine($"🔍 [BASE_EDIT_VM] ApplyQueryAttributes for {EntityName} with {query.Count} parameters");

            // Verificar múltiplas variações de chave
            Guid? entityId = null;

            var possibleKeys = new[] { $"{EntityName}Id", "Id", "EntityId", "id" };
            foreach (var key in possibleKeys)
            {
                if (query.TryGetValue(key, out var value))
                {
                    entityId = ConvertToGuid(value);
                    if (entityId.HasValue)
                    {
                        Debug.WriteLine($"📝 [BASE_EDIT_VM] Found {key} parameter: {entityId}");
                        break;
                    }
                }
            }

            if (entityId.HasValue && entityId.Value != Guid.Empty)
            {
                EntityId = entityId;
                _isEditMode = true;
                Title = $"Edit {EntityName}";
                SaveButtonText = "Update";
                Debug.WriteLine($"✅ [BASE_EDIT_VM] EDIT MODE for {EntityName} ID: {entityId}");

                // Carregar dados da entidade
                _ = LoadEntityAsync();
            }
            else
            {
                EntityId = null;
                _isEditMode = false;
                Title = $"New {EntityName}";
                SaveButtonText = "Create";
                Debug.WriteLine($"✅ [BASE_EDIT_VM] CREATE MODE for {EntityName}");

                // Finalizar inicialização para modo criação
                _isInitializing = false;
                HasUnsavedChanges = false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_EDIT_VM] ApplyQueryAttributes error: {ex.Message}");
            // Em caso de erro, assumir modo criação
            _isEditMode = false;
            Title = $"New {EntityName}";
        }
    }

    /// <summary>
    /// ✅ NOVO: Converte object para Guid de forma segura
    /// </summary>
    private Guid? ConvertToGuid(object obj)
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

    #region ✅ VIRTUAL: Entity Loading and Preparation

    /// <summary>
    /// ✅ VIRTUAL: Carrega dados da entidade para edição
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
    /// ✅ VIRTUAL: Popula propriedades a partir da entidade
    /// </summary>
    protected virtual async Task PopulateFromEntityAsync(T entity)
    {
        // Desabilitar processamento de mudanças durante carregamento
        _isInitializing = true;

        Name = entity.Name;
        Description = entity.Description ?? string.Empty;
        IsActive = entity.IsActive;
        IsFavorite = entity.IsFavorite;
        IsSystemDefault = entity.IsSystemDefault;
        CreatedAt = entity.CreatedAt;
        UpdatedAt = entity.UpdatedAt;

        // Finalizar inicialização
        _isInitializing = false;
        HasUnsavedChanges = false;

        // Atualizar UI
        UpdateFormCompletionProgress();
        UpdateSaveButton();

        Debug.WriteLine($"✅ [BASE_EDIT_VM] Populated from {EntityName}: {Name}");
    }

    /// <summary>
    /// ✅ VIRTUAL: Prepara entidade para salvar
    /// </summary>
    protected virtual async Task<T> PrepareEntityForSaveAsync()
    {
        T entity = new T
        {
            Id = EntityId ?? Guid.NewGuid(),
            Name = Name.Trim(),
            Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
            IsActive = IsActive,
            IsFavorite = IsFavorite,
            UserId = null, // Será definido pelo repository se necessário
            CreatedAt = IsEditMode ? CreatedAt : DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        Debug.WriteLine($"✅ [BASE_EDIT_VM] Prepared {EntityName} for save: {entity.Name}");
        return entity;
    }

    /// <summary>
    /// ✅ VIRTUAL: Atualiza dados locais após salvar
    /// </summary>
    protected virtual async Task UpdateLocalDataFromSavedEntity(T savedEntity)
    {
        EntityId = savedEntity.Id;
        UpdatedAt = savedEntity.UpdatedAt;

        Debug.WriteLine($"✅ [BASE_EDIT_VM] Updated local data from saved {EntityName}");
    }

    /// <summary>
    /// ✅ VIRTUAL: Callback após salvar com sucesso
    /// </summary>
    protected virtual async Task OnSaveSuccessAsync(T savedEntity)
    {
        // Implementar na classe filha se necessário
        Debug.WriteLine($"✅ [BASE_EDIT_VM] {EntityName} saved successfully");
    }

    #endregion

    #region ✅ NOVO: Toast and Alert Framework

    /// <summary>
    /// ✅ NOVO: Mostra toast de sucesso
    /// </summary>
    protected virtual async Task ShowSuccessAsync(string title, string message)
    {
        try
        {
            var toast = Toast.Make(message, ToastDuration.Short, 16);
            await toast.Show();
            Debug.WriteLine($"✅ [BASE_EDIT_VM] Success toast: {message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_EDIT_VM] Toast error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ NOVO: Mostra toast de erro
    /// </summary>
    protected virtual async Task ShowErrorAsync(string title, string message)
    {
        try
        {
            var toast = Toast.Make($"❌ {message}", ToastDuration.Long, 16);
            await toast.Show();
            Debug.WriteLine($"❌ [BASE_EDIT_VM] Error toast: {message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_EDIT_VM] Error toast failed: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ NOVO: Mostra confirmação
    /// </summary>
    protected virtual async Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel)
    {
        try
        {
            bool result = await Application.Current?.MainPage?.DisplayAlert(title, message, accept, cancel);
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_EDIT_VM] Confirmation dialog error: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region ✅ NOVO: Lifecycle Management

    /// <summary>
    /// ✅ NOVO: Cleanup de recursos
    /// </summary>
    public void Dispose()
    {
        _validationTimer?.Dispose();
        Debug.WriteLine($"🗑️ [BASE_EDIT_VM] Disposed resources for {EntityName}");
    }

    #endregion
}