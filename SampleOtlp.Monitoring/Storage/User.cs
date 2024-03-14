using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SampleOtlp.Monitoring.Storage;

[Table("UserSample")]
public class User
{
    public Guid Id { get; set; }
    
    public string Name { get; set; }
    
    public int Age { get; set; }
    
    public DateTime CreatedAt { get; set; }
}