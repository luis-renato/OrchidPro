using OrchidPro.Models;
using OrchidPro.Models.Base;

namespace OrchidPro.Services.Contracts;

public interface IVariantRepository : IBaseRepository<Variant>
{
    // Variant é independente como Family - sem métodos hierárquicos
    // Toda funcionalidade vem de IBaseRepository<Variant>
}