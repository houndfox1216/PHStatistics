using System;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Linq;
using System.Transactions;

namespace EmptyProject.Actions;

/// <summary>
/// 排序標籤資料之操作。
/// </summary>
[Description("排序標籤資料")]
public class TagReorderAction : ActionBase<DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.Category };

    /// <summary>
    /// 建構 CategoryReorderAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public TagReorderAction(IUser user, DataContext dbContext = null) : base("排序標籤資料", user, dbContext) { }

    protected override void OnExecuting(DataContext context) {
        var dataContext = context ?? new DataContext();
        var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.RepeatableRead });
        try {
            var sourceRow = dataContext.Tag.Find(Parameters.GetValue<int>("source"));
            var targetRow = dataContext.Tag.Find(Parameters.GetValue<int>("target"));
            var isAfter = Parameters.GetValue<bool>("isAfter");

            int? maxOrdinal = sourceRow.Ordinal.HasValue || targetRow.Ordinal.HasValue ? Math.Max(sourceRow.Ordinal ?? 0, targetRow.Ordinal ?? 0) : null;
            int? minOrdinal = sourceRow.Ordinal.HasValue && targetRow.Ordinal.HasValue ? Math.Min(sourceRow.Ordinal.Value, targetRow.Ordinal.Value) : null;
            sourceRow.Ordinal = targetRow.Ordinal;

            var query = dataContext.Tag.Where(e => e.Id != sourceRow.Id);
            if (maxOrdinal.HasValue) query.Where(e => e.Ordinal == null || e.Ordinal <= maxOrdinal);
            if (minOrdinal.HasValue) query.Where(e => e.Ordinal != null && e.Ordinal >= minOrdinal);

            var rows = query.OrderBy(e => e.Ordinal).ToList();

            int? currentOrdinal = null;
            for (var i = 0; i < rows.Count; i++) {
                var currentRow = rows[i];
                var nextRow = i < rows.Count - 1 ? rows[i + 1] : null;
                if (currentOrdinal.HasValue) {
                    var difference = (nextRow?.Ordinal ?? 0) - (currentOrdinal ?? 0);
                    if (nextRow != null && difference > 2) {
                        currentOrdinal += difference / 2;
                        break;
                    } else {
                        try {
                            currentRow.Ordinal = (currentOrdinal += 100).Value;
                        } catch {
                            difference = int.MaxValue - currentOrdinal.Value;
                            if (difference > 0) currentRow.Ordinal = currentOrdinal++;
                            else currentOrdinal = currentRow.Ordinal = int.MaxValue;
                        }
                    }
                } else {
                    if (currentRow.Id == targetRow.Id) {
                        var afterRow = isAfter ? sourceRow : targetRow;
                        var difference = (nextRow?.Ordinal ?? 0) - (targetRow.Ordinal ?? 0);
                        if (nextRow != null && difference >= 2) {
                            currentOrdinal = afterRow.Ordinal = (afterRow.Ordinal ?? 0) + difference / 2;
                            break;
                        } else {
                            try {
                                currentOrdinal = afterRow.Ordinal = (afterRow.Ordinal ?? 0) + 100;
                            } catch {
                                difference = int.MaxValue - afterRow.Ordinal ?? 0;
                                if (difference > 0) currentOrdinal = afterRow.Ordinal = (afterRow.Ordinal ?? 0) + 1;
                                else currentOrdinal = afterRow.Ordinal = int.MaxValue;
                            }
                        }
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