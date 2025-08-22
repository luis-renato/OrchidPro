using OrchidPro.Models;
using OrchidPro.Models.Base;
using OrchidPro.Services.Base;
using OrchidPro.Services.Contracts;
using OrchidPro.Services.Infrastructure.Supabase.Models;
using OrchidPro.Services.Data;
using OrchidPro.Extensions;

namespace OrchidPro.Services.Infrastructure.Supabase.Repositories;

public class SupabaseVariantRepository(SupabaseService supabaseService)
    : BaseRepository<Variant>(supabaseService), IVariantRepository
{
    private readonly BaseSupabaseEntityService<Variant, SupabaseVariant> _supabaseEntityService = new InternalSupabaseVariantService(supabaseService);

    protected override string EntityTypeName => "Variant";

    protected override async Task<IEnumerable<Variant>> GetAllFromServiceAsync()
        => await _supabaseEntityService.GetAllAsync();

    protected override async Task<Variant?> GetByIdFromServiceAsync(Guid id)
        => await _supabaseEntityService.GetByIdAsync(id);

    protected override async Task<Variant?> CreateInServiceAsync(Variant entity)
        => await _supabaseEntityService.CreateAsync(entity);

    protected override async Task<Variant?> UpdateInServiceAsync(Variant entity)
        => await _supabaseEntityService.UpdateAsync(entity);

    protected override async Task<bool> DeleteInServiceAsync(Guid id)
        => await _supabaseEntityService.DeleteAsync(id);

    protected override async Task<bool> NameExistsInServiceAsync(string name, Guid? excludeId)
        => await _supabaseEntityService.NameExistsAsync(name, excludeId);
}

internal class InternalSupabaseVariantService(SupabaseService supabaseService)
    : BaseSupabaseEntityService<Variant, SupabaseVariant>(supabaseService)
{
    protected override string EntityTypeName => "Variant";
    protected override string EntityPluralName => "Variants";

    protected override Variant ConvertToEntity(SupabaseVariant supabaseModel)
        => supabaseModel.ToVariant();

    protected override SupabaseVariant ConvertFromEntity(Variant entity)
        => SupabaseVariant.FromVariant(entity);
}