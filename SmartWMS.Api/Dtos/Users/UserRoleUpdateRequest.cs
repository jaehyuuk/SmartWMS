using System.ComponentModel.DataAnnotations;

namespace SmartWMS.Api.Dtos.Users;

public class UserRoleUpdateRequest {
    [Required]
    public string Role { get; set; } = string.Empty;
}