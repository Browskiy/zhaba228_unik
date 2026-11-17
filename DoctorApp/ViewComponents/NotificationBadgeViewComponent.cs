using System.Security.Claims;
using DoctorApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace DoctorApp.ViewComponents;

public class NotificationBadgeViewComponent : ViewComponent
{
    private readonly NotificationService _notifications;

    public NotificationBadgeViewComponent(NotificationService notifications)
    {
        _notifications = notifications;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        string? id = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        int count = id == null ? 0 : await _notifications.UnreadCountAsync(int.Parse(id));
        return Content(count > 0 ? count.ToString() : "");
    }
}
