using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.Input;

namespace OrchidPro.ViewModels.Families;

/// <summary>
/// ✅ CORRIGIDO: FamilyEditViewModel com navegação e validação em tempo real
/// </summary>
public partial class FamilyEditViewModel : BaseEditViewModel<Family>, IQueryAttributable
{
    private readonly IFamilyRepository _familyRepository;
    private Timer? _validationTimer;
    private readonly int _validationDelay = 800; // 800ms debounce

    /// <summary>
    /// ✅ Propriedade IsFavorite para binding
    /// </summary>
    [ObservableProperty]
    private bool isFavorite;

    /// <summary>
    /// ✅ Progress da completion do formulário
    /// </summary>
    [ObservableProperty]
    private double formCompletionProgress;

    /// <summary>
    /// ✅ Campo para controlar se está em modo de edição
    /// </summary>
    [ObservableProperty]
    private bool isEditMode;

    /// <summary>
    /// ✅ CORRIGIDO: Validação de nome em tempo real com debounce
    /// </summary>
    [ObservableProperty]
    private string nameValidationMessage = string.Empty;

    /// <summary>
    /// ✅ CORRIGIDO: Flag para indicar se o nome é válido
    /// </summary>
    [ObservableProperty]
    private bool isNameValid = true;

    /// <summary>
    /// ✅ CORRIGIDO: Propriedade para ID atual - remove duplicação
    /// </summary>
    public Guid? CurrentFamilyId => EntityId;

    /// <summary>
    /// ✅ Cor do botão de salvar baseada na validação
    /// </summary>
    [ObservableProperty]
    private Color saveButtonColor = Colors.Green;

    /// <summary>
    /// ✅ Texto do botão de salvar dinâmico
    /// </summary>
    [ObservableProperty]
    private string saveButtonText = "Save";

    /// <summary>
    /// ✅ NOVO: Indica se está validando nome
    /// </summary>
    [ObservableProperty]
    private bool isValidatingName;

    /// <summary>
    /// ✅ NOVO: Lista de famílias para validação rápida
    /// </summary>
    private List<Family> _allFamilies = new();

    /// <summary>
    /// ✅ NOVO: Entidade original para comparação de mudanças
    /// </summary>
    private Family? _originalEntity;

    public override string EntityName => "Family";
    public override string EntityNamePlural => "Families";

    /// <summary>
    /// ✅ Page title dinâmico baseado no modo - CORRIGIDO COM NOTIFICAÇÃO
    /// </summary>
    public new string PageTitle => IsEditMode ? "Edit Family" : "New Family";

    public FamilyEditViewModel(IFamilyRepository familyRepository, INavigationService navigationService)
        : base(familyRepository, navigationService)
    {
        _familyRepository = familyRepository;
        Debug.WriteLine("✅ [FAMILY_EDIT_VM] Initialized with real-time validation");

        // ✅ IMPORTANTE: Inicializar comando Cancel customizado
        CancelCommand = new AsyncRelayCommand(CancelAsync);

        // Initialize
        UpdateFormCompletionProgress();
        UpdateSaveButton();

        // Carregar dados para validação
        _ = LoadAllFamiliesForValidationAsync();

        // ✅ IMPORTANTE: Configurar nosso PropertyChanged específico (após base já ter chamado SetupValidation)
        PropertyChanged += OnFamilyPropertyChanged;

        Debug.WriteLine("✅ [FAMILY_EDIT_VM] Custom PropertyChanged handler attached + CancelCommand overridden");
    }

    /// <summary>
    /// ✅ Flag para controlar se deve processar mudanças de propriedades
    /// </summary>
    private bool _isInitializing = true;

    /// <summary>
    /// ✅ NOVO: Handler específico para mudanças de propriedades da Family
    /// </summary>
    private void OnFamilyPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // ✅ IMPORTANTE: Ignorar mudanças durante inicialização
        if (_isInitializing)
        {
            Debug.WriteLine($"🔧 [FAMILY_EDIT_VM] Ignoring property change during initialization: {e.PropertyName}");
            return;
        }

