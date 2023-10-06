namespace selflix.Db;

public class UserDevice
{
    UserDevice()
    {
        DeviceId = null!;
        Name = null!;
    }
    internal UserDevice(int userId, string deviceId, string name)
    {
        UserId = userId;
        DeviceId = deviceId;
        Name = name;
        Created = DateTime.UtcNow;
    }
    public int UserDeviceId { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; }
    public string DeviceId { get; set; }
    public byte[]? OtpKey { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? OS { get; set; }
    public DateTime Created { get; set; }
    public DateTime? LastLogin { get; set; }

    public User? User { get; set; }
}