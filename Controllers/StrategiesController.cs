using InovaGAB.API.DTOs;
using InovaGAB.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InovaGAB.API.Controllers;

/// <summary>
/// Estratégias Corporativas.
/// CRUD completo: apenas Líderes.
/// Leitura: Gestores e Operadores.
/// </summary>
[ApiController]
[Route("api/strategies")]
[Authorize]
[Produces("application/json")]
public class StrategiesController : ControllerBase
{
    private readonly IStrategyService _strategyService;

    public StrategiesController(IStrategyService strategyService)
    {
        _strategyService = strategyService;
    }

    /// <summary>
    /// Lista todas as estratégias (todos os perfis autenticados).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "leader,manager,operator")]
    [ProducesResponseType(typeof(List<StrategyResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var strategies = await _strategyService.GetAllAsync();
        return Ok(strategies);
    }

    /// <summary>
    /// Lista apenas estratégias ativas (usadas ao criar ideias/projetos).
    /// </summary>
    [HttpGet("active")]
    [Authorize(Roles = "leader,manager,operator")]
    [ProducesResponseType(typeof(List<StrategyResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive()
    {
        var strategies = await _strategyService.GetActiveAsync();
        return Ok(strategies);
    }

    /// <summary>
    /// Obtém uma estratégia pelo ID (MongoDB ObjectId — 24 hex chars).
    /// </summary>
    [HttpGet("{id:length(24)}")]
    [Authorize(Roles = "leader,manager,operator")]
    [ProducesResponseType(typeof(StrategyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            var strategy = await _strategyService.GetByIdAsync(id);
            return Ok(strategy);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cria uma nova estratégia corporativa. [Somente Líderes]
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "leader")]
    [ProducesResponseType(typeof(StrategyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateStrategyRequest request)
    {
        try
        {
            var strategy = await _strategyService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = strategy.Id }, strategy);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza uma estratégia existente. [Somente Líderes]
    /// </summary>
    [HttpPut("{id:length(24)}")]
    [Authorize(Roles = "leader")]
    [ProducesResponseType(typeof(StrategyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateStrategyRequest request)
    {
        try
        {
            var strategy = await _strategyService.UpdateAsync(id, request);
            return Ok(strategy);
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

    /// <summary>
    /// Remove uma estratégia. [Somente Líderes]
    /// </summary>
    [HttpDelete("{id:length(24)}")]
    [Authorize(Roles = "leader")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            await _strategyService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
