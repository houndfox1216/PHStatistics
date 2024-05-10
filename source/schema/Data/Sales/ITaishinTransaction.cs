// ReSharper disable CheckNamespace

namespace EmptyProject; 

/// <summary>
/// 台新銀行交易
/// </summary>
public interface ITaishinTransaction {
    /// <summary>
    /// 交易類型
    /// </summary>
    int TransactionType { get; }
}