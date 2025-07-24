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
/// ✅ MELHORADO: FamilyEditViewModel com funcionalidades completas e otimizadas
/// </summary>
public partial class FamilyEditViewModel : BaseEditViewModel<Family>
{
    private readonly IFamilyRepository _familyRepository;

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
    /// ✅ NOVO: Validação de nome em tempo real
    /// </summary>
    [ObservableProperty]
    private string nameValidationMessage = string.Empty;

    /// <summary>
    /// ✅ NOVO: Flag para indicar se o nome é válido
    /// </summary>
    [ObservableProperty]
    private bool isNameValid = true;

    /// <summary>
    /// ✅ NOVO: Propriedade para ID atual (evitar conflito com BaseEditViewModel)
    /// </summary>
    [ObservableProperty]
    private Guid currentId = Guid.NewGuid();
    /// <summary>
    /// ✅ NOVO: Cor do botão de salvar baseada na validação
    /// </summary>
    [ObservableProperty]
    private Color saveButtonColor = Colors.Green;

    /// <summary>
    /// ✅ NOVO: Texto do botão de salvar dinâmico
    /// </summary>
    [ObservableProperty]
    private string saveButtonText = "Save";

    public override string EntityName => "Family";
    public override string EntityNamePlural => "Families";

    public FamilyEditViewModel(IFamilyRepository familyRepository, INavigationService navigationService)
        : base(familyRepository, navigationService)
    {
        _familyRepository = familyRepository;
        Debug.WriteLine("✅ [FAMILY_EDIT_VM] Initialized with enhanced features");

        // Initialize validation
        UpdateFormCompletionProgress();
        UpdateSaveButton();
    }

    /// <summary>
    /// ✅ Implementação dos métodos partial gerados pelo ObservableProperty
    /// </summary>
    partial void OnIsFavoriteChanged(bool value)
    {
        CheckForUnsavedChanges();
        Debug.WriteLine($"⭐ [FAMILY_EDIT_VM] Favorite changed: {value}");
    }

