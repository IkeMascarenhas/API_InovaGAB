using InovaGAB.API.Models;
using InovaGAB.API.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace InovaGAB.API.Repositories;

public interface IIdeaRepository
{
    Task<List<Idea>> GetAllAsync();
    Task<List<Idea>> GetByAutorAsync(string autorId);
    Task<List<Idea>> GetByStrategyAsync(string strategyId);
    Task<List<Idea>> GetByStatusAsync(string status);
    Task<Idea?> GetByIdAsync(string id);
    Task CreateAsync(Idea idea);
    Task UpdateAsync(string id, Idea idea);
    Task DeleteAsync(string id);

    // Dashboard aggregations
    Task<long> CountByStatusAsync(string status);
    Task<long> CountAllAsync();
}

public class IdeaRepository : IIdeaRepository
{
    private readonly IMongoCollection<Idea> _ideas;

    public IdeaRepository(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _ideas = database.GetCollection<Idea>("ideas");
    }

    public async Task<List<Idea>> GetAllAsync() =>
        await _ideas.Find(_ => true).SortByDescending(i => i.DataCriacao).ToListAsync();

    public async Task<List<Idea>> GetByAutorAsync(string autorId) =>
        await _ideas.Find(i => i.AutorId == autorId).SortByDescending(i => i.DataCriacao).ToListAsync();

    public async Task<List<Idea>> GetByStrategyAsync(string strategyId) =>
        await _ideas.Find(i => i.StrategyId == strategyId).SortByDescending(i => i.DataCriacao).ToListAsync();

    public async Task<List<Idea>> GetByStatusAsync(string status) =>
        await _ideas.Find(i => i.Status == status).SortByDescending(i => i.DataCriacao).ToListAsync();

    public async Task<Idea?> GetByIdAsync(string id) =>
        await _ideas.Find(i => i.Id == id).FirstOrDefaultAsync();

    public async Task CreateAsync(Idea idea) =>
        await _ideas.InsertOneAsync(idea);

    public async Task UpdateAsync(string id, Idea idea) =>
        await _ideas.ReplaceOneAsync(i => i.Id == id, idea);

    public async Task DeleteAsync(string id) =>
        await _ideas.DeleteOneAsync(i => i.Id == id);

    public async Task<long> CountByStatusAsync(string status) =>
        await _ideas.CountDocumentsAsync(i => i.Status == status);

    public async Task<long> CountAllAsync() =>
        await _ideas.CountDocumentsAsync(_ => true);
}
