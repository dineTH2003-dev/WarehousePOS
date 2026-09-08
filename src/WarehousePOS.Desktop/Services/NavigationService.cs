using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace WarehousePOS.Desktop.Services;

/// <summary>
/// Frame-based navigation service.
/// Maps ViewModel types to View types and navigates the main Frame.
/// Manages page-level DI scopes to prevent EF Core concurrency/DbContext lifetime issues.
/// </summary>
public sealed class NavigationService : INavigationService
{
    private Frame? _frame;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SessionContext _session;
    // ViewModel → View type mapping
    private static readonly Dictionary<Type, Type> _viewMap = new();
    private static readonly HashSet<Type> _adminOnlyViewModels = [];

    // Session Page Cache: ViewModelType → (Page, Scope)
    private readonly Dictionary<Type, (Page Page, IServiceScope Scope)> _pageCache = new();

    public NavigationService(IServiceScopeFactory scopeFactory, SessionContext session)
    {
        _scopeFactory = scopeFactory;
        _session = session;
    }

    public static void Register<TViewModel, TView>()
        where TView : Page
    {
        _viewMap[typeof(TViewModel)] = typeof(TView);
    }

    public static void RegisterAdminOnly<TViewModel>() where TViewModel : class =>
        _adminOnlyViewModels.Add(typeof(TViewModel));

    public void SetFrame(Frame frame)
    {
        _frame = frame;
    }

    /// <summary>
    /// Navigates to the specified ViewModel, caching the Page and DI scope so entered
    /// data and view state are preserved when switching between tabs.
    /// </summary>
    public void NavigateTo<TViewModel>() where TViewModel : class
    {
        var vmType = typeof(TViewModel);

        if (_adminOnlyViewModels.Contains(vmType) && !_session.IsAdmin)
            return;

        if (_frame is null)
            throw new InvalidOperationException("Frame not set. Call SetFrame first.");

        if (!_viewMap.TryGetValue(vmType, out var viewType))
            throw new InvalidOperationException($"No view registered for {vmType.Name}");

        if (!_pageCache.TryGetValue(vmType, out var entry))
        {
            var scope = _scopeFactory.CreateScope();
            var page = (Page)(scope.ServiceProvider.GetService(viewType)
                       ?? Activator.CreateInstance(viewType)!);

            page.KeepAlive = true;
            entry = (page, scope);
            _pageCache[vmType] = entry;
        }

        if (ReferenceEquals(_frame.Content, entry.Page))
            return;

        _frame.Navigate(entry.Page);

        // Keep journal clean so history does not accumulate or reload
        while (_frame.CanGoBack)
        {
            _frame.RemoveBackEntry();
        }
    }

    /// <summary>
    /// Navigate using a caller-supplied scoped <see cref="IServiceProvider"/> so that
    /// Scoped services (EF Core DbContext, repositories, application services) are
    /// resolved from the specified scope.
    /// </summary>
    public void NavigateToScoped<TViewModel>(IServiceProvider scopedProvider) where TViewModel : class
        => NavigateCore(typeof(TViewModel), scopedProvider);

    private void NavigateCore(Type viewModelType, IServiceProvider provider)
    {
        if (_adminOnlyViewModels.Contains(viewModelType) && !_session.IsAdmin)
            return;

        if (_frame is null)
            throw new InvalidOperationException("Frame not set. Call SetFrame first.");

        if (!_viewMap.TryGetValue(viewModelType, out var viewType))
            throw new InvalidOperationException($"No view registered for {viewModelType.Name}");

        var page = provider.GetService(viewType) as Page
                   ?? (Page)Activator.CreateInstance(viewType)!;

        if (ReferenceEquals(_frame.Content, page))
            return;

        _frame.Navigate(page);
    }

    public void GoBack()
    {
        if (_frame?.CanGoBack == true)
            _frame.GoBack();
    }

    /// <summary>
    /// Disposes all page scopes and clears the cached views on logout.
    /// </summary>
    public void ClearCache()
    {
        foreach (var (_, scope) in _pageCache.Values)
        {
            try
            {
                scope.Dispose();
            }
            catch
            {
                // Best-effort scope disposal
            }
        }
        _pageCache.Clear();
    }
}
