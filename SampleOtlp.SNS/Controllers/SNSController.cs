using System.Text;
using Infrastructure.Caching;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace SampleOtlp.SNS.Controllers;

[Route("sns")]
[ApiController]
public class SNSController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> PostAsync(
        [FromBody] UserModel input,
        [FromServices] ICacheService cacheService)
    {
        input.Id = Guid.NewGuid();
        input.CreatedAt = DateTime.Now;
        await cacheService.SetAsync($"sns:{input.Id}", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(input)), null);
        return Ok(input);
    }
}