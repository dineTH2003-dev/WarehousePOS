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
    private IServiceScope? _currentScope;

    // ViewModel → View type mapping
    private static readonly Dictionary<Type, Type> _viewMap = new();
    private static readonly HashSet<Type> _adminOnlyViewModels = [];

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
    /// Navigates to the specified ViewModel, automatically creating a fresh DI scope
    /// and disposing the previous page's scope.
    /// </summary>
    public void NavigateTo<TViewModel>() where TViewModel : class
    {
        _currentScope?.Dispose();
        _currentScope = _scopeFactory.CreateScope();
        NavigateCore(typeof(TViewModel), _currentScope.ServiceProvider);
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

        _frame.Navigate(page);
    }

    public void GoBack()
    {
        if (_frame?.CanGoBack == true)
            _frame.GoBack();
    }
}