        // ✅ Processar mudanças apenas quando não está inicializando
        switch (e.PropertyName)
        {
            case nameof(Name):
                StartNameValidation();
                UpdateFormCompletionProgress();
                CheckForUnsavedChanges(); // ✅ Nossa implementação
                break;
            case nameof(Description):
                UpdateFormCompletionProgress();
                CheckForUnsavedChanges(); // ✅ Nossa implementação
                break;
            case nameof(IsActive):
                UpdateFormCompletionProgress();
                CheckForUnsavedChanges(); // ✅ Nossa implementação
                break;
            case nameof(IsFavorite):
                CheckForUnsavedChanges(); // ✅ Nossa implementação
                break;
        }
    }

    /// <summary>
    /// ✅ SOBRESCREVER: SetupValidation da classe base para evitar PropertyChanged duplicado
    /// </summary>
    protected override void SetupValidation()
    {
        // ✅ NÃO chamar base.SetupValidation() para evitar PropertyChanged handler da base
        Debug.WriteLine("🔧 [FAMILY_EDIT_VM] SetupValidation overridden - NOT calling base to avoid duplicate PropertyChanged");

        // Nossa própria validação já está configurada no construtor
    }

    #region ✅ CORRIGIDA: Property Change Handlers

    /// <summary>
    /// ✅ Implementação dos métodos partial gerados pelo ObservableProperty
    /// </summary>
    partial void OnIsEditModeChanged(bool value)
    {
        OnPropertyChanged(nameof(PageTitle));
        UpdateSaveButton();
        Debug.WriteLine($"🔄 [FAMILY_EDIT_VM] Edit mode changed: {value}, PageTitle: {PageTitle}");
    }

    partial void OnIsFavoriteChanged(bool value)
    {
        CheckForUnsavedChanges();
        Debug.WriteLine($"⭐ [FAMILY_EDIT_VM] Favorite changed: {value}");
    }

    #endregion

    #region ✅ CORRIGIDA: Navegação e Query Attributes

    /// <summary>
    /// ✅ CORRIGIDO: Processa parâmetros de navegação corretamente
    /// </summary>
    public new void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        try
        {
            Debug.WriteLine($"🔍 [FAMILY_EDIT_VM] ApplyQueryAttributes called with {query.Count} parameters");

            // ✅ CORRIGIDO: Verificar diferentes formatos de parâmetro
            Guid? familyId = null;

            // Tentar diferentes chaves de parâmetro
            if (query.TryGetValue("FamilyId", out var familyIdObj))
            {
                familyId = ConvertToGuid(familyIdObj);
                Debug.WriteLine($"📝 [FAMILY_EDIT_VM] Found FamilyId parameter: {familyId}");
            }
            else if (query.TryGetValue("Id", out var idObj))
            {
                familyId = ConvertToGuid(idObj);
                Debug.WriteLine($"📝 [FAMILY_EDIT_VM] Found Id parameter: {familyId}");
            }

            if (familyId.HasValue && familyId.Value != Guid.Empty)
            {
                EntityId = familyId;
                IsEditMode = true;
                Title = "Edit Family";
                SaveButtonText = "Update";
                Debug.WriteLine($"✅ [FAMILY_EDIT_VM] EDIT MODE for Family ID: {familyId}");
            }
            else
            {
                EntityId = null;
                IsEditMode = false;
                Title = "New Family";
                SaveButtonText = "Create";
                Debug.WriteLine($"✅ [FAMILY_EDIT_VM] CREATE MODE");
            }

            // ✅ CHAMAR O MÉTODO DA BASE TAMBÉM
            base.ApplyQueryAttributes(query);

            // ✅ IMPORTANTE: Para modo criação, finalizar inicialização após setup
            if (!IsEditMode)
            {
                _isInitializing = false;
                HasUnsavedChanges = false;
                Debug.WriteLine($"✅ [FAMILY_EDIT_VM] Create mode - _isInitializing: {_isInitializing}, HasUnsavedChanges: {HasUnsavedChanges}");
            }

        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] ApplyQueryAttributes error: {ex.Message}");
            // Em caso de erro, assumir modo criação
            IsEditMode = false;
            Title = "New Family";
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

        Debug.WriteLine($"⚠️ [FAMILY_EDIT_VM] Cannot convert {obj} ({obj.GetType().Name}) to Guid");
        return null;
    }

    #endregion

    #region ✅ CARREGAMENTO DE DADOS

    /// <summary>
    /// ✅ NOVO: Carrega todas as famílias para validação rápida
    /// </summary>
    private async Task LoadAllFamiliesForValidationAsync()
    {
        try
        {
            var families = await _familyRepository.GetAllAsync();
            _allFamilies = families?.ToList() ?? new List<Family>();
            Debug.WriteLine($"📋 [FAMILY_EDIT_VM] Loaded {_allFamilies.Count} families for validation");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] LoadAllFamiliesForValidation error: {ex.Message}");
            _allFamilies = new List<Family>();
        }
    }

    /// <summary>
    /// ✅ CORRIGIDO: Carrega dados da família para edição
    /// </summary>
    protected override async Task LoadEntityAsync()
    {
        try
        {
            if (!EntityId.HasValue) return;

            LoadingMessage = "Loading family...";
            IsBusy = true;

            var family = await _familyRepository.GetByIdAsync(EntityId.Value);
            if (family != null)
            {
                _originalEntity = family; // ✅ Armazenar entidade original
                await PopulateFromFamilyAsync(family);
                Debug.WriteLine($"✅ [FAMILY_EDIT_VM] Loaded family: {Name}");
            }
            else
            {
                Debug.WriteLine($"❌ [FAMILY_EDIT_VM] Family not found: {EntityId}");
                await ShowErrorAsync("Not Found", "Family not found");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] LoadEntity error: {ex.Message}");
            await ShowErrorAsync("Load Error", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// ✅ NOVO: Popula propriedades a partir da família
    /// </summary>
    private async Task PopulateFromFamilyAsync(Family family)
    {
        // ✅ IMPORTANTE: Desabilitar processamento de mudanças durante carregamento
        _isInitializing = true;

        Name = family.Name;
        Description = family.Description ?? string.Empty;
        IsActive = family.IsActive;
        IsFavorite = family.IsFavorite;
        IsSystemDefault = family.IsSystemDefault;
        CreatedAt = family.CreatedAt;
        UpdatedAt = family.UpdatedAt;

        UpdateFormCompletionProgress();
        UpdateSaveButton();

        // ✅ IMPORTANTE: Reabilitar processamento e limpar estado de mudanças
        _isInitializing = false;
        HasUnsavedChanges = false;

        await Task.CompletedTask;

        Debug.WriteLine($"✅ [FAMILY_EDIT_VM] Family loaded - HasUnsavedChanges: {HasUnsavedChanges}, _isInitializing: {_isInitializing}");
    }

    #endregion

    #region ✅ VALIDAÇÃO EM TEMPO REAL

    /// <summary>
    /// ✅ NOVO: Inicia validação de nome com debounce
    /// </summary>
    private void StartNameValidation()
    {
        try
        {
            _validationTimer?.Dispose();

            if (string.IsNullOrWhiteSpace(Name))
            {
                IsNameValid = false;
                NameValidationMessage = "Name is required";
                IsValidatingName = false;
                UpdateSaveButton();
                return;
            }

            IsValidatingName = true;
            UpdateSaveButton();

            _validationTimer = new Timer(async _ => await ValidateNameAsync(), null, _validationDelay, Timeout.Infinite);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] StartNameValidation error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ NOVO: Validação assíncrona de nome
    /// </summary>
    private async Task ValidateNameAsync()
    {
        try
        {
            var name = Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    IsNameValid = false;
                    NameValidationMessage = "Name is required";
                    IsValidatingName = false;
                    UpdateSaveButton();
                });
                return;
            }

            // ✅ OTIMIZAÇÃO: Usar cache local primeiro
            bool existsInCache = _allFamilies.Any(f =>
                string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase) &&
                f.Id != (CurrentFamilyId ?? Guid.Empty));

            bool nameExists = existsInCache;

            // ✅ FALLBACK: Se cache vazio, consultar repositório
            if (_allFamilies.Count == 0)
            {
                nameExists = await _familyRepository.NameExistsAsync(name, CurrentFamilyId);
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                IsNameValid = !nameExists;
                NameValidationMessage = nameExists ? $"'{name}' already exists" : string.Empty;
                IsValidatingName = false;

                UpdateSaveButton();

                Debug.WriteLine($"🔍 [FAMILY_EDIT_VM] Name validation: '{name}' - Valid: {IsNameValid}");
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] Name validation error: {ex.Message}");
            Device.BeginInvokeOnMainThread(() =>
            {
                IsNameValid = true; // Assume válido em caso de erro
                NameValidationMessage = string.Empty;
                IsValidatingName = false;
            });
        }
    }

    #endregion

    #region ✅ Form Management

    /// <summary>
    /// ✅ Atualiza progresso do formulário
    /// </summary>
    private void UpdateFormCompletionProgress()
    {
        try
        {
            double progress = 0.0;
            int totalFields = 3;

            if (!string.IsNullOrWhiteSpace(Name)) progress += 1.0;
            if (!string.IsNullOrWhiteSpace(Description)) progress += 1.0;
            if (IsActive) progress += 1.0;

            FormCompletionProgress = progress / totalFields;
            Debug.WriteLine($"📊 [FAMILY_EDIT_VM] Form completion: {FormCompletionProgress:P0}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] UpdateFormCompletionProgress error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ Atualiza estado do botão salvar
    /// </summary>
    private void UpdateSaveButton()
    {
        try
        {
            bool canSave = !string.IsNullOrWhiteSpace(Name) &&
                          IsNameValid &&
                          !IsValidatingName &&
                          !IsBusy;

            SaveButtonColor = canSave ? Colors.Green : Colors.Gray;

            if (IsValidatingName)
                SaveButtonText = IsEditMode ? "Validating..." : "Validating...";
            else
                SaveButtonText = IsEditMode ? "Update" : "Create";

            Debug.WriteLine($"🔘 [FAMILY_EDIT_VM] Save button - Can save: {canSave}, Text: '{SaveButtonText}'");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] UpdateSaveButton error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ Verifica mudanças não salvas - CORRIGIDO para considerar estado original E valores padrão
    /// </summary>
    private void CheckForUnsavedChanges()
    {
        try
        {
            if (IsEditMode)
            {
                // Em modo de edição, comparar com a entidade original
                if (_originalEntity != null)
                {
                    HasUnsavedChanges =
                        Name != _originalEntity.Name ||
                        (Description ?? string.Empty) != (_originalEntity.Description ?? string.Empty) ||
                        IsActive != _originalEntity.IsActive ||
                        IsFavorite != _originalEntity.IsFavorite;
                }
                else
                {
                    // Se não há entidade original ainda, não há mudanças
                    HasUnsavedChanges = false;
                }
            }
            else
            {
                // Em modo de criação, verificar se o usuário preencheu algo além dos valores padrão
                // IMPORTANTE: Valores padrão são: Name="", Description="", IsActive=true, IsFavorite=false
                HasUnsavedChanges = !string.IsNullOrWhiteSpace(Name) ||
                                  !string.IsNullOrWhiteSpace(Description) ||
                                  !IsActive ||  // Padrão é true, então false = mudança
                                  IsFavorite;   // Padrão é false, então true = mudança
            }

            Debug.WriteLine($"🔄 [FAMILY_EDIT_VM] CheckForUnsavedChanges - Mode: {(IsEditMode ? "Edit" : "Create")}, HasUnsavedChanges: {HasUnsavedChanges}");
            Debug.WriteLine($"    Values - Name: '{Name}', Description: '{Description}', IsActive: {IsActive}, IsFavorite: {IsFavorite}");

            if (IsEditMode && _originalEntity != null)
            {
                Debug.WriteLine($"    Original - Name: '{_originalEntity.Name}', Description: '{_originalEntity.Description ?? ""}', IsActive: {_originalEntity.IsActive}, IsFavorite: {_originalEntity.IsFavorite}");
            }
            else if (!IsEditMode)
            {
                Debug.WriteLine($"    Create mode - Checking against defaults: Name='', Description='', IsActive=true, IsFavorite=false");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] CheckForUnsavedChanges error: {ex.Message}");
        }
    }

    #endregion

    #region ✅ SOBRESCREVER: Cancel Command com limpeza de estado

    /// <summary>
    /// ✅ SOBRESCREVE CancelCommand para garantir limpeza de estado após "Discard"
    /// </summary>
    public new IAsyncRelayCommand CancelCommand { get; private set; }

    /// <summary>
    /// ✅ Implementação do Cancel com limpeza de estado
    /// </summary>
    private async Task CancelAsync()
    {
        try
        {
            Debug.WriteLine($"🚫 [FAMILY_EDIT_VM] CancelAsync - HasUnsavedChanges: {HasUnsavedChanges}");

            if (HasUnsavedChanges)
            {
                var canDiscard = await Application.Current?.MainPage?.DisplayAlert(
                    "Unsaved Changes",
                    "You have unsaved changes. Discard them?",
                    "Discard",
                    "Continue Editing");

                if (canDiscard != true)
                {
                    Debug.WriteLine("⏸️ [FAMILY_EDIT_VM] User chose to continue editing");
                    return;
                }

                // ✅ IMPORTANTE: Limpar estado após usuário escolher "Discard"
                HasUnsavedChanges = false;
                Debug.WriteLine("🧹 [FAMILY_EDIT_VM] HasUnsavedChanges cleared after discard");
            }

            Debug.WriteLine("🔙 [FAMILY_EDIT_VM] Navigating back");
            await _navigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] CancelAsync error: {ex.Message}");
        }
    }

    #endregion

    #region ✅ CORRIGIDO: Save Command

    /// <summary>
    /// ✅ CORRIGIDO: Save command específico para Family
    /// </summary>
    [RelayCommand]
    public async Task SaveFamilyAsync()
    {
        try
        {
            // Validação final
            if (string.IsNullOrWhiteSpace(Name))
            {
                await ShowErrorAsync("Validation Error", "Name is required");
                return;
            }

            if (!IsNameValid)
            {
                await ShowErrorAsync("Validation Error", NameValidationMessage);
                return;
            }

            IsBusy = true;
            SaveButtonText = IsEditMode ? "Updating..." : "Creating...";

            // Criar ou atualizar a família
            var family = new Family
            {
                Id = IsEditMode ? (CurrentFamilyId ?? Guid.NewGuid()) : Guid.NewGuid(),
                Name = Name.Trim(),
                Description = Description?.Trim() ?? string.Empty,
                IsActive = IsActive,
                IsFavorite = IsFavorite
            };

            Family result;
            if (IsEditMode)
            {
                result = await _familyRepository.UpdateAsync(family);
                Debug.WriteLine($"✅ [FAMILY_EDIT_VM] Family updated: {result.Name}");

                var toast = Toast.Make($"'{result.Name}' updated successfully", ToastDuration.Short, 14);
                await toast.Show();
            }
            else
            {
                result = await _familyRepository.CreateAsync(family);
                Debug.WriteLine($"✅ [FAMILY_EDIT_VM] Family created: {result.Name}");

                var toast = Toast.Make($"'{result.Name}' created successfully", ToastDuration.Short, 14);
                await toast.Show();
            }

            // ✅ IMPORTANTE: Limpar estado de mudanças após save bem-sucedido
            HasUnsavedChanges = false;
            Debug.WriteLine($"✅ [FAMILY_EDIT_VM] Save successful - HasUnsavedChanges reset to: {HasUnsavedChanges}");

            // Navegar de volta
            await _navigationService.GoBackAsync();

        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] Save error: {ex.Message}");
            await ShowErrorAsync("Save Error", ex.Message);
        }
        finally
        {
            IsBusy = false;
            UpdateSaveButton();
        }
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// ✅ Cleanup resources
    /// </summary>
    public override async Task OnDisappearingAsync()
    {
        // ✅ IMPORTANTE: Remover nosso PropertyChanged handler
        PropertyChanged -= OnFamilyPropertyChanged;

        _validationTimer?.Dispose();
        _validationTimer = null;

        // ✅ IMPORTANTE: Reset estado de inicialização para próxima visita
        _isInitializing = true;
        HasUnsavedChanges = false;

        await base.OnDisappearingAsync();

        Debug.WriteLine($"🧹 [FAMILY_EDIT_VM] Cleanup completed - _isInitializing: {_isInitializing}, HasUnsavedChanges: {HasUnsavedChanges}");
    }

    #endregion
}