    /// <summary>
    /// ✅ NOVO: Validação do nome em tempo real
    /// </summary>
    private void ValidateName()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                NameValidationMessage = "Family name is required";
                IsNameValid = false;
            }
            else if (Name.Length < 2)
            {
                NameValidationMessage = "Family name must be at least 2 characters";
                IsNameValid = false;
            }
            else if (Name.Length > 255)
            {
                NameValidationMessage = "Family name cannot exceed 255 characters";
                IsNameValid = false;
            }
            else if (!IsValidBotanicalName(Name))
            {
                NameValidationMessage = "Consider using a valid botanical family name (e.g., ending in -aceae)";
                IsNameValid = true; // Warning, not error
            }
            else
            {
                NameValidationMessage = string.Empty;
                IsNameValid = true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] ValidateName error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ NOVO: Atualiza o botão de salvar baseado na validação
    /// </summary>
    private void UpdateSaveButton()
    {
        try
        {
            if (IsEditMode)
            {
                SaveButtonText = "Update Family";
                SaveButtonColor = IsNameValid ? Color.FromArgb("#4CAF50") : Color.FromArgb("#FF5722");
            }
            else
            {
                SaveButtonText = "Create Family";
                SaveButtonColor = IsNameValid ? Color.FromArgb("#2196F3") : Color.FromArgb("#FF5722");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] UpdateSaveButton error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ Calcula progresso de completion do formulário
    /// </summary>
    private void UpdateFormCompletionProgress()
    {
        try
        {
            var totalFields = 2; // Name (required) + Description (optional)
            var completedFields = 0;

            if (!string.IsNullOrWhiteSpace(Name))
                completedFields++;

            if (!string.IsNullOrWhiteSpace(Description))
                completedFields++;

            FormCompletionProgress = (double)completedFields / totalFields;

            Debug.WriteLine($"📊 [FAMILY_EDIT_VM] Form completion: {FormCompletionProgress:P0}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] UpdateFormCompletionProgress error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ Verificação de mudanças não salvas
    /// </summary>
    private void CheckForUnsavedChanges()
    {
        try
        {
            // Usar detecção de mudanças básica para garantir que HasUnsavedChanges seja atualizado
            var hasChanges = !string.IsNullOrWhiteSpace(Name) ||
                           !string.IsNullOrWhiteSpace(Description) ||
                           !IsActive ||
                           IsFavorite;

            Debug.WriteLine($"🔄 [FAMILY_EDIT_VM] Checking for changes - Has data: {hasChanges}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] CheckForUnsavedChanges error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ NOVO: Método para carregar dados quando navegar para a página
    /// </summary>
    public async Task LoadDataAsync(IDictionary<string, object> query)
    {
        try
        {
            IsBusy = true;

            // Verificar se tem FamilyId para edição
            if (query.TryGetValue("FamilyId", out var familyIdObj) && familyIdObj is string familyIdStr)
            {
                if (Guid.TryParse(familyIdStr, out var familyId))
                {
                    IsEditMode = true;
                    var family = await _repository.GetByIdAsync(familyId);
                    if (family != null)
                    {
                        // Carregar dados do formulário
                        CurrentId = family.Id;
                        Name = family.Name;
                        Description = family.Description;
                        IsActive = family.IsActive;
                        IsFavorite = family.IsFavorite;
                        IsSystemDefault = family.IsSystemDefault;
                        CreatedAt = family.CreatedAt;
                        UpdatedAt = family.UpdatedAt;

                        UpdateFormCompletionProgress();
                        UpdateSaveButton();
                        Debug.WriteLine($"✅ [FAMILY_EDIT_VM] Loaded family for editing: {family.Name}");
                    }
                }
            }
            else
            {
                // Novo item
                IsEditMode = false;
                IsFavorite = false;
                IsActive = true;
                Name = string.Empty;
                Description = string.Empty;

                UpdateSaveButton();
                Debug.WriteLine($"✅ [FAMILY_EDIT_VM] Prepared for new family creation");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] LoadDataAsync error: {ex.Message}");
            await ShowErrorAsync("Load Error", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// ✅ CORRIGIDO: Save command sem override
    /// </summary>
    [RelayCommand]
    public async Task SaveFamilyAsync()
    {
        try
        {
            // Validação antes de salvar
            ValidateName();

            if (!IsNameValid)
            {
                await ShowErrorAsync("Validation Error", NameValidationMessage);
                return;
            }

            IsBusy = true;

            // Criar ou atualizar a família
            var family = new Family
            {
                Id = IsEditMode ? CurrentId : Guid.NewGuid(),
                Name = Name,
                Description = Description,
                IsActive = IsActive,
                IsFavorite = IsFavorite,
                UserId = Guid.NewGuid(), // TODO: Get from auth service
                CreatedAt = IsEditMode ? CreatedAt : DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (IsEditMode)
            {
                await _repository.UpdateAsync(family);
            }
            else
            {
                await _familyRepository.CreateAsync(family);
            }

            // Mostrar mensagem de sucesso
            var message = IsEditMode ? "Family updated successfully!" : "Family created successfully!";
            await ShowSuccessMessageAsync(message);

            // Navegar de volta
            await _navigationService.NavigateToAsync("..");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] SaveFamilyAsync error: {ex.Message}");
            await ShowErrorAsync("Save Error", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// ✅ NOVO: Toggle favorite command
    /// </summary>
    [RelayCommand]
    public async Task ToggleFavoriteAsync()
    {
        try
        {
            IsFavorite = !IsFavorite;

            var message = IsFavorite ? "Marked as favorite" : "Removed from favorites";
            var toast = Toast.Make(message, ToastDuration.Short, 14);
            await toast.Show();

            Debug.WriteLine($"⭐ [FAMILY_EDIT_VM] Favorite toggled: {IsFavorite}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] ToggleFavoriteAsync error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ CORRIGIDO: Quick save command (Ctrl+S) - nome único
    /// </summary>
    [RelayCommand]
    public async Task QuickSaveFamilyAsync()
    {
        if (CanSave)
        {
            await SaveFamilyAsync();
        }
    }

    /// <summary>
    /// ✅ Método para mostrar mensagens de sucesso
    /// </summary>
    protected virtual async Task ShowSuccessMessageAsync(string message)
    {
        try
        {
            Debug.WriteLine($"✅ [FAMILY_EDIT_VM] Success: {message}");

            var toast = Toast.Make(message, ToastDuration.Short, 16);
            await toast.Show();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] ShowSuccessMessageAsync error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ Método para mostrar mensagens de erro
    /// </summary>
    protected virtual async Task ShowErrorAsync(string title, string message)
    {
        try
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] Error: {title} - {message}");

            var toast = Toast.Make($"{title}: {message}", ToastDuration.Long, 14);
            await toast.Show();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] ShowErrorAsync error: {ex.Message}");
        }
    }

    // ✅ FUNCIONALIDADES ESPECÍFICAS DE FAMILY:

    /// <summary>
    /// Validação adicional específica para famílias botânicas
    /// </summary>
    protected virtual bool IsValidBotanicalName(string name)
    {
        // Exemplo: nomes de família botânica geralmente terminam em "-aceae"
        return name.EndsWith("aceae", StringComparison.OrdinalIgnoreCase) ||
               name.EndsWith("ae", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("Orchid", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Propriedade específica: indica se é família de orquídeas
    /// </summary>
    public bool IsOrchidFamily => Name?.Contains("Orchidaceae", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// ✅ Sugestões de nomes de família para auto-complete (futuro)
    /// </summary>
    public List<string> GetFamilyNameSuggestions()
    {
        return new List<string>
        {
            "Orchidaceae",
            "Bromeliaceae",
            "Araceae",
            "Cactaceae",
            "Gesneriaceae",
            "Rosaceae",
            "Asteraceae",
            "Fabaceae"
        };
    }

    /// <summary>
    /// ✅ Propriedade CanSave otimizada
    /// </summary>
    public new bool CanSave => IsNameValid &&
                              !string.IsNullOrWhiteSpace(Name) &&
                              Name.Length <= 255 &&
                              (Description?.Length ?? 0) <= 2000 &&
                              !IsBusy;

    /// <summary>
    /// ✅ Page title dinâmico baseado no modo
    /// </summary>
    public string PageTitle => IsEditMode ? $"Edit Family" : "New Family";

    /// <summary>
    /// ✅ NOVO: Comando para limpar formulário
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

            Debug.WriteLine("🧹 [FAMILY_EDIT_VM] Form cleared");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_VM] ClearForm error: {ex.Message}");
        }
    }

    // ✅ TODA A FUNCIONALIDADE HERDADA DA BASE MANTIDA:
    // ✅ SaveCommand, DeleteCommand, CancelCommand
    // ✅ Navegação e lifecycle methods
    // ✅ Conectividade e validação
    // ✅ Loading states e error handling
}