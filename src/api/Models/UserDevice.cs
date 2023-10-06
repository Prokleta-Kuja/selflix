using System.ComponentModel.DataAnnotations;
using selflix.Db;

namespace selflix.Models;

public class UserDeviceVM
{
    public UserDeviceVM(UserDevice ud)
    {
        UserDeviceId = ud.UserDeviceId;
        Name = ud.Name;
        Brand = ud.Brand;
        Model = ud.Model;
        OS = ud.OS;
        Created = ud.Created;
        LastLogin = ud.LastLogin;
    }

    [Required] public int UserDeviceId { get; set; }
    [Required] public string Name { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? OS { get; set; }
    [Required] public DateTime Created { get; set; }
    public DateTime? LastLogin { get; set; }
}

public class UserDeviceLM
{
    [Required] public int UserDeviceId { get; set; }
    [Required] public required string Name { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? OS { get; set; }
    [Required] public DateTime Created { get; set; }
    public DateTime? LastLogin { get; set; }
}

public class UserDeviceCM
{
    [Required] public required string Name { get; set; }
    [Required] public required string DeviceId { get; set; }
    public bool IsInvalid(out ValidationError errorModel)
    {
        errorModel = new();

        if (string.IsNullOrWhiteSpace(Name))
            errorModel.Errors.Add(nameof(Name), "Required");

        if (string.IsNullOrWhiteSpace(DeviceId))
            errorModel.Errors.Add(nameof(DeviceId), "Required");

        return errorModel.Errors.Count > 0;
    }
}

public class UserDeviceUM
{
    [Required] public required string Name { get; set; }
    public bool? ClearOtpKey { get; set; }
    public bool IsInvalid(out ValidationError errorModel)
    {
        errorModel = new();

        if (string.IsNullOrWhiteSpace(Name))
            errorModel.Errors.Add(nameof(Name), "Required");

        return errorModel.Errors.Count > 0;
    }
}