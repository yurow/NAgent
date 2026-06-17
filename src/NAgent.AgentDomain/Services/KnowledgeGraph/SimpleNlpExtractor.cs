using System.Text.RegularExpressions;

namespace NAgent.AgentDomain.Services.KnowledgeGraph;

/// <summary>
/// 基于规则的简单NLP实体关系提取器
/// 无向量、无外部依赖，纯C#实现
/// </summary>
public class SimpleNlpExtractor
{
    /// <summary>
    /// 提取实体（基于词典匹配和规则）
    /// </summary>
    public List<ExtractedEntity> ExtractEntities(string text)
    {
        var entities = new List<ExtractedEntity>();
        if (string.IsNullOrWhiteSpace(text)) return entities;

        // 1. 提取技术术语（大写缩写、驼峰命名、带点的缩写）
        ExtractTechTerms(text, entities);

        // 2. 提取中文专有名词（基于规则：连续的名词、人名、地名等）
        ExtractChineseProperNouns(text, entities);

        // 3. 提取英文专有名词（首字母大写的连续单词）
        ExtractEnglishProperNouns(text, entities);

        // 4. 提取URL/文件路径
        ExtractPathsAndUrls(text, entities);

        // 5. 提取数字/版本号
        ExtractVersionsAndNumbers(text, entities);

        // 去重（按名称+类型）
        return entities
            .GroupBy(e => $"{e.Name}|{e.EntityType}")
            .Select(g =>
            {
                var first = g.First();
                first.OccurrenceCount = g.Count();
                return first;
            })
            .ToList();
    }

    /// <summary>
    /// 提取关系（基于句式模板匹配）
    /// </summary>
    public List<ExtractedRelation> ExtractRelations(string text, List<ExtractedEntity> entities)
    {
        var relations = new List<ExtractedRelation>();
        if (string.IsNullOrWhiteSpace(text) || entities.Count < 2) return relations;

        var entityNames = entities.Select(e => e.Name).ToList();
        var sentences = SplitIntoSentences(text);

        foreach (var sentence in sentences)
        {
            // 找出句子中包含的实体
            var sentenceEntities = entities
                .Where(e => sentence.Contains(e.Name))
                .ToList();

            if (sentenceEntities.Count < 2) continue;

            // 基于关键词匹配关系
            foreach (var relationPattern in RelationPatterns)
            {
                if (sentence.Contains(relationPattern.Keyword))
                {
                    // 找到关系关键词前后的实体
                    var keywordIndex = sentence.IndexOf(relationPattern.Keyword);
                    var beforeEntities = sentenceEntities
                        .Where(e => sentence.IndexOf(e.Name) < keywordIndex)
                        .ToList();
                    var afterEntities = sentenceEntities
                        .Where(e => sentence.IndexOf(e.Name) > keywordIndex)
                        .ToList();

                    // 建立前后实体之间的关系
                    foreach (var source in beforeEntities.Take(2))
                    {
                        foreach (var target in afterEntities.Take(2))
                        {
                            if (source.Name != target.Name)
                            {
                                relations.Add(new ExtractedRelation
                                {
                                    SourceName = source.Name,
                                    TargetName = target.Name,
                                    RelationType = relationPattern.RelationType,
                                    Confidence = 0.7
                                });
                            }
                        }
                    }
                }
            }

            // 共现关系：同一句子中的实体默认相关
            for (int i = 0; i < sentenceEntities.Count; i++)
            {
                for (int j = i + 1; j < sentenceEntities.Count; j++)
                {
                    var e1 = sentenceEntities[i];
                    var e2 = sentenceEntities[j];
                    if (e1.Name != e2.Name && !relations.Any(r =>
                        (r.SourceName == e1.Name && r.TargetName == e2.Name) ||
                        (r.SourceName == e2.Name && r.TargetName == e1.Name)))
                    {
                        relations.Add(new ExtractedRelation
                        {
                            SourceName = e1.Name,
                            TargetName = e2.Name,
                            RelationType = "相关",
                            Confidence = 0.3
                        });
                    }
                }
            }
        }

        return relations;
    }

    /// <summary>
    /// 将长文本分块
    /// </summary>
    public List<string> ChunkText(string text, int chunkSize = 1000, int overlap = 100)
    {
        var chunks = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return chunks;

        // 按句子分割
        var sentences = SplitIntoSentences(text);
        var currentChunk = new System.Text.StringBuilder();

        foreach (var sentence in sentences)
        {
            if (currentChunk.Length + sentence.Length > chunkSize && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString().Trim());
                // 保留重叠部分
                var overlapText = GetOverlapText(currentChunk.ToString(), overlap);
                currentChunk.Clear();
                currentChunk.Append(overlapText);
            }
            currentChunk.Append(sentence);
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        return chunks;
    }

    #region 私有方法

