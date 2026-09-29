using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGE.Application.DTOs.Dashboard;
using SGE.Application.Interfaces.Repositories.Dashboard;
using SGE.Application.Interfaces.Services.Authentication;

namespace SGE.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public DashboardController(
        IDashboardRepository repository,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public Task<DashboardDto> Get() => _repository.GetAsync(_currentUserService.Role);
}
