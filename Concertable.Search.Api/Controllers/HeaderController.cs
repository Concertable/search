using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.Search.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
internal class HeaderController : ControllerBase
{
    private readonly IHeaderModule headerModule;

    public HeaderController(IHeaderModule headerModule)
    {
        this.headerModule = headerModule;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] SearchParams searchParams)
        => Ok(await headerModule.SearchAsync(searchParams));

    [HttpGet("amount/{amount}")]
    public async Task<IActionResult> GetByAmount(int amount, [FromQuery] HeaderType? headerType)
    {
        if (headerType is null)
            return BadRequest("Header type is required.");

        return Ok(await headerModule.GetByAmountAsync(headerType.Value, amount));
    }
}
