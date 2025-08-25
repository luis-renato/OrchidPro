using OrchidPro.Models;
using OrchidPro.ViewModels.Base;
using OrchidPro.Services.Contracts;
using OrchidPro.Services.Navigation;
using OrchidPro.Extensions;
using CommunityToolkit.Mvvm.Input;

namespace OrchidPro.ViewModels.Sources;

public partial class SourcesListViewModel : BaseListViewModel<Source, SourceItemViewModel>
{
    private readonly ISourceRepository _sourceRepository;

    public override string EntityName => "Source";
    public override string EntityNamePlural => "Sources";
    public override string EditRoute => "sourceedit";

    public SourcesListViewModel(ISourceRepository repository, INavigationService navigationService)
        : base(repository, navigationService)
    {
        _sourceRepository = repository;
        this.LogInfo("🚀 ULTRA CLEAN SourcesListViewModel - base does everything!");
    }

    protected override SourceItemViewModel CreateItemViewModel(Source entity)
    {
        return new SourceItemViewModel(entity);
    }

    public IAsyncRelayCommand<SourceItemViewModel> DeleteSingleCommand => DeleteSingleItemCommand;
    public new IAsyncRelayCommand DeleteSelectedCommand => base.DeleteSelectedCommand;

    // REMOVED: LoadBySupplierTypeAsync - não usar LoadFilteredAsync que não existe
    // Se necessário, implementar usando métodos que existem na BaseListViewModel
}