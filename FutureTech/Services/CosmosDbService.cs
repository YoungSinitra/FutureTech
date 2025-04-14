using Microsoft.Azure.Cosmos;
using FutureTech.Models;

namespace FutureTech.Services
{
    public class CosmosDbService : ICosmosDbService
    {
        private readonly Container _container;

        public CosmosDbService(CosmosClient cosmosClient, string databaseName, string containerName)
        {
            _container = cosmosClient.GetContainer(databaseName, containerName);
        }

        public async Task<IEnumerable<Student>> GetStudentsAsync(string queryString)
        {
            var query = _container.GetItemQueryIterator<Student>(new QueryDefinition(queryString));
            var results = new List<Student>();
            
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response.ToList());
            }
            
            return results;
        }

        public async Task<Student> GetStudentAsync(string id)
        {
            try
            {
                var response = await _container.ReadItemAsync<Student>(id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task AddStudentAsync(Student student)
        {
            await _container.CreateItemAsync(student, new PartitionKey(student.Id));
        }

        public async Task UpdateStudentAsync(string id, Student student)
        {
            await _container.UpsertItemAsync(student, new PartitionKey(student.Id));
        }

        public async Task DeleteStudentAsync(string id)
        {
            await _container.DeleteItemAsync<Student>(id, new PartitionKey(id));
        }
    }
} 