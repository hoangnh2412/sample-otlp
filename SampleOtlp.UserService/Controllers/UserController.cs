using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SampleOtlp.Monitoring.Storage;

namespace SampleOtlp.UserService.Controllers;

[Route("users")]
[ApiController]
public class UserController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> PaginationAsync(
        [FromServices] UserDbContext dbContext
    )
    {
        try
        {
            var users = await dbContext.Users.Take(20).ToListAsync();
            return Ok(users);
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromBody] User input,
        [FromServices] UserDbContext dbContext)
    {
        using (var httpClientHandler = new HttpClientHandler())
        {
            httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
            using (var cognitoClient = new HttpClient(httpClientHandler))
            {
                await cognitoClient.PostAsync(new Uri("https://localhost:7220/cognito/user"), new StringContent(JsonConvert.SerializeObject(input), System.Text.Encoding.UTF8, "application/json"));
            }
        }

        await dbContext.Users.AddAsync(new Monitoring.Storage.User
        {
            Id = Guid.NewGuid(),
            Name = input.Name,
            Age = input.Age,
            CreatedAt = DateTime.Now
        });
        await dbContext.SaveChangesAsync();
        return Ok();
    }
}