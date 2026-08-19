using Concertable.Search.Application.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;

namespace Concertable.Search.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicies.Search)]
internal sealed class HeaderController : ControllerBase
{
    private readonly IHeaderDispatcher headerDispatcher;

    public HeaderController(IHeaderDispatcher headerDispatcher)
    {
        this.headerDispatcher = headerDispatcher;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery, BindRequired] HeaderType headerType, [FromQuery] SearchParams searchParams)
        => Ok(await headerDispatcher.SearchAsync(headerType, searchParams));

    [HttpGet("amount/{amount}")]
    public async Task<IActionResult> GetByAmount(int amount, [FromQuery, BindRequired] HeaderType headerType)
        => Ok(await headerDispatcher.GetByAmountAsync(headerType, amount));
}
