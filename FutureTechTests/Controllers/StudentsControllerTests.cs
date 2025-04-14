using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using FutureTech.Controllers;
using FutureTech.Services;
using FutureTech.Models;

namespace FutureTechTests.Controllers
{
    public class StudentsControllerTests
    {
        private readonly Mock<ICosmosDbService> _cosmosDbMock;
        private readonly Mock<IBlobStorageService> _blobStorageMock;
        private readonly Mock<ILogger<StudentsController>> _loggerMock;
        private readonly StudentsController _controller;

        public StudentsControllerTests()
        {
            _cosmosDbMock = new Mock<ICosmosDbService>();
            _blobStorageMock = new Mock<IBlobStorageService>();
            _loggerMock = new Mock<ILogger<StudentsController>>();

            _controller = new StudentsController(
                _cosmosDbMock.Object,
                _blobStorageMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task Index_ReturnsViewWithStudents()
        {
            // Arrange
            var students = new List<Student>
            {
                new Student { Id = "1", FirstName = "John", LastName = "Doe" },
                new Student { Id = "2", FirstName = "Jane", LastName = "Smith" }
            };
            _cosmosDbMock.Setup(s => s.GetStudentsAsync(It.IsAny<string>())).ReturnsAsync(students);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Student>>(viewResult.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task Details_WithValidId_ReturnsViewWithStudent()
        {
            // Arrange
            var student = new Student { Id = "1", FirstName = "John", ProfileImageUrl = null };
            _cosmosDbMock.Setup(s => s.GetStudentAsync("1")).ReturnsAsync(student);

            // Act
            var result = await _controller.Details("1");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<Student>(viewResult.Model);
            Assert.Equal("John", model.FirstName);
        }

        [Fact]
        public void Create_Get_ReturnsView()
        {
            var result = _controller.Create();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Search_WithResults_ReturnsViewWithStudents()
        {
            // Arrange
            var students = new List<Student>
            {
                new Student { Id = "1", FirstName = "John", LastName = "Doe" }
            };
            _cosmosDbMock.Setup(s => s.GetStudentsAsync(It.IsAny<string>())).ReturnsAsync(students);

            // Act
            var result = await _controller.Search("John");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Student>>(viewResult.Model);
            Assert.Single(model);
            Assert.Equal("John", model[0].FirstName);
        }
    }
}
