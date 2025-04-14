using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FutureTech.Controllers
{
    public class AuthController : Controller
    {
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;

        public AuthController(ILogger<AuthController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string returnUrl = "/")
        {
            return Challenge(new AuthenticationProperties { RedirectUri = returnUrl },
                GoogleDefaults.AuthenticationScheme);
        }

        [Authorize]
        public IActionResult Profile()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // Clear authentication cookies if authentication service is available
                if (HttpContext.RequestServices.GetService(typeof(IAuthenticationService)) != null)
                {
                    await HttpContext.SignOutAsync("Cookies");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error during sign out: {ex.Message}");
                // Continue with redirect even if logout fails
            }
            
            // Redirect to home page after logout
            return RedirectToAction("Index", "Home");
        }
    }
} 