using System.Framework.Elasticsearch;

namespace PHStatistics; 

/// <summary>
/// 全文索引脈絡
/// </summary>
public class IndexContext : ElasticsearchContext {
    /// <summary>
    /// 建構 IndexContext
    /// </summary>
    public IndexContext() : base("cloudfun://uri", "index") { }
}