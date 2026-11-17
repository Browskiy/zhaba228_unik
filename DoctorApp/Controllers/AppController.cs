using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace DoctorApp.Controllers;

public abstract class AppController : Controller
{
    protected int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    protected void Success(string message) => TempData["Success"] = message;

    protected void Error(string message) => TempData["Error"] = message;
}
