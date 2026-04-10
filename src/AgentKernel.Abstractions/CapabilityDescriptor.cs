namespace AgentKernel.Abstractions;

/// <summary>
/// 表示一个 Capability 的结构化描述信息。
/// 用于让系统不仅能执行某个能力，还能理解这个能力的用途、类别和输入输出契约。
/// </summary>
public class CapabilityDescriptor
{
    /// <summary>
    /// 能力唯一名称。
    /// 应与 Capability 的 Name 对齐。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 给人看的能力显示名称。
    /// 例如：扫描文件、读取元数据、视觉复核。
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 能力说明。
    /// 用于描述该能力的职责和适用范围。
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 能力类别。
    /// 例如：input、metadata、filter、vision、ocr、export、shell。
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// 所属业务域。
    /// 例如：photo、ocr、shell、cad。
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// 当前能力依赖的输入项名称集合。
    /// 例如：source_items、filtered_items。
    /// </summary>
    public List<string> Consumes { get; set; } = [];

    /// <summary>
    /// 当前能力产出的输出项名称集合。
    /// 例如：metadata_items、verified_items。
    /// </summary>
    public List<string> Produces { get; set; } = [];

    /// <summary>
    /// 当前能力是否依赖模型。
    /// 例如视觉识别、OCR、文本分类通常为 true。
    /// </summary>
    public bool RequiresModel { get; set; }

    /// <summary>
    /// 当前能力的结果是否适合进入人工复核。
    /// </summary>
    public bool SupportsReview { get; set; }
}
