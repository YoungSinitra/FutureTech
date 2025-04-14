using FutureTech.Models;

namespace FutureTech.Services
{
    public interface ICosmosDbService
    {
        Task<IEnumerable<Student>> GetStudentsAsync(string queryString);
        Task<Student> GetStudentAsync(string id);
        Task AddStudentAsync(Student student);
        Task UpdateStudentAsync(string id, Student student);
        Task DeleteStudentAsync(string id);
    }
} 