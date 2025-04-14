using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using FutureTech.Controllers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using System.Threading.Tasks;

namespace FutureTechTests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<ILogger<AuthController>> _loggerMock;
        private readonly Mock<IConfiguration> _configMock;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _loggerMock = new Mock<ILogger<AuthController>>();
            _configMock = new Mock<IConfiguration>();
            _controller = new AuthController(_loggerMock.Object, _configMock.Object);
        }

        [Fact]
        public void Login_Get_ReturnsViewResult()
        {
            var result = _controller.Login();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Login_Post_ReturnsChallengeResult()
        {
            var result = _controller.Login("/profile");

            var challengeResult = Assert.IsType<ChallengeResult>(result);
            Assert.Equal(GoogleDefaults.AuthenticationScheme, challengeResult.AuthenticationSchemes[0]);
            Assert.Equal("/profile", challengeResult.Properties.RedirectUri);
        }

        [Fact]
        public void Profile_ReturnsViewResult()
        {
            var result = _controller.Profile();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Logout_ReturnsRedirectToAction()
        {
            var httpContextMock = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContextMock
            };

            httpContextMock.Response.Body = new System.IO.MemoryStream();

            var result = await _controller.Logout();

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }
    }
}
