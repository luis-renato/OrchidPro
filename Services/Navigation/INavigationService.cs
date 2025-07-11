// Services/Navigation/INavigationService.cs
namespace OrchidPro.Services.Navigation;

public interface INavigationService
{
    Task NavigateToAsync(string route, bool animate = true);
    Task NavigateToAsync(string route, Dictionary<string, object> parameters, bool animate = true);
    Task GoBackAsync(bool animate = true);
    Task NavigateToLoginAsync();
    Task NavigateToMainAsync();
}