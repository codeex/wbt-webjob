using System.Text;
using System.Text.RegularExpressions;

namespace WbtWebJob.Utils;

/// <summary>
/// Curl命令解析器，用于解析curl命令并提取HTTP请求信息
/// </summary>
public class CurlParser
{
    public class CurlParseResult
    {
        public string Url { get; set; } = string.Empty;
        public string Method { get; set; } = "GET";
        public Dictionary<string, string> Headers { get; set; } = new();
        public string? RequestBody { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 解析curl命令
    /// </summary>
    public static CurlParseResult Parse(string curlCommand)
    {
        var result = new CurlParseResult();

        try
        {
            if (string.IsNullOrWhiteSpace(curlCommand))
            {
                result.Success = false;
                result.ErrorMessage = "curl命令不能为空";
                return result;
            }

            // 移除多余的换行和空格，合并成一行
            var cleanedCommand = Regex.Replace(curlCommand, @"\s*\\\s*\n\s*", " ");
            cleanedCommand = Regex.Replace(cleanedCommand, @"\s+", " ").Trim();

            // 提取URL（第一个不以-开头的参数通常是URL）
            var urlMatch = Regex.Match(cleanedCommand, @"curl\s+['""]?([^'"">\s-][^\s'""]+)['""]?");
            if (urlMatch.Success)
            {
                result.Url = urlMatch.Groups[1].Value.Trim('\'', '"');
            }
            else
            {
                // 尝试查找 curl 后面的第一个 http/https URL
                urlMatch = Regex.Match(cleanedCommand, @"(https?://[^\s'""]+)");
                if (urlMatch.Success)
                {
                    result.Url = urlMatch.Groups[1].Value.Trim('\'', '"');
                }
            }

            if (string.IsNullOrEmpty(result.Url))
            {
                result.Success = false;
                result.ErrorMessage = "无法从curl命令中提取URL";
                return result;
            }

            // 提取HTTP方法
            var methodMatch = Regex.Match(cleanedCommand, @"-X\s+([A-Z]+)", RegexOptions.IgnoreCase);
            if (methodMatch.Success)
            {
                result.Method = methodMatch.Groups[1].Value.ToUpper();
            }
            else if (Regex.IsMatch(cleanedCommand, @"--data(-raw|-binary)?|--json|-d\s"))
            {
                // 如果有--data参数但没有显式指定方法，默认为POST
                result.Method = "POST";
            }

            // 提取所有Header
            var headerMatches = Regex.Matches(cleanedCommand, @"-H\s+['""]([^'""]+)['""]");
            foreach (Match match in headerMatches)
            {
                var headerValue = match.Groups[1].Value;
                var colonIndex = headerValue.IndexOf(':');
                if (colonIndex > 0)
                {
                    var headerName = headerValue.Substring(0, colonIndex).Trim();
                    var headerVal = headerValue.Substring(colonIndex + 1).Trim();
                    result.Headers[headerName] = headerVal;
                }
            }

            // 提取请求体数据
            // 支持: --data, --data-raw, --data-binary, -d, --json
            var dataPatterns = new[]
            {
                @"--data-raw\s+['""](.+?)['""](?=\s+-|$)",
                @"--data-binary\s+['""](.+?)['""](?=\s+-|$)",
                @"--data\s+['""](.+?)['""](?=\s+-|$)",
                @"-d\s+['""](.+?)['""](?=\s+-|$)",
                @"--json\s+['""](.+?)['""](?=\s+-|$)"
            };

            foreach (var pattern in dataPatterns)
            {
                var dataMatch = Regex.Match(cleanedCommand, pattern, RegexOptions.Singleline);
                if (dataMatch.Success)
                {
                    result.RequestBody = dataMatch.Groups[1].Value;
                    // 尝试格式化JSON
                    result.RequestBody = TryFormatJson(result.RequestBody);
                    break;
                }
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"解析curl命令时出错: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// 尝试格式化JSON字符串
    /// </summary>
    private static string TryFormatJson(string json)
    {
        try
        {
            var obj = System.Text.Json.JsonSerializer.Deserialize<object>(json);
            if (obj != null)
            {
                return System.Text.Json.JsonSerializer.Serialize(obj, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
        }
        catch
        {
            // 如果无法格式化，返回原始字符串
        }
        return json;
    }

    /// <summary>
    /// 将解析结果转换为Headers的JSON字符串
    /// </summary>
    public static string HeadersToJson(Dictionary<string, string> headers)
    {
        if (headers == null || headers.Count == 0)
            return string.Empty;

        return System.Text.Json.JsonSerializer.Serialize(headers, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
