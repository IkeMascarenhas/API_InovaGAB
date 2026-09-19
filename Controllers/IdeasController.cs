using System.Security.Claims;
using InovaGAB.API.DTOs;
using InovaGAB.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InovaGAB.API.Controllers;

/// <summary>
/// Ideias de Inovação.
/// Operadores: cadastram e gerenciam suas próprias ideias.
/// Gestores: consultam todas, aprovam, rejeitam e reclassificam.
/// </summary>
[ApiController]
[Route("api/ideas")]
[Authorize]
[Produces("application/json")]
public class IdeasController : ControllerBase
{
    private readonly IIdeaService _ideaService;

    public IdeasController(IIdeaService ideaService)
    {
        _ideaService = ideaService;
    }


    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("Usuário não identificado.");

    private string GetUserNome() => User.FindFirstValue(ClaimTypes.Name) ?? "Colaborador";

    private string GetUserRole() =>
        User.FindFirstValue(ClaimTypes.Role)
        ?? User.FindFirstValue("perfil")
        ?? "operator";


    /// <summary>
    /// Cadastra uma nova ideia vinculada a uma estratégia ativa. [Operadores]
    /// A pontuação pela IA Gemini é atribuída automaticamente em background.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "operator")]
    [ProducesResponseType(typeof(IdeaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateIdeaRequest request)
    {
        try
        {
            var userId = GetUserId();
            var userName = GetUserNome();
            var idea = await _ideaService.CreateAsync(request, userId, userName);
            return CreatedAtAction(nameof(GetById), new { id = idea.Id }, idea);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lista as ideias do operador autenticado. [Operadores]
    /// </summary>
    [HttpGet("my")]
    [Authorize(Roles = "operator")]
    [ProducesResponseType(typeof(List<IdeaResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyIdeas()
    {
        var userId = GetUserId();
        var ideas = await _ideaService.GetMyIdeasAsync(userId);
        return Ok(ideas);
    }

    /// <summary>
    /// Exclui uma ideia pendente do operador. [Operadores — somente ideias próprias no status pendente]
    /// </summary>
    [HttpDelete("{id:length(24)}")]
    [Authorize(Roles = "operator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            var userId = GetUserId();
            await _ideaService.DeleteAsync(id, userId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }


    /// <summary>
    /// Lista todas as ideias da plataforma. [Gestores e Líderes]
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "manager,leader")]
    [ProducesResponseType(typeof(List<IdeaResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var ideas = await _ideaService.GetAllAsync();
        return Ok(ideas);
    }

    /// <summary>
    /// Filtra ideias por status. [Gestores e Líderes]
    /// Status: pendente | em_analise | aprovada | rejeitada | convertida_em_projeto
    /// </summary>
    [HttpGet("status/{status}")]
    [Authorize(Roles = "manager,leader")]
    [ProducesResponseType(typeof(List<IdeaResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByStatus(string status)
    {
        var ideas = await _ideaService.GetByStatusAsync(status);
        return Ok(ideas);
    }

    /// <summary>
    /// Obtém detalhes de uma ideia pelo ID.
    /// Operadores: somente suas próprias. Gestores: qualquer.
    /// </summary>
    [HttpGet("{id:length(24)}")]
    [Authorize(Roles = "operator,manager,leader")]
    [ProducesResponseType(typeof(IdeaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            var userId = GetUserId();
            var role = GetUserRole();
            var idea = await _ideaService.GetByIdAsync(id, userId, role);
            return Ok(idea);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Atualiza o status de uma ideia (aprovar/rejeitar/priorizar). [Gestores]
    /// Status válidos: em_analise | aprovada | rejeitada | convertida_em_projeto
    /// </summary>
    [HttpPatch("{id:length(24)}/status")]
    [Authorize(Roles = "manager")]
    [ProducesResponseType(typeof(IdeaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateIdeaStatusRequest request)
    {
        try
        {
            var idea = await _ideaService.UpdateStatusAsync(id, request);
            return Ok(idea);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
