namespace WbtWebJob.Core.Models;

public class HttpJobConfiguration
{
    /// <summary>
    /// HTTP请求URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// HTTP方法 (GET, POST, PUT, DELETE等)
    /// </summary>
    public string Method { get; set; } = "GET";

    /// <summary>
    /// 请求头
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// 请求体
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// 授权类型 (None, Basic, Bearer等)
    /// </summary>
    public string AuthType { get; set; } = "None";

    /// <summary>
    /// 授权凭据
    /// </summary>
    public string? AuthCredentials { get; set; }

    /// <summary>
    /// 超时时间（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
