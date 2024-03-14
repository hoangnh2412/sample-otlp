using Microsoft.AspNetCore.Mvc;

namespace SampleOtlp.Cognito.Controllers;

[Route("cognito")]
[ApiController]
public class CognitoController : ControllerBase
{
    [HttpGet("user/{id}")]
    public IActionResult Get([FromRoute] Guid id)
    {
        return Ok(new
        {
            Id = Guid.Parse("00337ebb-9bee-478f-aa90-8917af561765"),
            FirstName = "Hoang",
            LastName = "Nguyen"
        });
    }

    [HttpPost("user")]
    public IActionResult Post([FromBody] UserModel input)
    {
        return Ok();
    }
}