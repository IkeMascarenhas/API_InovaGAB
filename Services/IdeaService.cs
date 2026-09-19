using InovaGAB.API.DTOs;
using InovaGAB.API.Models;
using InovaGAB.API.Repositories;

namespace InovaGAB.API.Services;

public interface IIdeaService
{
    Task<IdeaResponse> CreateAsync(CreateIdeaRequest request, string autorId, string autorNome);
    Task<List<IdeaResponse>> GetMyIdeasAsync(string autorId);
    Task<IdeaResponse> GetByIdAsync(string id, string requesterId, string requesterRole);
    Task DeleteAsync(string id, string autorId);
    Task<List<IdeaResponse>> GetAllAsync();
    Task<List<IdeaResponse>> GetByStatusAsync(string status);
    Task<IdeaResponse> UpdateStatusAsync(string id, UpdateIdeaStatusRequest request);
}

public class IdeaService : IIdeaService
{
    private readonly IIdeaRepository _ideaRepository;
    private readonly IStrategyRepository _strategyRepository;
    private readonly IGeminiService _geminiService;

    public IdeaService(
        IIdeaRepository ideaRepository,
        IStrategyRepository strategyRepository,
        IGeminiService geminiService)
    {
        _ideaRepository = ideaRepository;
        _strategyRepository = strategyRepository;
        _geminiService = geminiService;
    }

    public async Task<IdeaResponse> CreateAsync(
        CreateIdeaRequest request, string autorId, string autorNome)
    {
        var strategy = await _strategyRepository.GetByIdAsync(request.StrategyId)
            ?? throw new KeyNotFoundException($"Estratégia '{request.StrategyId}' não encontrada.");

        if (strategy.Status != "ativo")
            throw new InvalidOperationException("A estratégia selecionada não está ativa.");

        var idea = new Idea
        {
            Titulo = request.Titulo,
            Descricao = request.Descricao,
            StrategyId = request.StrategyId,
            Status = "pendente",
            AutorId = autorId,
            AutorNome = autorNome,
            DataCriacao = DateTime.UtcNow
        };

        await _ideaRepository.CreateAsync(idea);

        _ = Task.Run(async () =>
        {
            try
            {
                var score = await _geminiService.ScoreIdeaAsync(idea, strategy);
                idea.PontuacaoIA = score.Pontuacao;
                idea.JustificativaIA = score.Justificativa;
                await _ideaRepository.UpdateAsync(idea.Id, idea);
            }
            catch
            {
            }
        });

        return ToResponse(idea);
    }

    public async Task<List<IdeaResponse>> GetMyIdeasAsync(string autorId)
    {
        var ideas = await _ideaRepository.GetByAutorAsync(autorId);
        return ideas.Select(ToResponse).ToList();
    }

    public async Task<IdeaResponse> GetByIdAsync(
        string id, string requesterId, string requesterRole)
    {
        var idea = await _ideaRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Ideia '{id}' não encontrada.");

        if (requesterRole == "operator" && idea.AutorId != requesterId)
            throw new UnauthorizedAccessException("Acesso negado.");

        return ToResponse(idea);
    }

    public async Task DeleteAsync(string id, string autorId)
    {
        var idea = await _ideaRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Ideia '{id}' não encontrada.");

        if (idea.AutorId != autorId)
            throw new UnauthorizedAccessException("Você só pode excluir suas próprias ideias.");

        if (idea.Status != "pendente")
            throw new InvalidOperationException("Apenas ideias no status 'pendente' podem ser excluídas.");

        await _ideaRepository.DeleteAsync(id);
    }

    public async Task<List<IdeaResponse>> GetAllAsync()
    {
        var ideas = await _ideaRepository.GetAllAsync();
        return ideas.Select(ToResponse).ToList();
    }

    public async Task<List<IdeaResponse>> GetByStatusAsync(string status)
    {
        var ideas = await _ideaRepository.GetByStatusAsync(status);
        return ideas.Select(ToResponse).ToList();
    }

    public async Task<IdeaResponse> UpdateStatusAsync(string id, UpdateIdeaStatusRequest request)
    {
        var validStatuses = new[] { "em_analise", "aprovada", "rejeitada", "convertida_em_projeto" };
        if (!validStatuses.Contains(request.Status))
            throw new ArgumentException($"Status inválido. Use: {string.Join(", ", validStatuses)}");

        var idea = await _ideaRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Ideia '{id}' não encontrada.");

        idea.Status = request.Status;

        if (request.ComentarioGestor is not null)
            idea.ComentarioGestor = request.ComentarioGestor;

        if (request.StrategyId is not null)
        {
            var strategy = await _strategyRepository.GetByIdAsync(request.StrategyId)
                ?? throw new KeyNotFoundException($"Estratégia '{request.StrategyId}' não encontrada.");
            idea.StrategyId = strategy.Id;
        }

        await _ideaRepository.UpdateAsync(id, idea);
        return ToResponse(idea);
    }

    private static IdeaResponse ToResponse(Idea i) =>
        new(i.Id, i.Titulo, i.Descricao, i.Status, i.AutorId, i.AutorNome,
            i.StrategyId, i.ComentarioGestor, i.PontuacaoIA, i.JustificativaIA, i.DataCriacao);
}