    private void ExtractTechTerms(string text, List<ExtractedEntity> entities)
    {
        // 匹配技术缩写：GPT-4, .NET, ASP.NET, C#, HTML5 等
        var techPatterns = new[]
        {
            @"[A-Z]{2,}(?:[-\.]?[A-Z0-9]+)*",  // GPT, GPT-4, HTML5
            @"[A-Z]\#",                          // C#, F#
            @"\.[A-Z][a-zA-Z]*",                 // .NET, .JS
            @"[a-z]+[A-Z][a-zA-Z]*",             // camelCase (如 iPhone, eBay)
        };

        foreach (var pattern in techPatterns)
        {
            var matches = Regex.Matches(text, pattern);
            foreach (Match match in matches)
            {
                if (match.Value.Length >= 2 && !IsCommonWord(match.Value))
                {
                    entities.Add(new ExtractedEntity
                    {
                        Name = match.Value,
                        EntityType = "技术"
                    });
                }
            }
        }
    }

    private void ExtractChineseProperNouns(string text, List<ExtractedEntity> entities)
    {
        // 匹配中文人名、地名、组织名等
        // 简单规则：2-8个连续中文字符，前后有标点或空格
        var matches = Regex.Matches(text, @"(?<=[，。；：！？、\s]|^)([\u4e00-\u9fa5]{2,8})(?=[，。；：！？、\s]|$)");
        foreach (Match match in matches)
        {
            var name = match.Groups[1].Value;
            if (!IsCommonChineseWord(name))
            {
                var type = GuessChineseEntityType(name);
                entities.Add(new ExtractedEntity
                {
                    Name = name,
                    EntityType = type
                });
            }
        }
    }

    private void ExtractEnglishProperNouns(string text, List<ExtractedEntity> entities)
    {
        // 匹配首字母大写的连续单词（可能是专有名词）
        var matches = Regex.Matches(text, @"\b[A-Z][a-z]+(?:\s+[A-Z][a-z]+)*\b");
        foreach (Match match in matches)
        {
            var name = match.Value.Trim();
            if (name.Length >= 2 && !IsCommonWord(name))
            {
                entities.Add(new ExtractedEntity
                {
                    Name = name,
                    EntityType = "概念"
                });
            }
        }
    }

    private void ExtractPathsAndUrls(string text, List<ExtractedEntity> entities)
    {
        // URL
        var urlMatches = Regex.Matches(text, @"https?://[^\s<>""{}|\\^`\[\]]+");
        foreach (Match match in urlMatches)
        {
            entities.Add(new ExtractedEntity
            {
                Name = match.Value,
                EntityType = "链接"
            });
        }

        // 文件路径
        var pathMatches = Regex.Matches(text, @"[a-zA-Z]:\\[^\s<>""{}|]+|(?:/[^\s<>""{}|]+)+\.[a-zA-Z0-9]+");
        foreach (Match match in pathMatches)
        {
            entities.Add(new ExtractedEntity
            {
                Name = match.Value,
                EntityType = "文件"
            });
        }
    }

    private void ExtractVersionsAndNumbers(string text, List<ExtractedEntity> entities)
    {
        // 版本号：v1.0, 1.2.3, 2.0
        var versionMatches = Regex.Matches(text, @"[vV]?\d+\.\d+(?:\.\d+)*");
        foreach (Match match in versionMatches)
        {
            entities.Add(new ExtractedEntity
            {
                Name = match.Value,
                EntityType = "版本"
            });
        }
    }

    private string GuessChineseEntityType(string name)
    {
        // 简单启发式判断
        if (name.EndsWith("公司") || name.EndsWith("集团") || name.EndsWith("科技") || name.EndsWith("网络"))
            return "组织";
        if (name.EndsWith("省") || name.EndsWith("市") || name.EndsWith("县") || name.EndsWith("区"))
            return "地点";
        if (name.EndsWith("人") || name.EndsWith("先生") || name.EndsWith("女士"))
            return "人物";
        if (name.EndsWith("系统") || name.EndsWith("平台") || name.EndsWith("框架"))
            return "技术";
        return "概念";
    }

