using Concertable.Search.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.Search.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
internal class AutocompleteController : ControllerBase
{
    private readonly IAutocompleteServiceFactory autocompleteServiceFactory;

    public AutocompleteController(IAutocompleteServiceFactory autocompleteServiceFactory)
    {
        this.autocompleteServiceFactory = autocompleteServiceFactory;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] HeaderType? headerType, [FromQuery] string? searchTerm)
    {
        var service = autocompleteServiceFactory.Create(headerType);
        return Ok(await service.GetAsync(searchTerm));
    }
}
