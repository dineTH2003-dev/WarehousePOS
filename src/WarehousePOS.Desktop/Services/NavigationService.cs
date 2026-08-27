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

    public void NavigateTo<TViewModel>() where TViewModel : class
    {
        if (_frame is null)
            throw new InvalidOperationException("Frame not set. Call SetFrame first.");

        if (!_viewMap.TryGetValue(typeof(TViewModel), out var viewType))
            throw new InvalidOperationException($"No view registered for {typeof(TViewModel).Name}");

        var page = _serviceProvider.GetService(viewType) as Page
                   ?? (Page)Activator.CreateInstance(viewType)!;

        _frame.Navigate(page);
    }

    public void GoBack()
    {
        if (_frame?.CanGoBack == true)
            _frame.GoBack();
    }
}
