namespace selflix.Db;

public class StreamToken
{
    public int StreamTokenId { get; set; }
    public int UserId { get; set; }
    public int UserDeviceId { get; set; }
    public int VideoId { get; set; }
    public DateTime Start { get; set; }
    public DateTime? End { get; set; }

    public User? User { get; set; }
    public UserDevice? UserDevice { get; set; }
    public Video? Video { get; set; }
}