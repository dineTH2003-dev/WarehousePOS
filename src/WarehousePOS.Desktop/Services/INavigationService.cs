namespace WarehousePOS.Desktop.Services;

/// <summary>Navigation service — controls which view is shown in the main content Frame.</summary>
public interface INavigationService
{
    void NavigateTo<TViewModel>() where TViewModel : class;
    void NavigateToScoped<TViewModel>(IServiceProvider scopedProvider) where TViewModel : class;
    void GoBack();
}
