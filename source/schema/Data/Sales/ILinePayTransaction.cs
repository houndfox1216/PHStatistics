// ReSharper disable CheckNamespace

namespace EmptyProject; 

/// <summary>
/// LINE Pay 交易
/// </summary>
public interface ILinePayTransaction {
    /// <summary>
    /// 交易編號
    /// </summary>
    string TransactionId { get; set; }
}