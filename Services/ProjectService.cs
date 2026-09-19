using InovaGAB.API.DTOs;
using InovaGAB.API.Models;
using InovaGAB.API.Repositories;

namespace InovaGAB.API.Services;

public interface IProjectService
{
    Task<ProjectResponse> CreateAsync(CreateProjectRequest request, string gestorId);
    Task<ProjectResponse> UpdateAsync(string id, UpdateProjectRequest request, string gestorId);
    Task DeleteAsync(string id, string gestorId);
    Task<List<ProjectResponse>> GetAllAsync();
    Task<List<ProjectResponse>> GetByGestorAsync(string gestorId);
    Task<ProjectResponse> GetByIdAsync(string id);
    Task<List<ProjectResponse>> GetByStatusAsync(string status);
    Task<DashboardResponse> GetDashboardAsync();
}

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IIdeaRepository _ideaRepository;
    private readonly IStrategyRepository _strategyRepository;

    public ProjectService(
        IProjectRepository projectRepository,
        IIdeaRepository ideaRepository,
        IStrategyRepository strategyRepository)
    {
        _projectRepository = projectRepository;
        _ideaRepository = ideaRepository;
        _strategyRepository = strategyRepository;
    }

    public async Task<ProjectResponse> CreateAsync(CreateProjectRequest request, string gestorId)
    {
        var idea = await _ideaRepository.GetByIdAsync(request.IdeiaId)
            ?? throw new KeyNotFoundException($"Ideia '{request.IdeiaId}' não encontrada.");

        if (idea.Status != "aprovada")
            throw new InvalidOperationException("Somente ideias aprovadas podem originar projetos.");

        var strategy = await _strategyRepository.GetByIdAsync(request.StrategyId)
            ?? throw new KeyNotFoundException($"Estratégia '{request.StrategyId}' não encontrada.");

        if (strategy.Status != "ativo")
            throw new InvalidOperationException("A estratégia selecionada não está ativa.");

        var existingProject = await _projectRepository.GetByIdeiaIdAsync(request.IdeiaId);
        if (existingProject is not null)
            throw new InvalidOperationException("Já existe um projeto associado a esta ideia.");

        var project = new Project
        {
            Nome = request.Nome,
            Descricao = request.Descricao,
            IdeiaId = request.IdeiaId,
            StrategyId = request.StrategyId,
            GestorId = gestorId,
            Investimento = request.Investimento,
            RetornoEsperado = request.RetornoEsperado,
            PrazoMeses = request.PrazoMeses,
            Status = "planejamento",
            Progresso = 0,
            DataCriacao = DateTime.UtcNow
        };

        await _projectRepository.CreateAsync(project);

        idea.Status = "convertida_em_projeto";
        await _ideaRepository.UpdateAsync(idea.Id, idea);

        return ToResponse(project);
    }

    public async Task<ProjectResponse> UpdateAsync(
        string id, UpdateProjectRequest request, string gestorId)
    {
        var project = await _projectRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Projeto '{id}' não encontrado.");

        if (project.GestorId != gestorId)
            throw new UnauthorizedAccessException("Somente o gestor responsável pode editar este projeto.");

        if (request.Nome is not null) project.Nome = request.Nome;
        if (request.Descricao is not null) project.Descricao = request.Descricao;
        if (request.Status is not null)
        {
            ValidateProjectStatus(request.Status);
            project.Status = request.Status;
        }
        if (request.Investimento.HasValue) project.Investimento = request.Investimento.Value;
        if (request.RetornoEsperado.HasValue) project.RetornoEsperado = request.RetornoEsperado.Value;
        if (request.PrazoMeses.HasValue) project.PrazoMeses = request.PrazoMeses.Value;
        if (request.Progresso.HasValue)
        {
            if (request.Progresso.Value < 0 || request.Progresso.Value > 100)
                throw new ArgumentException("Progresso deve estar entre 0 e 100.");
            project.Progresso = request.Progresso.Value;
        }
        if (request.StrategyId is not null)
        {
            var strategy = await _strategyRepository.GetByIdAsync(request.StrategyId)
                ?? throw new KeyNotFoundException($"Estratégia '{request.StrategyId}' não encontrada.");
            project.StrategyId = strategy.Id;
        }

        await _projectRepository.UpdateAsync(id, project);
        return ToResponse(project);
    }

    public async Task DeleteAsync(string id, string gestorId)
    {
        var project = await _projectRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Projeto '{id}' não encontrado.");

        if (project.GestorId != gestorId)
            throw new UnauthorizedAccessException("Somente o gestor responsável pode excluir este projeto.");

        await _projectRepository.DeleteAsync(id);
    }

    public async Task<List<ProjectResponse>> GetAllAsync()
    {
        var projects = await _projectRepository.GetAllAsync();
        return projects.Select(ToResponse).ToList();
    }

    public async Task<List<ProjectResponse>> GetByGestorAsync(string gestorId)
    {
        var projects = await _projectRepository.GetByGestorAsync(gestorId);
        return projects.Select(ToResponse).ToList();
    }

    public async Task<ProjectResponse> GetByIdAsync(string id)
    {
        var project = await _projectRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Projeto '{id}' não encontrado.");
        return ToResponse(project);
    }

    public async Task<List<ProjectResponse>> GetByStatusAsync(string status)
    {
        ValidateProjectStatus(status);
        var projects = await _projectRepository.GetByStatusAsync(status);
        return projects.Select(ToResponse).ToList();
    }

    public async Task<DashboardResponse> GetDashboardAsync()
    {
        var totalIdeias     = (int)await _ideaRepository.CountAllAsync();
        var pendentes       = (int)await _ideaRepository.CountByStatusAsync("pendente");
        var aprovadas       = (int)await _ideaRepository.CountByStatusAsync("aprovada");
        var rejeitadas      = (int)await _ideaRepository.CountByStatusAsync("rejeitada");

        var totalProjetos   = (int)await _projectRepository.CountAllAsync();
        var emAndamento     = (int)await _projectRepository.CountByStatusAsync("em_andamento");
        var concluidos      = (int)await _projectRepository.CountByStatusAsync("concluido");

        var investimento    = await _projectRepository.SumInvestimentoAsync();
        var retorno         = await _projectRepository.SumRetornoEsperadoAsync();
        var lucro           = retorno - investimento;
        var roi             = investimento > 0 ? ((retorno - investimento) / investimento) * 100.0 : 0;
        var mediaPrazo      = await _projectRepository.AveragePrazoMesesAsync();
        var mediaProgresso  = await _projectRepository.AverageProgressoAsync();

        var totalStrategies = (await _strategyRepository.GetAllAsync()).Count;

        return new DashboardResponse(
            TotalIdeias: totalIdeias,
            IdeiasPendentes: pendentes,
            IdeiasAprovadas: aprovadas,
            IdeiasRejeitadas: rejeitadas,
            TotalProjetos: totalProjetos,
            ProjetosEmAndamento: emAndamento,
            ProjetosConcluidos: concluidos,
            InvestimentoTotal: Math.Round(investimento, 2),
            RetornoTotal: Math.Round(retorno, 2),
            RoiMedioPercentual: Math.Round(roi, 2),
            LucroObtido: Math.Round(lucro, 2),
            MediaPrazoMeses: (int)Math.Round(mediaPrazo),
            MediaProgresso: Math.Round(mediaProgresso, 1),
            TotalStrategies: totalStrategies
        );
    }

    private static void ValidateProjectStatus(string status)
    {
        string[] valid = ["planejamento", "em_andamento", "concluido", "cancelado"];
        if (!valid.Contains(status))
            throw new ArgumentException($"Status inválido. Use: {string.Join(", ", valid)}");
    }

    private static ProjectResponse ToResponse(Project p) =>
        new(p.Id, p.Nome, p.Descricao, p.IdeiaId, p.StrategyId, p.Status,
            p.GestorId, p.Investimento, p.RetornoEsperado, p.RoiPercentual,
            p.PrazoMeses, p.Progresso, p.DataCriacao);
}
