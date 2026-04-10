using AgentKernel.Abstractions;

namespace AgentKernel.Core;

/// <summary>
/// 表示系统能力注册中心。
/// 用于统一注册、查询和管理当前可用的 Capability 及其描述信息。
/// </summary>
public class CapabilityRegistry
{
    private readonly Dictionary<string, ITaskCapability> _capabilities;
    private readonly Dictionary<string, CapabilityDescriptor> _descriptors;

    /// <summary>
    /// 初始化能力注册中心。
    /// </summary>
    public CapabilityRegistry()
    {
        _capabilities = new Dictionary<string, ITaskCapability>(StringComparer.OrdinalIgnoreCase);
        _descriptors = new Dictionary<string, CapabilityDescriptor>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 注册一个 Capability 及其描述信息。
    /// </summary>
    /// <param name="capability">能力实例。</param>
    /// <param name="descriptor">能力描述。</param>
    public void Register(ITaskCapability capability, CapabilityDescriptor descriptor)
    {
        if (capability is null)
        {
            throw new ArgumentNullException(nameof(capability));
        }

        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (string.IsNullOrWhiteSpace(capability.Name))
        {
            throw new InvalidOperationException("Capability.Name 不能为空。");
        }

        if (string.IsNullOrWhiteSpace(descriptor.Name))
        {
            throw new InvalidOperationException("CapabilityDescriptor.Name 不能为空。");
        }

        if (!string.Equals(capability.Name, descriptor.Name, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Capability.Name 与 CapabilityDescriptor.Name 必须一致。");
        }

        _capabilities[capability.Name] = capability;
        _descriptors[descriptor.Name] = descriptor;
    }

    /// <summary>
    /// 判断指定能力是否已注册。
    /// </summary>
    /// <param name="name">能力名称。</param>
    /// <returns>如果已注册则返回 true。</returns>
    public bool Contains(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return _capabilities.ContainsKey(name);
    }

    /// <summary>
    /// 根据动作定义查找可处理的 Capability。
    /// </summary>
    /// <param name="action">动作定义。</param>
    /// <returns>匹配到的能力实例；如果没有找到则返回 null。</returns>
    public ITaskCapability? Find(TaskActionDefinition action)
    {
        if (action is null)
        {
            return null;
        }

        if (!_capabilities.TryGetValue(action.Name, out ITaskCapability? capability))
        {
            return null;
        }

        return capability.CanHandle(action)
            ? capability
            : null;
    }

    /// <summary>
    /// 根据能力名称获取 Capability。
    /// </summary>
    /// <param name="name">能力名称。</param>
    /// <returns>匹配到的能力实例；如果没有找到则返回 null。</returns>
    public ITaskCapability? GetCapability(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return _capabilities.TryGetValue(name, out ITaskCapability? capability)
            ? capability
            : null;
    }

    /// <summary>
    /// 根据能力名称获取描述信息。
    /// </summary>
    /// <param name="name">能力名称。</param>
    /// <returns>匹配到的能力描述；如果没有找到则返回 null。</returns>
    public CapabilityDescriptor? GetDescriptor(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return _descriptors.TryGetValue(name, out CapabilityDescriptor? descriptor)
            ? descriptor
            : null;
    }

    /// <summary>
    /// 获取当前所有能力描述信息。
    /// </summary>
    /// <returns>能力描述集合。</returns>
    public IReadOnlyCollection<CapabilityDescriptor> GetAllDescriptors()
    {
        return _descriptors.Values.ToList().AsReadOnly();
    }

    /// <summary>
    /// 获取当前所有能力实例。
    /// </summary>
    /// <returns>能力实例集合。</returns>
    public IReadOnlyCollection<ITaskCapability> GetAllCapabilities()
    {
        return _capabilities.Values.ToList().AsReadOnly();
    }
}
