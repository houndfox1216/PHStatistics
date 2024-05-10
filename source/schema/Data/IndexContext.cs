using System.Framework.Elasticsearch;

<<<<<<< HEAD
namespace EmptyProject; 
=======
namespace PHStatistics; 
>>>>>>> origin/develop/schema

/// <summary>
/// 全文索引脈絡
/// </summary>
public class IndexContext : ElasticsearchContext {
    /// <summary>
    /// 建構 IndexContext
    /// </summary>
    public IndexContext() : base("cloudfun://uri", "index") { }
}