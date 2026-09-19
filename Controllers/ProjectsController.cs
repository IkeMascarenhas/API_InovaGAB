using System.Security.Claims;
using InovaGAB.API.DTOs;
using InovaGAB.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InovaGAB.API.Controllers;

/// <summary>
/// Projetos de Inovação.
/// Gestores: CRUD completo, atualizam dados de acompanhamento.
/// Líderes: apenas leitura (andamento, status, investimento, prazo, retorno).
/// </summary>
[ApiController]
[Route("api/projects")]
[Authorize]
[Produces("application/json")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException();


    /// <summary>
    /// Retorna o resumo executivo da plataforma. [Somente Líderes]
    /// Métricas: ROI, lucro obtido, investimento total, progresso médio,
    /// contagem de ideias por status, projetos por status.
    /// </summary>
    [HttpGet("dashboard")]
    [Authorize(Roles = "leader")]
    [ProducesResponseType(typeof(DashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDashboard()
    {
        var dashboard = await _projectService.GetDashboardAsync();
        return Ok(dashboard);
    }


    /// <summary>
    /// Lista todos os projetos da plataforma. [Gestores e Líderes]
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "manager,leader")]
    [ProducesResponseType(typeof(List<ProjectResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var projects = await _projectService.GetAllAsync();
        return Ok(projects);
    }

    /// <summary>
    /// Lista projetos do gestor autenticado. [Gestores]
    /// </summary>
    [HttpGet("my")]
    [Authorize(Roles = "manager")]
    [ProducesResponseType(typeof(List<ProjectResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyProjects()
    {
        var gestorId = GetUserId();
        var projects = await _projectService.GetByGestorAsync(gestorId);
        return Ok(projects);
    }

    /// <summary>
    /// Filtra projetos por status. [Gestores e Líderes]
    /// Status: planejamento | em_andamento | concluido | cancelado
    /// </summary>
    [HttpGet("status/{status}")]
    [Authorize(Roles = "manager,leader")]
    [ProducesResponseType(typeof(List<ProjectResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByStatus(string status)
    {
        try
        {
            var projects = await _projectService.GetByStatusAsync(status);
            return Ok(projects);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Obtém um projeto pelo ID. [Gestores e Líderes]
    /// </summary>
    [HttpGet("{id:length(24)}")]
    [Authorize(Roles = "manager,leader")]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            var project = await _projectService.GetByIdAsync(id);
            return Ok(project);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }


    /// <summary>
    /// Cria um novo projeto a partir de uma ideia aprovada. [Somente Gestores]
    /// A ideia referenciada deve estar no status 'aprovada'.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "manager")]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest request)
    {
        try
        {
            var gestorId = GetUserId();
            var project = await _projectService.CreateAsync(request, gestorId);
            return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
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
    /// Atualiza dados de acompanhamento do projeto (progresso, investimento, status). [Somente Gestores responsáveis]
    /// </summary>
    [HttpPut("{id:length(24)}")]
    [Authorize(Roles = "manager")]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateProjectRequest request)
    {
        try
        {
            var gestorId = GetUserId();
            var project = await _projectService.UpdateAsync(id, request, gestorId);
            return Ok(project);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Remove um projeto. [Somente Gestores responsáveis]
    /// </summary>
    [HttpDelete("{id:length(24)}")]
    [Authorize(Roles = "manager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            var gestorId = GetUserId();
            await _projectService.DeleteAsync(id, gestorId);
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
    }
}