    private bool IsCommonWord(string word)
    {
        var commonWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "A", "AN", "THE", "AND", "OR", "BUT", "IN", "ON", "AT", "TO", "FOR", "OF", "WITH", "BY",
            "IS", "ARE", "WAS", "WERE", "BE", "BEEN", "BEING", "HAVE", "HAS", "HAD", "DO", "DOES", "DID",
            "THIS", "THAT", "THESE", "THOSE", "I", "YOU", "HE", "SHE", "IT", "WE", "THEY",
            "FROM", "UP", "ABOUT", "INTO", "OVER", "AFTER", "BEFORE", "UNDER", "AGAIN",
            "FURTHER", "THEN", "ONCE", "HERE", "THERE", "WHEN", "WHERE", "WHY", "HOW",
            "ALL", "ANY", "BOTH", "EACH", "FEW", "MORE", "MOST", "OTHER", "SOME", "SUCH",
            "NO", "NOR", "NOT", "ONLY", "OWN", "SAME", "SO", "THAN", "TOO", "VERY"
        };
        return commonWords.Contains(word);
    }

    private bool IsCommonChineseWord(string word)
    {
        var commonWords = new[] { "我们", "你们", "他们", "它们", "这个", "那个", "这里", "那里", "什么", "怎么", "为什么" };
        return commonWords.Contains(word);
    }

    private List<string> SplitIntoSentences(string text)
    {
        // 按中文和英文标点分割句子
        var sentences = Regex.Split(text, @"(?<=[。！？.!?])\s*")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .ToList();
        return sentences;
    }

    private string GetOverlapText(string text, int overlapLength)
    {
        if (text.Length <= overlapLength) return text;
        // 找到重叠部分的句子边界
        var overlap = text.Substring(text.Length - overlapLength);
        var lastSentenceEnd = overlap.IndexOfAny(new[] { '。', '！', '？', '.', '!', '?' });
        if (lastSentenceEnd >= 0)
        {
            return overlap.Substring(lastSentenceEnd + 1);
        }
        return overlap;
    }

    private static readonly List<(string Keyword, string RelationType)> RelationPatterns = new()
    {
        ("使用", "使用"),
        ("基于", "基于"),
        ("包含", "包含"),
        ("属于", "属于"),
        ("创建", "创建"),
        ("开发", "开发"),
        ("支持", "支持"),
        ("需要", "需要"),
        ("调用", "调用"),
        ("引用", "引用"),
        ("依赖", "依赖"),
        ("实现", "实现"),
        ("继承", "继承"),
        ("关联", "关联"),
        ("用于", "用于"),
        ("应用于", "应用于"),
        ("是", "是"),
        ("有", "有"),
        ("具有", "具有"),
        ("提供", "提供"),
        ("生成", "生成"),
        ("由", "由"),
        ("通过", "通过"),
        ("利用", "利用"),
        ("采用", "采用"),
        ("运用", "运用"),
        ("借助", "借助"),
        ("根据", "根据"),
        ("按照", "按照"),
        ("遵循", "遵循"),
        ("符合", "符合"),
        ("满足", "满足"),
        ("达到", "达到"),
        ("实现", "实现"),
        ("完成", "完成"),
        ("解决", "解决"),
        ("处理", "处理"),
        ("管理", "管理"),
        ("控制", "控制"),
        ("配置", "配置"),
        ("设置", "设置"),
        ("定义", "定义"),
        ("声明", "声明"),
        ("指定", "指定"),
        ("确定", "确定"),
        ("决定", "决定"),
        ("选择", "选择"),
        ("选用", "选用"),
        ("应用", "应用"),
        ("部署", "部署"),
        ("安装", "安装"),
        ("运行", "运行"),
        ("执行", "执行"),
        ("启动", "启动"),
        ("停止", "停止"),
        ("暂停", "暂停"),
        ("恢复", "恢复"),
        ("重启", "重启"),
        ("关闭", "关闭"),
        ("打开", "打开"),
        ("访问", "访问"),
        ("连接", "连接"),
        ("断开", "断开"),
        ("发送", "发送"),
        ("接收", "接收"),
        ("传输", "传输"),
        ("传递", "传递"),
        ("转换", "转换"),
        ("变换", "变换"),
        ("映射", "映射"),
        ("对应", "对应"),
        ("匹配", "匹配"),
        ("比较", "比较"),
        ("对比", "对比"),
        ("分析", "分析"),
        ("解析", "解析"),
        ("分解", "分解"),
        ("组合", "组合"),
        ("整合", "整合"),
        ("合并", "合并"),
        ("拆分", "拆分"),
        ("分割", "分割"),
        ("提取", "提取"),
        ("抽取", "抽取"),
        ("过滤", "过滤"),
        ("筛选", "筛选"),
        ("排序", "排序"),
        ("排列", "排列"),
        ("组织", "组织"),
        ("存储", "存储"),
        ("保存", "保存"),
        ("读取", "读取"),
        ("写入", "写入"),
        ("删除", "删除"),
        ("修改", "修改"),
        ("更新", "更新"),
        ("替换", "替换"),
        ("插入", "插入"),
        ("添加", "添加"),
        ("移除", "移除"),
        ("清空", "清空"),
        ("重置", "重置"),
        ("初始化", "初始化"),
        ("创建", "创建"),
        ("销毁", "销毁"),
        ("释放", "释放"),
        ("分配", "分配"),
        ("回收", "回收"),
        ("占用", "占用"),
        ("占用", "占用"),
        ("占用", "占用"),
    };

    #endregion
}

/// <summary>
/// 提取的实体
/// </summary>
public class ExtractedEntity
{
    public string Name { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int OccurrenceCount { get; set; } = 1;
}

/// <summary>
/// 提取的关系
/// </summary>
public class ExtractedRelation
{
    public string SourceName { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public string RelationType { get; set; } = string.Empty;
    public double Confidence { get; set; } = 1.0;
}
