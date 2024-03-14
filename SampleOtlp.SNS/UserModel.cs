using System.ComponentModel.DataAnnotations;

namespace SampleOtlp.SNS;
public class UserModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Age is required")]
    public int Age { get; set; }

    public DateTime CreatedAt { get; set; }
}