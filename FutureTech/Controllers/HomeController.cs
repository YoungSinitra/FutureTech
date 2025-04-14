using System.Diagnostics;
using FutureTech.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Azure.Cosmos;
using FutureTech.Services;

namespace FutureTech.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly ICosmosDbService _cosmosDbService;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, ICosmosDbService cosmosDbService)
        {
            _logger = logger;
            _configuration = configuration;
            _cosmosDbService = cosmosDbService;
        }

        public IActionResult Index()
        {
            // Check if user is authenticated and is an admin
            if (User.Identity.IsAuthenticated)
            {
                var email = User.FindFirstValue(ClaimTypes.Email);
                var adminEmails = _configuration.GetSection("Authentication:AdminEmails").Get<string[]>();
                
                if (adminEmails?.Contains(email) == true)
                {
                    ViewBag.IsAdmin = true;
                }
            }
            
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [Authorize(Policy = "AdminOnly")]
        public IActionResult Dashboard()
        {
            return View();
        }

        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> TestCosmosDb()
        {
            ViewBag.Success = false;
            ViewBag.Error = "";
            ViewBag.Students = new List<Models.Student>();
            
            try
            {
                // First attempt to query all students
                var queryString = "SELECT * FROM c";
                var students = await _cosmosDbService.GetStudentsAsync(queryString);
                ViewBag.Students = students;
                
                // Try to add a test student
                var testStudent = new Models.Student
                {
                    Id = "test-" + Guid.NewGuid().ToString(),
                    FirstName = "Test",
                    LastName = "User",
                    Email = "test@example.com",
                    MobileNumber = "1234567890",
                    EnrolmentStatus = "Active"
                };
                
                await _cosmosDbService.AddStudentAsync(testStudent);
                ViewBag.Success = true;
                ViewBag.Message = "Successfully connected to Cosmos DB and saved a test student!";
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                if (ex.InnerException != null)
                {
                    ViewBag.Error += $"\nInner exception: {ex.InnerException.Message}";
                }
                _logger.LogError(ex, "Error testing Cosmos DB connection");
            }
            
            return View();
        }
    }
}
