using Concertable.Search.Application.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.Search.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
internal sealed class HeaderController : ControllerBase
{
    private readonly IHeaderDispatcher headerDispatcher;

    public HeaderController(IHeaderDispatcher headerDispatcher)
    {
        this.headerDispatcher = headerDispatcher;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] SearchParams searchParams)
        => Ok(await headerDispatcher.SearchAsync(searchParams));

    [HttpGet("amount/{amount}")]
    public async Task<IActionResult> GetByAmount(int amount, [FromQuery] HeaderType? headerType)
    {
        if (headerType is null)
            return BadRequest("Header type is required.");

        return Ok(await headerDispatcher.GetByAmountAsync(headerType.Value, amount));
    }
}
