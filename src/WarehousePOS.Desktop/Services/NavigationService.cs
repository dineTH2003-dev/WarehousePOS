using System.Windows.Controls;

namespace WarehousePOS.Desktop.Services;

/// <summary>
/// Frame-based navigation service.
/// Maps ViewModel types to View types and navigates the main Frame.
/// </summary>
public sealed class NavigationService : INavigationService
{
    private Frame? _frame;
    private readonly IServiceProvider _serviceProvider;

    // ViewModel → View type mapping
    private static readonly Dictionary<Type, Type> _viewMap = new();

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public static void Register<TViewModel, TView>()
        where TView : Page
    {
        _viewMap[typeof(TViewModel)] = typeof(TView);
    }

    public void SetFrame(Frame frame)
    {
        _frame = frame;
    }

    /// <summary>
    /// Navigate using the root service provider. Prefer <see cref="NavigateToScoped{TViewModel}"/>
    /// to avoid resolving Scoped services (e.g. DbContext) from the root container.
    /// </summary>
    public void NavigateTo<TViewModel>() where TViewModel : class
        => NavigateCore(typeof(TViewModel), _serviceProvider);

    /// <summary>
    /// Navigate using a caller-supplied scoped <see cref="IServiceProvider"/> so that
    /// Scoped services (EF Core DbContext, repositories, application services) are
    /// resolved from the correct scope and not captured from the root container.
    /// </summary>
    public void NavigateToScoped<TViewModel>(IServiceProvider scopedProvider) where TViewModel : class
        => NavigateCore(typeof(TViewModel), scopedProvider);

    private void NavigateCore(Type viewModelType, IServiceProvider provider)
    {
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
