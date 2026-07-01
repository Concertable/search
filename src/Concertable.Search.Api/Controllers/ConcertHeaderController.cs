using Concertable.Search.Application.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.Search.Api.Controllers;

[ApiController]
[Route("api/concert/headers")]
internal sealed class ConcertHeaderController : ControllerBase
{
    private readonly IConcertHeaderService concertheaderDispatcher;

    public ConcertHeaderController(IConcertHeaderService concertheaderDispatcher)
    {
        this.concertheaderDispatcher = concertheaderDispatcher;
    }

    [AllowAnonymous]
    [HttpGet("popular")]
    public async Task<IActionResult> GetPopular()
        => Ok(await concertheaderDispatcher.GetPopularAsync());

    [AllowAnonymous]
    [HttpGet("free")]
    public async Task<IActionResult> GetFree()
        => Ok(await concertheaderDispatcher.GetFreeAsync());

    [HttpGet("recommended")]
    [Authorize]
    public async Task<IActionResult> GetRecommended([FromQuery] ConcertParams concertParams)
        => Ok(await concertheaderDispatcher.GetRecommendedAsync(concertParams));
}
