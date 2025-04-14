using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FutureTech.Models;
using FutureTech.Services;
using System.Threading.Tasks;

namespace FutureTech.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class StudentsController : Controller
    {
        private readonly ICosmosDbService _cosmosDbService;
        private readonly IBlobStorageService _blobStorageService;
        private readonly ILogger<StudentsController> _logger;

        public StudentsController(
            ICosmosDbService cosmosDbService,
            IBlobStorageService blobStorageService,
            ILogger<StudentsController> logger)
        {
            _cosmosDbService = cosmosDbService;
            _blobStorageService = blobStorageService;
            _logger = logger;
        }

        // GET: Students
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            try
            {
                var queryString = "SELECT * FROM c";
                var students = await _cosmosDbService.GetStudentsAsync(queryString);
                
                // Handle pagination
                var totalItems = students.Count();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                
                var paginatedStudents = students
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                
                ViewBag.CurrentPage = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalPages = totalPages;
                
                return View(paginatedStudents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving students");
                return View(new List<Student>());
            }
        }

        // GET: Students/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            var student = await _cosmosDbService.GetStudentAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            // If there's a profile image, generate a SAS URI for it
            if (!string.IsNullOrEmpty(student.ProfileImageUrl))
            {
                var blobName = Path.GetFileName(student.ProfileImageUrl);
                ViewBag.SasUri = await _blobStorageService.GetProfileImageSasUriAsync(blobName);
            }

            return View(student);
        }

        // GET: Students/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Students/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Student student, IFormFile profileImage)
        {
            try
            {
                // Log the incoming student data for debugging
                _logger.LogInformation($"Create student called with: FirstName={student.FirstName}, LastName={student.LastName}, Email={student.Email}");

                // Handle model state manually to bypass ProfileImageUrl validation if it's causing issues
                if (!ModelState.IsValid)
                {
                    // Check if the only error is for ProfileImageUrl
                    var errors = ModelState.Where(x => x.Value.Errors.Count > 0).ToList();
                    if (errors.Count == 1 && errors[0].Key == "ProfileImageUrl")
                    {
                        // Clear the error and continue
                        ModelState.Remove("ProfileImageUrl");
                    }
                    else
                    {
                        // Other validation errors exist
                        foreach (var modelState in ModelState)
                        {
                            foreach (var error in modelState.Value.Errors)
                            {
                                _logger.LogWarning($"Property: {modelState.Key}, Error: {error.ErrorMessage}");
                                // Add a custom error message that will be displayed to the user
                                if (modelState.Key != "ProfileImageUrl") // Ignore profile image errors
                                {
                                    ModelState.AddModelError("", $"Error in {modelState.Key}: {error.ErrorMessage}");
                                }
                            }
                        }
                        
                        if (!ModelState.IsValid)
                        {
                            return View(student);
                        }
                    }
                }
                    
                // Always generate a new ID to ensure uniqueness
                student.Id = Guid.NewGuid().ToString();
                _logger.LogInformation($"Generated new student ID: {student.Id}");
                
                // Upload profile image if provided
                if (profileImage != null && profileImage.Length > 0)
                {
                    try
                    {
                        student.ProfileImageUrl = await _blobStorageService.UploadProfileImageAsync(profileImage, student.Id);
                        _logger.LogInformation($"Successfully uploaded image for student, URL: {student.ProfileImageUrl}");
                    }
                    catch (Exception imgEx)
                    {
                        _logger.LogError(imgEx, $"Error uploading profile image: {imgEx.Message}");
                        // Continue without image
                        ViewBag.ImageError = $"Could not upload image: {imgEx.Message}";
                    }
                }
                else
                {
                    _logger.LogInformation("No profile image provided");
                    // Ensure ProfileImageUrl is null, not empty string
                    student.ProfileImageUrl = null;
                }

                // Try to save the student
                _logger.LogInformation($"Saving student to Cosmos DB: {student.Id}");
                await _cosmosDbService.AddStudentAsync(student);
                _logger.LogInformation($"Student saved successfully to Cosmos DB: {student.Id}");
                
                // Add a success message and redirect
                TempData["SuccessMessage"] = $"Student {student.FirstName} {student.LastName} was created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating student: {ex.Message}");
                
                if (ex.InnerException != null)
                {
                    _logger.LogError(ex.InnerException, $"Inner exception: {ex.InnerException.Message}");
                    ModelState.AddModelError("", $"Error: {ex.Message}. Inner error: {ex.InnerException.Message}");
                }
                else
                {
                    ModelState.AddModelError("", $"Error: {ex.Message}");
                }
                
                // Return to the Create view with the error and the student data
                return View(student);
            }
        }

        // GET: Students/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            var student = await _cosmosDbService.GetStudentAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            // If there's a profile image, generate a SAS URI for it
            if (!string.IsNullOrEmpty(student.ProfileImageUrl))
            {
                var blobName = Path.GetFileName(student.ProfileImageUrl);
                ViewBag.SasUri = await _blobStorageService.GetProfileImageSasUriAsync(blobName);
            }

            return View(student);
        }

        // POST: Students/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Student student, IFormFile profileImage)
        {
            _logger.LogInformation($"Edit student called for ID: {id}, Name: {student.FirstName} {student.LastName}");
            
            if (id != student.Id)
            {
                _logger.LogWarning($"ID mismatch: URL ID {id} doesn't match student ID {student.Id}");
                ModelState.AddModelError("", "ID mismatch detected.");
                return View(student);
            }

            // Verify student exists
            var existingStudent = await _cosmosDbService.GetStudentAsync(id);
            if (existingStudent == null)
            {
                _logger.LogWarning($"Student with ID {id} not found during edit operation");
                ModelState.AddModelError("", "Student not found.");
                return View(student);
            }

            // Remove model validation errors related to profileImage and ProfileImageUrl
            if (ModelState.ContainsKey("profileImage"))
            {
                ModelState.Remove("profileImage");
            }
            
            if (ModelState.ContainsKey("ProfileImageUrl"))
            {
                ModelState.Remove("ProfileImageUrl");
            }

            // Manual validation for empty required fields
            if (string.IsNullOrWhiteSpace(student.FirstName))
            {
                ModelState.AddModelError("FirstName", "First Name is required.");
            }
            
            if (string.IsNullOrWhiteSpace(student.LastName))
            {
                ModelState.AddModelError("LastName", "Last Name is required.");
            }
            
            if (string.IsNullOrWhiteSpace(student.Email))
            {
                ModelState.AddModelError("Email", "Email Address is required.");
            }
            
            if (string.IsNullOrWhiteSpace(student.MobileNumber))
            {
                ModelState.AddModelError("MobileNumber", "Mobile Number is required.");
            }

            // Handle model validation
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model validation failed during student edit");
                foreach (var modelState in ModelState.Where(x => x.Value.Errors.Count > 0))
                {
                    foreach (var error in modelState.Value.Errors)
                    {
                        _logger.LogWarning($"Validation error - Property: {modelState.Key}, Error: {error.ErrorMessage}");
                    }
                }
                
                // Re-generate SAS URI for image display
                if (!string.IsNullOrEmpty(student.ProfileImageUrl))
                {
                    var blobName = Path.GetFileName(student.ProfileImageUrl);
                    ViewBag.SasUri = await _blobStorageService.GetProfileImageSasUriAsync(blobName);
                }
                
                return View(student);
            }

            try
            {
                // Store current values for comparison and preserve data that isn't in the form
                bool statusChanged = existingStudent.EnrolmentStatus != student.EnrolmentStatus;
                
                // Handle profile image upload if provided
                if (profileImage != null && profileImage.Length > 0)
                {
                    _logger.LogInformation($"New profile image detected for student {id}, size: {profileImage.Length} bytes");
                    try
                    {
                        // If there's an existing image, delete it first
                        if (!string.IsNullOrEmpty(student.ProfileImageUrl))
                        {
                            _logger.LogInformation($"Deleting existing profile image: {student.ProfileImageUrl}");
                            var existingBlobName = Path.GetFileName(student.ProfileImageUrl);
                            await _blobStorageService.DeleteProfileImageAsync(existingBlobName);
                        }

                        // Upload the new image
                        _logger.LogInformation("Uploading new profile image");
                        student.ProfileImageUrl = await _blobStorageService.UploadProfileImageAsync(profileImage, student.Id);
                        _logger.LogInformation($"New profile image URL: {student.ProfileImageUrl}");
                    }
                    catch (Exception imgEx)
                    {
                        _logger.LogError(imgEx, "Error processing profile image");
                        ModelState.AddModelError("", $"Error processing profile image: {imgEx.Message}");
                        
                        // Re-generate SAS URI for existing image display
                        if (!string.IsNullOrEmpty(existingStudent.ProfileImageUrl))
                        {
                            var blobName = Path.GetFileName(existingStudent.ProfileImageUrl);
                            ViewBag.SasUri = await _blobStorageService.GetProfileImageSasUriAsync(blobName);
                        }
                        
                        return View(student);
                    }
                }
                else
                {
                    _logger.LogInformation("No new profile image provided, keeping existing image URL");
                    // No new image uploaded, preserve the existing ProfileImageUrl from hidden field
                }

                // Update the student in Cosmos DB
                _logger.LogInformation($"Updating student {id} in Cosmos DB");
                await _cosmosDbService.UpdateStudentAsync(id, student);
                _logger.LogInformation($"Student {id} successfully updated");
                
                // Set appropriate success message
                if (statusChanged)
                {
                    TempData["StatusMessage"] = $"Student {student.FullName} status updated to {student.EnrolmentStatus}.";
                    _logger.LogInformation($"Student status changed to {student.EnrolmentStatus}");
                }
                else
                {
                    TempData["SuccessMessage"] = $"Student {student.FullName} was updated successfully.";
                }
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating student {id}: {ex.Message}");
                
                if (ex.InnerException != null)
                {
                    _logger.LogError(ex.InnerException, $"Inner exception: {ex.InnerException.Message}");
                }
                
                ModelState.AddModelError("", $"Unable to update student: {ex.Message}");
                
                // Re-generate SAS URI for image display after error
                if (!string.IsNullOrEmpty(student.ProfileImageUrl))
                {
                    var blobName = Path.GetFileName(student.ProfileImageUrl);
                    ViewBag.SasUri = await _blobStorageService.GetProfileImageSasUriAsync(blobName);
                }
                
                return View(student);
            }
        }

        // GET: Students/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            var student = await _cosmosDbService.GetStudentAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            // If there's a profile image, generate a SAS URI for it
            if (!string.IsNullOrEmpty(student.ProfileImageUrl))
            {
                var blobName = Path.GetFileName(student.ProfileImageUrl);
                ViewBag.SasUri = await _blobStorageService.GetProfileImageSasUriAsync(blobName);
            }

            return View(student);
        }

        // POST: Students/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            var student = await _cosmosDbService.GetStudentAsync(id);
            if (student != null)
            {
                // Delete profile image if exists
                if (!string.IsNullOrEmpty(student.ProfileImageUrl))
                {
                    var blobName = Path.GetFileName(student.ProfileImageUrl);
                    await _blobStorageService.DeleteProfileImageAsync(blobName);
                }

                await _cosmosDbService.DeleteStudentAsync(id);
                
                // Add notification message
                TempData["DeleteMessage"] = $"Student {student.FullName} was permanently deleted from the system.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Students/SoftDelete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDelete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            var student = await _cosmosDbService.GetStudentAsync(id);
            if (student != null)
            {
                student.EnrolmentStatus = "Inactive";
                await _cosmosDbService.UpdateStudentAsync(id, student);
                
                // Add notification message for soft delete
                TempData["StatusMessage"] = $"Student {student.FullName} has been marked as inactive.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Students/Search
        public IActionResult Search()
        {
            return View();
        }

        // POST: Students/Search
        [HttpPost]
        public async Task<IActionResult> Search(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                return View(new List<Student>());
            }

            searchTerm = searchTerm.ToLower();
            var queryString = $"SELECT * FROM c WHERE CONTAINS(LOWER(c.firstName), '{searchTerm}') OR CONTAINS(LOWER(c.lastName), '{searchTerm}') OR CONTAINS(LOWER(c.id), '{searchTerm}')";
            
            var students = await _cosmosDbService.GetStudentsAsync(queryString);
            return View(students);
        }
    }
} 