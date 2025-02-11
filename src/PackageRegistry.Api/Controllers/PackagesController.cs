using Backbone.Comms.Infra.Abstractions.Brokers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Octokit;

namespace PackageRegistry.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PackagesController(
    IMediatorBroker mediator
) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async ValueTask<IActionResult> GetAllRepositories(
        [FromServices] GitHubClient gitHubClient,
        CancellationToken ct)
    {
        var repositories = await gitHubClient.Repository.GetAllForOrg("WoW-2-0-Backbone");
        
        return Ok(repositories);
    }
}