using System.Text;
using Infrastructure.Caching;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace SampleOtlp.Cognito.Controllers;

[Route("cognito")]
[ApiController]
public class CognitoController : ControllerBase
{
    private readonly ILogger<CognitoController> _logger;

    public CognitoController(ILogger<CognitoController> logger)
    {
        _logger = logger;
    }

    [HttpGet("user/{id}")]
    public async Task<IActionResult> GetAsync(
        [FromRoute] Guid id,
        [FromServices] ICacheService cacheService)
    {
        try
        {
            await cacheService.GetAsync("cognito");
            return Ok(new
            {
                Id = Guid.Parse("00337ebb-9bee-478f-aa90-8917af561765"),
                FirstName = "Hoang",
                LastName = "Nguyen"
            });
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("user")]
    public async Task<IActionResult> PostAsync(
        [FromBody] UserModel input,
        [FromServices] ICacheService cacheService)
    {
        input.Id = Guid.NewGuid();
        input.CreatedAt = DateTime.Now;
        await cacheService.SetAsync($"cognito:{input.Id}", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(input)), null);
        return Ok(input);
    }
}