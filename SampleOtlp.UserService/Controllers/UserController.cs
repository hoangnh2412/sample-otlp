using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SampleOtlp.Monitoring.Queue;
using SampleOtlp.Monitoring.Storage;

namespace SampleOtlp.UserService.Controllers;

[Route("users")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly ILogger<UserController> _logger;

    public UserController(ILogger<UserController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> PaginationAsync(
        [FromServices] UserDbContext dbContext)
    {
        try
        {
            var users = await dbContext.Users.OrderByDescending(x => x.CreatedAt).Take(20).ToListAsync();
            return Ok(users);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromQuery] int hasError,
        [FromBody] User input,
        [FromServices] UserDbContext dbContext,
        [FromServices] MessageSender messageSender)
    {
        try
        {
            using (var httpClientHandler = new HttpClientHandler())
            {
                httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
                using (var cognitoClient = new HttpClient(httpClientHandler))
                {
                    var response = await cognitoClient.PostAsync(new Uri("https://localhost:7220/cognito/user"), new StringContent(JsonConvert.SerializeObject(input), System.Text.Encoding.UTF8, "application/json"));
                    var content = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                        throw new Exception($"{response.StatusCode} - {content}");
                }
            }

            var user = new Monitoring.Storage.User
            {
                Id = Guid.NewGuid(),
                Name = input.Name,
                Age = input.Age,
                CreatedAt = DateTime.Now
            };
            await dbContext.Users.AddAsync(user);
            await dbContext.SaveChangesAsync();

            messageSender.SendMessage(JsonConvert.SerializeObject(user));
            return Ok();
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return StatusCode(500, ex.Message);
        }
    }
}