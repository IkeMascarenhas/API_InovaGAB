using InovaGAB.API.DTOs;
using InovaGAB.API.Models;
using InovaGAB.API.Repositories;

namespace InovaGAB.API.Services;

public interface IStrategyService
{
    Task<List<StrategyResponse>> GetAllAsync();
    Task<List<StrategyResponse>> GetActiveAsync();
    Task<StrategyResponse> GetByIdAsync(string id);
    Task<StrategyResponse> CreateAsync(CreateStrategyRequest request);
    Task<StrategyResponse> UpdateAsync(string id, UpdateStrategyRequest request);
    Task DeleteAsync(string id);
}

public class StrategyService : IStrategyService
{
    private readonly IStrategyRepository _repository;

    public StrategyService(IStrategyRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<StrategyResponse>> GetAllAsync()
    {
        var strategies = await _repository.GetAllAsync();
        return strategies.Select(ToResponse).ToList();
    }

    public async Task<List<StrategyResponse>> GetActiveAsync()
    {
        var strategies = await _repository.GetActiveAsync();
        return strategies.Select(ToResponse).ToList();
    }

    public async Task<StrategyResponse> GetByIdAsync(string id)
    {
        var strategy = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Estratégia '{id}' não encontrada.");
        return ToResponse(strategy);
    }

    public async Task<StrategyResponse> CreateAsync(CreateStrategyRequest request)
    {
        ValidateStatus(request.Status);

        var strategy = new Strategy
        {
            Titulo = request.Titulo,
            Descricao = request.Descricao,
            Categoria = request.Categoria,
            Campanha = request.Campanha,
            Status = request.Status,
            DataCriacao = DateTime.UtcNow
        };

        await _repository.CreateAsync(strategy);
        return ToResponse(strategy);
    }

    public async Task<StrategyResponse> UpdateAsync(string id, UpdateStrategyRequest request)
    {
        var strategy = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Estratégia '{id}' não encontrada.");

        if (request.Titulo is not null) strategy.Titulo = request.Titulo;
        if (request.Descricao is not null) strategy.Descricao = request.Descricao;
        if (request.Categoria is not null) strategy.Categoria = request.Categoria;
        if (request.Campanha is not null) strategy.Campanha = request.Campanha;
        if (request.Status is not null)
        {
            ValidateStatus(request.Status);
            strategy.Status = request.Status;
        }

        await _repository.UpdateAsync(id, strategy);
        return ToResponse(strategy);
    }

    public async Task DeleteAsync(string id)
    {
        var strategy = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Estratégia '{id}' não encontrada.");
        await _repository.DeleteAsync(id);
    }


    private static void ValidateStatus(string status)
    {
        if (status != "ativo" && status != "inativo")
            throw new ArgumentException("Status inválido. Use: ativo | inativo");
    }

    private static StrategyResponse ToResponse(Strategy s) =>
        new(s.Id, s.Titulo, s.Descricao, s.Categoria, s.Campanha, s.Status, s.DataCriacao);
}
