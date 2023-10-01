using System.ComponentModel.DataAnnotations;
using selflix.Db;

namespace selflix.Models;

public class UserVM
{
    public UserVM(User u)
    {
        Id = u.UserId;
        Name = u.Name;
        IsAdmin = u.IsAdmin;
        Disabled = u.Disabled;
    }
    [Required] public int Id { get; set; }
    [Required] public string Name { get; set; }
    [Required] public bool IsAdmin { get; set; }
    public DateTime? Disabled { get; set; }
}

public class UserLM
{
    [Required] public int Id { get; set; }
    [Required] public required string Name { get; set; }
    [Required] public bool IsAdmin { get; set; }
    public DateTime? Disabled { get; set; }
}

public class UserCM
{
    [Required] public required string Name { get; set; }
    [Required] public bool IsAdmin { get; set; }
    [Required] public required string Password { get; set; }
    public bool IsInvalid(out ValidationError errorModel)
    {
        errorModel = new();

        if (string.IsNullOrWhiteSpace(Name))
            errorModel.Errors.Add(nameof(Name), "Required");

        if (string.IsNullOrWhiteSpace(Password))
            errorModel.Errors.Add(nameof(Password), "Required");

        return errorModel.Errors.Count > 0;
    }
}

public class UserUM
{
    [Required] public required string Name { get; set; }
    [Required] public bool IsAdmin { get; set; }
    public string? NewPassword { get; set; }
    public bool? Disabled { get; set; }
    public bool? ClearOtpKey { get; set; }
    public bool IsInvalid(out ValidationError errorModel)
    {
        errorModel = new();

        if (string.IsNullOrWhiteSpace(Name))
            errorModel.Errors.Add(nameof(Name), "Required");

        return errorModel.Errors.Count > 0;
    }
}