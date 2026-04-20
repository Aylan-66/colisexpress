using ColisExpress.Application.DTOs.Admin;
using ColisExpress.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ColisExpress.Web.Controllers;

[Route("api/admin")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "EstAdmin")]
public class AdminApiController : ControllerBase
{
    private readonly IAdminService _admin;

    public AdminApiController(IAdminService admin) => _admin = admin;

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var data = await _admin.GetDashboardAsync(ct);
        return Ok(data);
    }

    [HttpGet("transporteurs/pending")]
    public async Task<IActionResult> TransporteursPending(CancellationToken ct)
    {
        var all = await _admin.GetTransporteursAsync(ct);
        var pending = all.Where(t => t.StatutKyc == ColisExpress.Domain.Enums.StatutKyc.EnAttente).ToList();
        return Ok(pending);
    }

    [HttpPost("transporteurs/{id:guid}/approve")]
    public async Task<IActionResult> ApproveTransporteur(Guid id, CancellationToken ct)
    {
        var result = await _admin.DecideKycAsync(new KycDecisionRequest { TransporteurId = id, Approuver = true }, ct);
        if (!result.Success) return BadRequest(new { error = result.Error });
        return Ok(new { message = "KYC approuvé." });
    }

    [HttpPost("transporteurs/{id:guid}/reject")]
    public async Task<IActionResult> RejectTransporteur(Guid id, CancellationToken ct)
    {
        var result = await _admin.DecideKycAsync(new KycDecisionRequest { TransporteurId = id, Approuver = false }, ct);
        if (!result.Success) return BadRequest(new { error = result.Error });
        return Ok(new { message = "KYC rejeté." });
    }
}
