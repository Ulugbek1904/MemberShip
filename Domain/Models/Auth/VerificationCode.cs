using Common.Models.Base;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Auth;

[Table("verification_code", Schema = "auth"), Index(nameof(Email))]
[Index(nameof(Key))]
public class VerificationCode : ModelBase<long>
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = default;
    public DateTime ExipireDate { get; set; }
    public bool HasUsed { get; set; }
    public required string Key { get; set; }
    public string Otp { get; set; }

}
