namespace InovaGAB.API.DTOs;

public record RegisterRequest(
    string Nome,
    string Email,
    string Password,
    string Perfil
);

public record LoginRequest(
    string Email,
    string Password
);

public record AuthResponse(
    string Token,
    string UserId,
    string Nome,
    string Email,
    string Perfil,
    DateTime ExpiresAt
);

public record CreateStrategyRequest(
    string Titulo,
    string Descricao,
    string Categoria,
    string Campanha,
    string Status
);

public record UpdateStrategyRequest(
    string? Titulo,
    string? Descricao,
    string? Categoria,
    string? Campanha,
    string? Status
);

public record StrategyResponse(
    string Id,
    string Titulo,
    string Descricao,
    string Categoria,
    string Campanha,
    string Status,
    DateTime DataCriacao
);

public record CreateIdeaRequest(
    string Titulo,
    string Descricao,
    string StrategyId
);

public record UpdateIdeaStatusRequest(
    string Status,
    string? ComentarioGestor,
    string? StrategyId
);

public record IdeaResponse(
    string Id,
    string Titulo,
    string Descricao,
    string Status,
    string AutorId,
    string AutorNome,
    string StrategyId,
    string ComentarioGestor,
    int PontuacaoIA,
    string JustificativaIA,
    DateTime DataCriacao
);

public record CreateProjectRequest(
    string Nome,
    string Descricao,
    string IdeiaId,
    string StrategyId,
    double Investimento,
    double RetornoEsperado,
    int PrazoMeses
);

public record UpdateProjectRequest(
    string? Nome,
    string? Descricao,
    string? Status,
    double? Investimento,
    double? RetornoEsperado,
    int? PrazoMeses,
    int? Progresso,
    string? StrategyId
);

public record ProjectResponse(
    string Id,
    string Nome,
    string Descricao,
    string IdeiaId,
    string StrategyId,
    string Status,
    string GestorId,
    double Investimento,
    double RetornoEsperado,
    double RoiPercentual,
    int PrazoMeses,
    int Progresso,
    DateTime DataCriacao
);

public record DashboardResponse(
    int TotalIdeias,
    int IdeiasPendentes,
    int IdeiasAprovadas,
    int IdeiasRejeitadas,
    int TotalProjetos,
    int ProjetosEmAndamento,
    int ProjetosConcluidos,
    double InvestimentoTotal,
    double RetornoTotal,
    double RoiMedioPercentual,
    double LucroObtido,
    int MediaPrazoMeses,
    double MediaProgresso,
    int TotalStrategies
);

public record GeminiScoreResponse(
    int Pontuacao,
    string Justificativa
);
