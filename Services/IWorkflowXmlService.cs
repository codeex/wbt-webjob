using WbtWebJob.Models;

namespace WbtWebJob.Services;

/// <summary>
/// 工作流XML序列化/反序列化服务接口
/// </summary>
public interface IWorkflowXmlService
{
    /// <summary>
    /// 将工作流对象序列化为XML字符串
    /// </summary>
    /// <param name="workflow">工作流对象</param>
    /// <returns>XML字符串</returns>
    Task<string> SerializeToXmlAsync(Workflow workflow);

    /// <summary>
    /// 从XML字符串反序列化为工作流对象
    /// </summary>
    /// <param name="xmlContent">XML字符串</param>
    /// <returns>工作流对象</returns>
    Task<Workflow> DeserializeFromXmlAsync(string xmlContent);

    /// <summary>
    /// 将工作流保存为XML文件
    /// </summary>
    /// <param name="workflow">工作流对象</param>
    /// <param name="filePath">文件路径</param>
    /// <returns>保存的文件路径</returns>
    Task<string> SaveToFileAsync(Workflow workflow, string? filePath = null);

    /// <summary>
    /// 从XML文件加载工作流
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>工作流对象</returns>
    Task<Workflow> LoadFromFileAsync(string filePath);

    /// <summary>
    /// 验证XML内容是否有效
    /// </summary>
    /// <param name="xmlContent">XML字符串</param>
    /// <returns>验证结果</returns>
    Task<(bool IsValid, string? ErrorMessage)> ValidateXmlAsync(string xmlContent);
}
