using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models.Base;
using System.Diagnostics;

namespace OrchidPro.ViewModels;

/// <summary>
/// PASSO 6.1: BaseItemViewModel corrigido - sem problemas de tipos genéricos
/// Representa um item individual em uma lista com funcionalidade de seleção
/// </summary>
public abstract partial class BaseItemViewModel<T> : ObservableObject where T : class, IBaseEntity
{
    [ObservableProperty]
    private bool isSelected;

    public Guid Id { get; }
    public string Name { get; }
    public string? Description { get; }
    public bool IsActive { get; }
    public bool IsSystemDefault { get; }
    public string DisplayName { get; }
    public string StatusDisplay { get; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; }

    /// <summary>
    /// ✅ CORRIGIDO: Command que aceita o próprio tipo para evitar conflitos
    /// </summary>
    public Action<BaseItemViewModel<T>>? SelectionChangedAction { get; set; }

    private readonly T _model;

    /// <summary>
    /// Nome da entidade (deve ser implementado pela classe filha)
    /// </summary>
    public abstract string EntityName { get; }

    protected BaseItemViewModel(T entity)
    {
        _model = entity;
        Id = entity.Id;
        Name = entity.Name;
        Description = entity.Description;
        IsActive = entity.IsActive;
        IsSystemDefault = entity.IsSystemDefault;
        DisplayName = entity.DisplayName;
        StatusDisplay = entity.StatusDisplay;
        CreatedAt = entity.CreatedAt;
        UpdatedAt = entity.UpdatedAt;

        Debug.WriteLine($"🔨 [BASE_ITEM_VM] Created for {EntityName}: {Name}");
    }

    /// <summary>
    /// Obtém o modelo subjacente
    /// </summary>
    public T ToModel() => _model;

    /// <summary>
    /// ✅ CORRIGIDO: Alterna estado de seleção usando Action simples
    /// </summary>
    [RelayCommand]
    private void ToggleSelection()
    {
        Debug.WriteLine($"🔘 [BASE_ITEM_VM] ToggleSelection called for {EntityName}: {Name}");
        Debug.WriteLine($"🔘 [BASE_ITEM_VM] Current IsSelected: {IsSelected}");

        IsSelected = !IsSelected;

        Debug.WriteLine($"🔘 [BASE_ITEM_VM] New IsSelected: {IsSelected}");
        Debug.WriteLine($"🔘 [BASE_ITEM_VM] SelectionChangedAction is null: {SelectionChangedAction == null}");

        if (SelectionChangedAction != null)
        {
            Debug.WriteLine($"🔘 [BASE_ITEM_VM] Executing SelectionChangedAction for {EntityName}: {Name}");
            SelectionChangedAction.Invoke(this);
        }
        else
        {
            Debug.WriteLine($"❌ [BASE_ITEM_VM] SelectionChangedAction is NULL for {EntityName}: {Name}");
        }
    }

    /// <summary>
    /// ✅ CORRIGIDO: Observer da propriedade IsSelected
    /// </summary>
    partial void OnIsSelectedChanged(bool value)
    {
        Debug.WriteLine($"🔄 [BASE_ITEM_VM] OnIsSelectedChanged: {EntityName} {Name} -> {value}");

        if (SelectionChangedAction != null)
        {
            Debug.WriteLine($"🔄 [BASE_ITEM_VM] Notifying SelectionChangedAction: {EntityName} {Name}");
            SelectionChangedAction.Invoke(this);
        }
    }

    /// <summary>
    /// Indica se item pode ser editado
    /// </summary>
    public virtual bool CanEdit => true;

    /// <summary>
    /// Indica se item pode ser deletado
    /// </summary>
    public virtual bool CanDelete => !IsSystemDefault;

    /// <summary>
    /// Cor do badge de status baseado no estado ativo
    /// </summary>
    public virtual Color StatusBadgeColor => IsActive ? Colors.Green : Colors.Red;

    /// <summary>
    /// Texto do badge de status
    /// </summary>
    public virtual string StatusBadge => IsActive ? "ACTIVE" : "INACTIVE";

    /// <summary>
    /// Preview da descrição para UI (truncada)
    /// </summary>
    public virtual string DescriptionPreview
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Description))
                return "No description available";

            return Description.Length > 100
                ? $"{Description.Substring(0, 97)}..."
                : Description;
        }
    }

    /// <summary>
    /// Data de criação formatada
    /// </summary>
    public virtual string CreatedAtFormatted => CreatedAt.ToString("MMM dd, yyyy");

    /// <summary>
    /// Indica se é um item recente (criado nos últimos 7 dias)
    /// </summary>
    public virtual bool IsRecent => DateTime.UtcNow - CreatedAt <= TimeSpan.FromDays(7);

    /// <summary>
    /// Indicador de recente para UI
    /// </summary>
    public virtual string RecentIndicator => IsRecent ? "🆕" : "";

    /// <summary>
    /// Display completo do status combinando múltiplos indicadores
    /// </summary>
    public virtual string FullStatusDisplay
    {
        get
        {
            var status = StatusDisplay;
            if (IsSystemDefault) status += " • System";
            if (IsRecent) status += " • New";
            return status;
        }
    }

    /// <summary>
    /// Método para debug de seleção
    /// </summary>
    public virtual void DebugSelection()
    {
        Debug.WriteLine($"🔍 [BASE_ITEM_VM] DEBUG SELECTION for {EntityName} {Name}:");
        Debug.WriteLine($"    IsSelected: {IsSelected}");
        Debug.WriteLine($"    SelectionChangedAction: {(SelectionChangedAction != null ? "EXISTS" : "NULL")}");
        Debug.WriteLine($"    CanEdit: {CanEdit}");
        Debug.WriteLine($"    CanDelete: {CanDelete}");
    }
}