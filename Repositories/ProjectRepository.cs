using InovaGAB.API.Models;
using InovaGAB.API.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace InovaGAB.API.Repositories;

public interface IProjectRepository
{
    Task<List<Project>> GetAllAsync();
    Task<List<Project>> GetByGestorAsync(string gestorId);
    Task<List<Project>> GetByStrategyAsync(string strategyId);
    Task<List<Project>> GetByStatusAsync(string status);
    Task<Project?> GetByIdAsync(string id);
    Task<Project?> GetByIdeiaIdAsync(string ideiaId);
    Task CreateAsync(Project project);
    Task UpdateAsync(string id, Project project);
    Task DeleteAsync(string id);

    // Dashboard aggregations
    Task<long> CountAllAsync();
    Task<long> CountByStatusAsync(string status);
    Task<double> SumInvestimentoAsync();
    Task<double> SumRetornoEsperadoAsync();
    Task<double> AveragePrazoMesesAsync();
    Task<double> AverageProgressoAsync();
}

public class ProjectRepository : IProjectRepository
{
    private readonly IMongoCollection<Project> _projects;

    public ProjectRepository(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _projects = database.GetCollection<Project>("projects");
    }

    public async Task<List<Project>> GetAllAsync() =>
        await _projects.Find(_ => true).SortByDescending(p => p.DataCriacao).ToListAsync();

    public async Task<List<Project>> GetByGestorAsync(string gestorId) =>
        await _projects.Find(p => p.GestorId == gestorId).SortByDescending(p => p.DataCriacao).ToListAsync();

    public async Task<List<Project>> GetByStrategyAsync(string strategyId) =>
        await _projects.Find(p => p.StrategyId == strategyId).SortByDescending(p => p.DataCriacao).ToListAsync();

    public async Task<List<Project>> GetByStatusAsync(string status) =>
        await _projects.Find(p => p.Status == status).SortByDescending(p => p.DataCriacao).ToListAsync();

    public async Task<Project?> GetByIdAsync(string id) =>
        await _projects.Find(p => p.Id == id).FirstOrDefaultAsync();

    public async Task<Project?> GetByIdeiaIdAsync(string ideiaId) =>
        await _projects.Find(p => p.IdeiaId == ideiaId).FirstOrDefaultAsync();

    public async Task CreateAsync(Project project) =>
        await _projects.InsertOneAsync(project);

    public async Task UpdateAsync(string id, Project project) =>
        await _projects.ReplaceOneAsync(p => p.Id == id, project);

    public async Task DeleteAsync(string id) =>
        await _projects.DeleteOneAsync(p => p.Id == id);

    public async Task<long> CountAllAsync() =>
        await _projects.CountDocumentsAsync(_ => true);

    public async Task<long> CountByStatusAsync(string status) =>
        await _projects.CountDocumentsAsync(p => p.Status == status);

    public async Task<double> SumInvestimentoAsync()
    {
        var result = await _projects.Find(_ => true).ToListAsync();
        return result.Sum(p => p.Investimento);
    }

    public async Task<double> SumRetornoEsperadoAsync()
    {
        var result = await _projects.Find(_ => true).ToListAsync();
        return result.Sum(p => p.RetornoEsperado);
    }

    public async Task<double> AveragePrazoMesesAsync()
    {
        var result = await _projects.Find(_ => true).ToListAsync();
        return result.Count > 0 ? result.Average(p => p.PrazoMeses) : 0;
    }

    public async Task<double> AverageProgressoAsync()
    {
        var result = await _projects.Find(_ => true).ToListAsync();
        return result.Count > 0 ? result.Average(p => p.Progresso) : 0;
    }
}
