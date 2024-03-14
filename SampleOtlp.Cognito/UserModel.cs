using System.ComponentModel.DataAnnotations;

namespace SampleOtlp.Cognito;
public class UserModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "FirstName is required")]
    public string FirstName { get; set; }

    [Required(ErrorMessage = "LastName is required")]
    public string LastName { get; set; }

    public DateTime CreatedAt { get; set; }
}