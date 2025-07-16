using System.ComponentModel.DataAnnotations;

namespace OrchidPro.Models;

/// <summary>
/// PASSO 1: Interface base para todas as entidades do sistema
/// Esta interface define propriedades comuns que todas as entidades devem ter
/// Permite criar ViewModels genéricos sem quebrar o código existente
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
    /// Indica se é um dado padrão do sistema
    /// </summary>
    bool IsSystemDefault { get; set; }

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
/// PASSO 1: Interface genérica para repositórios
/// Define operações CRUD padrão que todos os repositórios devem implementar
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
    Task<OperationResult> RefreshAllDataAsync();
    Task RefreshCacheAsync();
    Task<bool> TestConnectionAsync();
    string GetCacheInfo();
    void InvalidateCacheExternal();
}

/// <summary>
/// PASSO 1: Estatísticas base para qualquer entidade
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