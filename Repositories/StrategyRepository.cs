using InovaGAB.API.Models;
using InovaGAB.API.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace InovaGAB.API.Repositories;

public interface IStrategyRepository
{
    Task<List<Strategy>> GetAllAsync();
    Task<List<Strategy>> GetActiveAsync();
    Task<Strategy?> GetByIdAsync(string id);
    Task CreateAsync(Strategy strategy);
    Task UpdateAsync(string id, Strategy strategy);
    Task DeleteAsync(string id);
}

public class StrategyRepository : IStrategyRepository
{
    private readonly IMongoCollection<Strategy> _strategies;

    public StrategyRepository(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _strategies = database.GetCollection<Strategy>("strategies");
    }

    public async Task<List<Strategy>> GetAllAsync() =>
        await _strategies.Find(_ => true).SortByDescending(s => s.DataCriacao).ToListAsync();

    public async Task<List<Strategy>> GetActiveAsync() =>
        await _strategies.Find(s => s.Status == "ativo").SortByDescending(s => s.DataCriacao).ToListAsync();

    public async Task<Strategy?> GetByIdAsync(string id) =>
        await _strategies.Find(s => s.Id == id).FirstOrDefaultAsync();

    public async Task CreateAsync(Strategy strategy) =>
        await _strategies.InsertOneAsync(strategy);

    public async Task UpdateAsync(string id, Strategy strategy) =>
        await _strategies.ReplaceOneAsync(s => s.Id == id, strategy);

    public async Task DeleteAsync(string id) =>
        await _strategies.DeleteOneAsync(s => s.Id == id);
}
