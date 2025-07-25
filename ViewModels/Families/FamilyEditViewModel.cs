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
/// ✅ CORRIGIDO: FamilyEditViewModel com navegação e carregamento correto de dados
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

    public override string EntityName => "Family";
    public override string EntityNamePlural => "Families";

    public FamilyEditViewModel(IFamilyRepository familyRepository, INavigationService navigationService)
        : base(familyRepository, navigationService)
    {
        _familyRepository = familyRepository;
        Debug.WriteLine("✅ [FAMILY_EDIT_VM] Initialized with real-time validation");

        // Initialize
        UpdateFormCompletionProgress();
        UpdateSaveButton();

        // Carregar dados para validação
        _ = LoadAllFamiliesForValidationAsync();
    }

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

        Debug.WriteLine($"⚠️ [FAMILY_EDIT_VM] Cannot convert {obj} to Guid");
        return null;
    }

    #endregion

    #region ✅ CORRIGIDA: Override do PopulateFromEntityAsync

    /// <summary>
    /// ✅ CORRIGIDO: Sobrescreve método para carregar propriedades específicas da Family
    /// </summary>
    protected override async Task PopulateFromEntityAsync(Family entity)
    {
        try
        {
            // Chamar o método base primeiro
            await base.PopulateFromEntityAsync(entity);

            // ✅ CORREÇÃO CRÍTICA: Configurar propriedades específicas da Family
            IsFavorite = entity.IsFavorite;

            // ✅ CORREÇÃO CRÍTICA: Atualizar título para modo de edição
            IsEditMode = true;
            Title = "Edit Family";
            SaveButtonText = "Update";

            // Atualizar UI
            UpdateFormCompletionProgress();
            UpdateSaveButton();

            Debug.WriteLine($"✅ [FAMILY_EDIT_VM] PopulateFromEntityAsync completed - Name: '{Name}', Favorite: {IsFavorite}, Active: {IsActive}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] PopulateFromEntityAsync error: {ex.Message}");
            throw; // Re-throw para que a UI possa lidar com o erro
        }
    }

    #endregion

    #region ✅ CORRIGIDA: Validação de Nome em Tempo Real

    /// <summary>
    /// ✅ CORRIGIDO: Monitoring de mudanças no Name via PropertyChanged
    /// </summary>
    protected override void SetupValidation()
    {
        base.SetupValidation();

        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Name))
            {
                Debug.WriteLine($"🔤 [FAMILY_EDIT_VM] Name changed to: '{Name}'");

                // Cancel previous validation timer
                _validationTimer?.Dispose();

                // Reset validation state
                IsNameValid = true;
                NameValidationMessage = string.Empty;

                // Start new validation timer (debounce)
                if (!string.IsNullOrWhiteSpace(Name))
                {
                    _validationTimer = new Timer(async _ => await ValidateNameAsync(Name),
                                               null, _validationDelay, Timeout.Infinite);
                }

                UpdateFormCompletionProgress();
                UpdateSaveButton();
            }
        };
    }

    /// <summary>
    /// ✅ NOVO: Carrega todas as famílias para validação rápida
    /// </summary>
    private async Task LoadAllFamiliesForValidationAsync()
    {
        try
        {
            _allFamilies = await _familyRepository.GetAllAsync(true);
            Debug.WriteLine($"📋 [FAMILY_EDIT_VM] Loaded {_allFamilies.Count} families for validation");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] Failed to load families for validation: {ex.Message}");
            _allFamilies = new List<Family>();
        }
    }

    /// <summary>
    /// ✅ CORRIGIDO: Validação de nome em tempo real com debounce
    /// </summary>
    private async Task ValidateNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                IsNameValid = true;
                NameValidationMessage = string.Empty;
                IsValidatingName = false;
            });
            return;
        }

        Device.BeginInvokeOnMainThread(() => IsValidatingName = true);

        try
        {
            // ✅ OTIMIZAÇÃO: Usar cache local primeiro
            var existsInCache = _allFamilies.Any(f =>
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
    /// ✅ Implementação dos métodos partial gerados pelo ObservableProperty
    /// </summary>
    partial void OnIsFavoriteChanged(bool value)
    {
        CheckForUnsavedChanges();
        Debug.WriteLine($"⭐ [FAMILY_EDIT_VM] Favorite changed: {value}");
    }

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
    /// ✅ Verifica mudanças não salvas
    /// </summary>
    private void CheckForUnsavedChanges()
    {
        try
        {
            // Implementar lógica de verificação de mudanças
            bool hasChanges = !string.IsNullOrWhiteSpace(Name) ||
                              !string.IsNullOrWhiteSpace(Description) ||
                              !IsActive ||
                              IsFavorite;

            HasUnsavedChanges = hasChanges;
            Debug.WriteLine($"🔄 [FAMILY_EDIT_VM] Has unsaved changes: {hasChanges}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] CheckForUnsavedChanges error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ Page title dinâmico baseado no modo
    /// </summary>
    public string PageTitle => IsEditMode ? "Edit Family" : "New Family";

    /// <summary>
    /// ✅ Comando para limpar formulário
    /// </summary>
    [RelayCommand]
    public void ClearForm()
    {
        try
        {
            Name = string.Empty;
            Description = string.Empty;
            IsActive = true;
            IsFavorite = false;

            UpdateFormCompletionProgress();
            UpdateSaveButton();
            Debug.WriteLine("🧹 [FAMILY_EDIT_VM] Form cleared");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] ClearForm error: {ex.Message}");
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
        _validationTimer?.Dispose();
        _validationTimer = null;
        await base.OnDisappearingAsync();
    }

    #endregion
}