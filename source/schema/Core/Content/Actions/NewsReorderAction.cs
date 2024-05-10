using System;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Linq;
using System.Transactions;

// ReSharper disable once CheckNamespace
namespace EmptyProject.Actions;

/// <summary>
/// 排序屬性資料之操作。
/// </summary>
[Description("排序屬性資料")]
public class NewsReorderAction : ActionBase<DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => [SystemPermission.News];

    /// <summary>
    /// 建構 CategoryReorderAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    /// <param name="context">資料脈絡</param>
    public NewsReorderAction(IUser user, DataContext context = null) : base("排序屬性資料", user, context) { }

    protected override void OnExecuting(DataContext context) {
        var dataContext = context ?? new DataContext();
        var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.RepeatableRead });
        try {
            var sourceRow = dataContext.News.Find(Parameters.GetValue<int>("source")) ?? throw new OperationException("未提供 source 參數");
            var targetRow = dataContext.News.Find(Parameters.GetValue<int>("target")) ?? throw new OperationException("未提供 source 參數");
            var isAfter = Parameters.GetValue<bool>("isAfter");

            int? maxOrdinal = sourceRow.Ordinal.HasValue || targetRow.Ordinal.HasValue ? Math.Max(sourceRow.Ordinal ?? 0, targetRow.Ordinal ?? 0) : null;
            int? minOrdinal = sourceRow.Ordinal.HasValue && targetRow.Ordinal.HasValue ? Math.Min(sourceRow.Ordinal.Value, targetRow.Ordinal.Value) : null;
            sourceRow.Ordinal = targetRow.Ordinal;

            var query = dataContext.News.Where(e => e.Id != sourceRow.Id);
            if (maxOrdinal.HasValue) query = query.Where(e => e.Ordinal <= maxOrdinal);
            if (minOrdinal.HasValue) query = query.Where(e => e.Ordinal >= minOrdinal);

            var rows = query.OrderBy(e => e.Ordinal).ToList();

            int? currentOrdinal = null;
            for (var i = 0; i < rows.Count; i++) {
                var currentRow = rows[i];
                var nextRow = i < rows.Count - 1 ? rows[i + 1] : null;
                if (currentOrdinal.HasValue) {
                    if (nextRow != null && nextRow.Ordinal - currentOrdinal > 2) {
                        currentOrdinal += (nextRow.Ordinal - currentOrdinal) / 2;
                        break;
                    }

                    try {
                        currentRow.Ordinal = (int)(currentOrdinal += 100);
                    } catch {
                        var difference = int.MaxValue - currentOrdinal ?? 0;
                        if (difference > 0) currentRow.Ordinal = currentOrdinal++;
                        else currentOrdinal = currentRow.Ordinal = int.MaxValue;
                    }
                } else {
                    if (currentRow.Id != targetRow.Id) continue;
                    var afterRow = isAfter ? sourceRow : targetRow;
                    if (nextRow != null && nextRow.Ordinal - targetRow.Ordinal >= 2) {
                        currentOrdinal = afterRow.Ordinal += (nextRow.Ordinal - targetRow.Ordinal) / 2;
                        break;
                    }

                    try {
                        currentOrdinal = afterRow.Ordinal += 100;
                    } catch {
                        var difference = int.MaxValue - afterRow.Ordinal ?? 0;
                        if (difference > 0) currentOrdinal = afterRow.Ordinal++;
                        else currentOrdinal = afterRow.Ordinal = int.MaxValue;
                    }
                }
            }
            dataContext.SaveChanges();
            scope.Complete();
        } catch (FrameworkException ce) {
            throw new FrameworkException(ce.Message, ce.InnerException, MessageType.Info);
        } catch (System.Data.DataException due) {
            throw new DataException("資料異動中，請稍後再試", due, MessageType.Warning);
        } catch (Exception e) {
            throw new FrameworkException("目前無法進行相關操作，請聯繫維護人員!", e, MessageType.Error);
        } finally {
            #region 清除所使用的資源

            scope?.Dispose();
            if (context == null) dataContext.Dispose();

            #endregion
        }
    }

    public void Execute(int source, int target, bool isAfter) {
            Parameters.Add("source", source);
            Parameters.Add("target", target);
            Parameters.Add("isAfter", isAfter);
            Execute();
        }
}