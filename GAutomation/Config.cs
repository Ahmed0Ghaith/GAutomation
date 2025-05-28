public class Config
{
    public int NoOfInstances { get; set; } = 1;
    public int LoopCount { get; set; } = 1;
    public string Server { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string TargetUrl { get; set; } = "";
    public string CssSelector { get; set; } = "";
    public bool HeadlessMode { get; set; } = false;
    public int MinDelayBetweenInstances { get; set; } = 2000;
    public int MaxDelayBetweenInstances { get; set; } = 5000;
    public int MinScrollDelay { get; set; } = 500;
    public int MaxScrollDelay { get; set; } = 2000;
    public int MinPageStayTime { get; set; } = 5000;
    public int MaxPageStayTime { get; set; } = 15000;
}
