using System.ComponentModel.DataAnnotations;

namespace OrchidPro.Models.Base;

/// <summary>
/// ✅ ENHANCED: Interface base com IsFavorite genérico
/// </summary>
public interface IBaseEntity
{
    /// <summary>
    /// Identificador único da entidade
    /// </summary>
    Guid Id { get; set; }

    /// <summary>
    /// ID do usuário proprietário (null para dados do sistema)
    /// </summary>
    Guid? UserId { get; set; }

    /// <summary>
    /// Nome/título principal da entidade
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Descrição opcional da entidade
    /// </summary>
    string? Description { get; set; }

    /// <summary>
    /// Indica se a entidade está ativa
    /// </summary>
    bool IsActive { get; set; }

    /// <summary>
    /// ✅ NOVO: Indica se a entidade é favorita do usuário
    /// </summary>
    bool IsFavorite { get; set; }

    /// <summary>
    /// ✅ ATUALIZADO: IsSystemDefault como computed property
    /// Baseado em UserId == null ao invés de campo no banco
    /// </summary>
    bool IsSystemDefault { get; }

    /// <summary>
    /// Data de criação
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// Data da última atualização
    /// </summary>
    DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Nome para exibição na UI
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Status para exibição na UI
    /// </summary>
    string StatusDisplay { get; }

    /// <summary>
    /// Valida a entidade e retorna lista de erros
    /// </summary>
    /// <param name="errors">Lista de erros encontrados</param>
    /// <returns>True se válido, false se há erros</returns>
    bool IsValid(out List<string> errors);

    /// <summary>
    /// Cria uma cópia da entidade para edição
    /// </summary>
    /// <returns>Clone da entidade</returns>
    IBaseEntity Clone();
}

/// <summary>
/// Interface genérica para repositórios
/// </summary>
public interface IBaseRepository<T> where T : class, IBaseEntity
{
    // Operações de leitura
    Task<List<T>> GetAllAsync(bool includeInactive = false);
    Task<List<T>> GetFilteredAsync(string? searchText = null, bool? statusFilter = null);
    Task<T?> GetByIdAsync(Guid id);
    Task<T?> GetByNameAsync(string name);

    // Operações de escrita
    Task<T> CreateAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task<bool> DeleteAsync(Guid id);
    Task<int> DeleteMultipleAsync(IEnumerable<Guid> ids);

    // Validações e utilitários
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null);
    Task<BaseStatistics> GetStatisticsAsync();
    Task RefreshCacheAsync();
    Task<bool> TestConnectionAsync();
    string GetCacheInfo();
    void InvalidateCacheExternal();
}

/// <summary>
/// Estatísticas base para qualquer entidade
/// </summary>
public class BaseStatistics
{
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
    public int SystemDefaultCount { get; set; }
    public int UserCreatedCount { get; set; }
    public DateTime LastRefreshTime { get; set; }
}