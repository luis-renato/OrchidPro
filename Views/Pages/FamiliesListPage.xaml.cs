using OrchidPro.ViewModels.Families;

namespace OrchidPro.Views.Pages;

public partial class FamiliesListPage : ContentPage
{
    private readonly FamiliesListViewModel _viewModel;
    private List<FamilyItemViewModel> _itemsSnapshot;
    private List<FamilyItemViewModel> _allItems; // Cache de todos os items para filtro local
    private DateTime _lastDisappearTime;

    public FamiliesListPage(FamiliesListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        _allItems = new List<FamilyItemViewModel>();

        // Escutar mudanças específicas nos dados
        MessagingCenter.Subscribe<object, FamilyItemViewModel>(this, "FamilyCreated", (sender, newFamily) =>
        {
            System.Diagnostics.Debug.WriteLine($"➕ [FAMILIES_LIST_PAGE] Adding new family to list: {newFamily.Name}");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _viewModel.Items.Insert(0, newFamily);
                _allItems.Insert(0, newFamily);
                _viewModel.TotalCount++;
                if (newFamily.IsActive) _viewModel.ActiveCount++;
                _viewModel.HasData = _viewModel.Items.Any();
                UpdateItemsSnapshot();
            });
        });

        MessagingCenter.Subscribe<object, FamilyItemViewModel>(this, "FamilyUpdated", (sender, updatedFamily) =>
        {
            System.Diagnostics.Debug.WriteLine($"✏️ [FAMILIES_LIST_PAGE] Updating family in list: {updatedFamily.Name}");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                var existingFamily = _viewModel.Items.FirstOrDefault(f => f.Id == updatedFamily.Id);
                if (existingFamily != null)
                {
                    var index = _viewModel.Items.IndexOf(existingFamily);

                    if (existingFamily.IsActive != updatedFamily.IsActive)
                    {
                        if (updatedFamily.IsActive) _viewModel.ActiveCount++;
                        else _viewModel.ActiveCount--;
                    }

                    _viewModel.Items[index] = updatedFamily;

                    // Atualizar também no cache
                    var cacheIndex = _allItems.FindIndex(f => f.Id == updatedFamily.Id);
                    if (cacheIndex >= 0)
                    {
                        _allItems[cacheIndex] = updatedFamily;
                    }

                    UpdateItemsSnapshot();
                    System.Diagnostics.Debug.WriteLine($"✅ [FAMILIES_LIST_PAGE] Family updated in-place at index {index}");
                }
            });
        });

        MessagingCenter.Subscribe<object, Guid>(this, "FamilyDeleted", (sender, deletedFamilyId) =>
        {
            System.Diagnostics.Debug.WriteLine($"🗑️ [FAMILIES_LIST_PAGE] Removing family from list: {deletedFamilyId}");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                var familyToRemove = _viewModel.Items.FirstOrDefault(f => f.Id == deletedFamilyId);
                if (familyToRemove != null)
                {
                    _viewModel.Items.Remove(familyToRemove);
                    _allItems.RemoveAll(f => f.Id == deletedFamilyId);
                    _viewModel.TotalCount--;
                    if (familyToRemove.IsActive) _viewModel.ActiveCount--;
                    _viewModel.HasData = _viewModel.Items.Any();
                    UpdateItemsSnapshot();

                    System.Diagnostics.Debug.WriteLine($"✅ [FAMILIES_LIST_PAGE] Family removed from list");
                }
            });
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await PerformEntranceAnimation();

        if (!_viewModel.Items.Any())
        {
            // Lista vazia - carregar sempre
            System.Diagnostics.Debug.WriteLine($"📥 [FAMILIES_LIST_PAGE] Initial load - List is empty");
            await _viewModel.OnAppearingAsync();
            UpdateAllItemsCache();
            UpdateItemsSnapshot();
        }
        else
        {
            var timeSinceDisappear = DateTime.Now - _lastDisappearTime;

            if (timeSinceDisappear.TotalSeconds < 60)
            {
                System.Diagnostics.Debug.WriteLine($"🔍 [FAMILIES_LIST_PAGE] Checking for changes since last visit ({timeSinceDisappear.TotalSeconds:F1}s ago)");

                var hasChanges = await DetectChanges();
                if (hasChanges)
                {
                    System.Diagnostics.Debug.WriteLine($"🔄 [FAMILIES_LIST_PAGE] Changes detected - List already updated");
                    UpdateAllItemsCache();
                    UpdateItemsSnapshot();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"✅ [FAMILIES_LIST_PAGE] No changes detected - Keeping current list");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"⚡ [FAMILIES_LIST_PAGE] Old session - Keeping existing list ({_viewModel.Items.Count} items)");
            }

            await _viewModel.OnAppearingAsync();
        }
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await PerformExitAnimation();

        _lastDisappearTime = DateTime.Now;
        System.Diagnostics.Debug.WriteLine($"👋 [FAMILIES_LIST_PAGE] Disappearing at {_lastDisappearTime:HH:mm:ss}");

        // Cleanup
        MessagingCenter.Unsubscribe<object, FamilyItemViewModel>(this, "FamilyCreated");
        MessagingCenter.Unsubscribe<object, FamilyItemViewModel>(this, "FamilyUpdated");
        MessagingCenter.Unsubscribe<object, Guid>(this, "FamilyDeleted");
    }

    /// <summary>
    /// Atualiza cache com todos os items (para filtro local)
    /// </summary>
    private void UpdateAllItemsCache()
    {
        try
        {
            _allItems = _viewModel.Items.ToList();
            System.Diagnostics.Debug.WriteLine($"📦 [FAMILIES_LIST_PAGE] All items cache updated with {_allItems.Count} items");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Error updating all items cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Aplica filtros localmente (fallback se repository não funcionar)
    /// </summary>
    private void ApplyLocalFilters()
    {
        try
        {
            var searchText = _viewModel.SearchText?.Trim()?.ToLowerInvariant() ?? string.Empty;
            var statusFilter = _viewModel.StatusFilter;

            System.Diagnostics.Debug.WriteLine($"🏷️ [FAMILIES_LIST_PAGE] Applying local filters - Search: '{searchText}', Status: '{statusFilter}'");

            var filteredItems = _allItems.AsEnumerable();

            // Filtro por texto
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredItems = filteredItems.Where(item =>
                    (item.Name?.ToLowerInvariant().Contains(searchText) == true) ||
                    (item.Description?.ToLowerInvariant().Contains(searchText) == true));
            }

            // Filtro por status
            if (statusFilter != "All")
            {
                var isActiveFilter = statusFilter == "Active";
                filteredItems = filteredItems.Where(item => item.IsActive == isActiveFilter);
            }

            var resultList = filteredItems.ToList();

            // Atualizar lista exibida
            _viewModel.Items.Clear();
            foreach (var item in resultList)
            {
                _viewModel.Items.Add(item);
            }

            _viewModel.HasData = _viewModel.Items.Any();

            System.Diagnostics.Debug.WriteLine($"✅ [FAMILIES_LIST_PAGE] Local filter applied. Results: {_viewModel.Items.Count}/{_allItems.Count}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Error applying local filters: {ex.Message}");
        }
    }

    /// <summary>
    /// Detecta se houve mudanças comparando com snapshot anterior
    /// </summary>
    private async Task<bool> DetectChanges()
    {
        try
        {
            if (_itemsSnapshot == null || !_itemsSnapshot.Any())
            {
                System.Diagnostics.Debug.WriteLine($"📷 [FAMILIES_LIST_PAGE] No snapshot available - Will refresh");
                return true;
            }

            var currentItems = _itemsSnapshot;

            System.Diagnostics.Debug.WriteLine($"🔄 [FAMILIES_LIST_PAGE] Loading latest data to compare...");

            await _viewModel.LoadItemsCommand.ExecuteAsync(null);

            var latestItems = _viewModel.Items.ToList();
            bool hasChanges = false;

            if (currentItems.Count != latestItems.Count)
            {
                System.Diagnostics.Debug.WriteLine($"📊 [FAMILIES_LIST_PAGE] Count changed: {currentItems.Count} → {latestItems.Count}");
                hasChanges = true;
            }
            else
            {
                foreach (var currentItem in currentItems)
                {
                    var latestItem = latestItems.FirstOrDefault(i => i.Id == currentItem.Id);

                    if (latestItem == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"➖ [FAMILIES_LIST_PAGE] Item removed: {currentItem.Name}");
                        hasChanges = true;
                        break;
                    }

                    if (currentItem.Name != latestItem.Name ||
                        currentItem.Description != latestItem.Description ||
                        currentItem.IsActive != latestItem.IsActive)
                    {
                        System.Diagnostics.Debug.WriteLine($"✏️ [FAMILIES_LIST_PAGE] Item changed: {latestItem.Name}");
                        hasChanges = true;
                        break;
                    }
                }

                if (!hasChanges)
                {
                    foreach (var latestItem in latestItems)
                    {
                        if (!currentItems.Any(i => i.Id == latestItem.Id))
                        {
                            System.Diagnostics.Debug.WriteLine($"➕ [FAMILIES_LIST_PAGE] New item found: {latestItem.Name}");
                            hasChanges = true;
                            break;
                        }
                    }
                }
            }

            if (hasChanges)
            {
                System.Diagnostics.Debug.WriteLine($"✅ [FAMILIES_LIST_PAGE] Changes detected and list updated in single operation");
                return true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"✅ [FAMILIES_LIST_PAGE] No changes detected - Data is current");
                return false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Error detecting changes: {ex.Message}");
            return true;
        }
    }

    /// <summary>
    /// Atualiza o snapshot da lista atual
    /// </summary>
    private void UpdateItemsSnapshot()
    {
        try
        {
            _itemsSnapshot = _viewModel.Items.ToList();
            System.Diagnostics.Debug.WriteLine($"📷 [FAMILIES_LIST_PAGE] Snapshot updated with {_itemsSnapshot.Count} items");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Error updating snapshot: {ex.Message}");
        }
    }

    /// <summary>
    /// Animação de entrada da página com fade in suave
    /// </summary>
    private async Task PerformEntranceAnimation()
    {
        try
        {
            // Estados iniciais para animação
            RootGrid.Opacity = 0;
            RootGrid.Scale = 0.95;
            RootGrid.TranslationY = 30;

            FabButton.Opacity = 0;
            FabButton.Scale = 0.8;
            FabButton.TranslationY = 50;

            // Animação principal do conteúdo
            await Task.WhenAll(
                RootGrid.FadeTo(1, 600, Easing.CubicOut),
                RootGrid.ScaleTo(1, 600, Easing.SpringOut),
                RootGrid.TranslateTo(0, 0, 600, Easing.CubicOut)
            );

            // Animação do FAB com delay
            await Task.Delay(200);
            await Task.WhenAll(
                FabButton.FadeTo(1, 400, Easing.CubicOut),
                FabButton.ScaleTo(1, 400, Easing.SpringOut),
                FabButton.TranslateTo(0, 0, 400, Easing.CubicOut)
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Entrance animation error: {ex.Message}");
            // Garantir que elementos ficam visíveis mesmo com erro
            RootGrid.Opacity = 1;
            FabButton.Opacity = 1;
        }
    }

    /// <summary>
    /// Animação de saída da página
    /// </summary>
    private async Task PerformExitAnimation()
    {
        try
        {
            await Task.WhenAll(
                RootGrid.FadeTo(0.8, 300, Easing.CubicIn),
                RootGrid.ScaleTo(0.98, 300, Easing.CubicIn),
                FabButton.FadeTo(0, 200, Easing.CubicIn),
                FabButton.ScaleTo(0.9, 200, Easing.CubicIn)
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Exit animation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handler do filtro de status - COM FILTRO LOCAL
    /// </summary>
    private async void OnFilterTapped(object sender, EventArgs e)
    {
        try
        {
            string[] options = { "All", "Active", "Inactive" };
            string result = await DisplayActionSheet("Filter by Status", "Cancel", null, options);

            if (result != "Cancel" && result != null)
            {
                System.Diagnostics.Debug.WriteLine($"🏷️ [FAMILIES_LIST_PAGE] Status filter changed to: {result}");

                _viewModel.StatusFilter = result;

                // Tentar filtro do ViewModel primeiro
                try
                {
                    await _viewModel.FilterByStatusCommand.ExecuteAsync(null);
                    UpdateItemsSnapshot();
                    System.Diagnostics.Debug.WriteLine($"✅ [FAMILIES_LIST_PAGE] ViewModel filter applied successfully");
                }
                catch (Exception vmEx)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ [FAMILIES_LIST_PAGE] ViewModel filter failed: {vmEx.Message}");
                    System.Diagnostics.Debug.WriteLine($"🔄 [FAMILIES_LIST_PAGE] Falling back to local filter...");

                    // Fallback: Aplicar filtro local
                    ApplyLocalFilters();
                    UpdateItemsSnapshot();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Filter error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handler para mudanças no texto de busca - COM FILTRO LOCAL
    /// </summary>
    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            var searchText = e.NewTextValue ?? string.Empty;
            System.Diagnostics.Debug.WriteLine($"🔍 [FAMILIES_LIST_PAGE] Search text changed: '{searchText}'");
            System.Diagnostics.Debug.WriteLine($"🔍 [FAMILIES_LIST_PAGE] ViewModel SearchText before: '{_viewModel.SearchText}'");

            // Garantir que o ViewModel tenha o texto atualizado
            _viewModel.SearchText = searchText;
            System.Diagnostics.Debug.WriteLine($"🔍 [FAMILIES_LIST_PAGE] ViewModel SearchText after: '{_viewModel.SearchText}'");

            // Tentar comando do ViewModel primeiro
            try
            {
                if (_viewModel.SearchCommand?.CanExecute(null) == true)
                {
                    System.Diagnostics.Debug.WriteLine($"🔍 [FAMILIES_LIST_PAGE] Executing ViewModel SearchCommand...");
                    await _viewModel.SearchCommand.ExecuteAsync(null);
                    UpdateItemsSnapshot();
                    System.Diagnostics.Debug.WriteLine($"✅ [FAMILIES_LIST_PAGE] ViewModel search completed. Items count: {_viewModel.Items.Count}");
                }
                else
                {
                    throw new InvalidOperationException("SearchCommand cannot execute or is null");
                }
            }
            catch (Exception vmEx)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ [FAMILIES_LIST_PAGE] ViewModel search failed: {vmEx.Message}");
                System.Diagnostics.Debug.WriteLine($"🔄 [FAMILIES_LIST_PAGE] Falling back to local search...");

                // Fallback: Aplicar filtro local
                ApplyLocalFilters();
                UpdateItemsSnapshot();
                System.Diagnostics.Debug.WriteLine($"✅ [FAMILIES_LIST_PAGE] Local search completed. Items count: {_viewModel.Items.Count}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Search error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Handler do FAB - Controla todos os modos (Add/Cancel/Delete)
    /// </summary>
    private async void OnFabPressed(object sender, EventArgs e)
    {
        try
        {
            // Animação de feedback do FAB
            await FabButton.ScaleTo(0.9, 100, Easing.CubicIn);
            await FabButton.ScaleTo(1, 100, Easing.CubicOut);

            // Debug para entender o estado atual
            System.Diagnostics.Debug.WriteLine($"🎯 [FAB_PRESSED] MultiSelect: {_viewModel.IsMultiSelectMode}, Selected: {_viewModel.SelectedItems.Count}");

            // Lógica de controle do FAB
            if (_viewModel.IsMultiSelectMode)
            {
                if (_viewModel.SelectedItems.Count > 0)
                {
                    // Modo Delete - deletar selecionados
                    System.Diagnostics.Debug.WriteLine($"🎯 [FAB_PRESSED] Executing Delete Selected ({_viewModel.SelectedItems.Count} items)");
                    _viewModel.DeleteSelectedCommand?.Execute(null);
                }
                else
                {
                    // Modo Cancel - FORÇAR saída do modo de seleção
                    System.Diagnostics.Debug.WriteLine($"🎯 [FAB_PRESSED] FORCING exit from multi-select mode");

                    // Sair do modo seleção diretamente (não toggle)
                    if (_viewModel.IsMultiSelectMode)
                    {
                        _viewModel.IsMultiSelectMode = false;
                        _viewModel.DeselectAllCommand?.Execute(null);
                        System.Diagnostics.Debug.WriteLine($"✅ [FAB_PRESSED] Successfully exited multi-select mode");
                    }
                }
            }
            else
            {
                // Modo Add - adicionar novo (estado normal)
                System.Diagnostics.Debug.WriteLine($"🎯 [FAB_PRESSED] Executing Add New Family");
                _viewModel.AddItemCommand?.Execute(null);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] FAB error: {ex.Message}");
        }
    }
}