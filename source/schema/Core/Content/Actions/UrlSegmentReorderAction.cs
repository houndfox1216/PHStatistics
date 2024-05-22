using System;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Linq;
using System.Transactions;

namespace PHStatistics.Core.Content.Actions {
    /// <summary>
    /// 排序網址區段之操作。
    /// </summary>
    [Description("排序網址區段")]
    public class UrlSegmentReorderAction : ActionBase<DataContext, SystemPermission> {
        /// <summary>
        /// 必須具備的系統權限
        /// </summary>
        public override SystemPermission[] Permissions => new[] { SystemPermission.UrlSegment };

        /// <summary>
        /// 建構 CategoryReorderAction。
        /// </summary>
        /// <param name="user">請求操作的用戶</param>
        /// <param name="context">資料脈絡</param>
        public UrlSegmentReorderAction(IUser user, DataContext context = null) : base("排序類別資料", user, context) { }

        protected override void OnExecuting(DataContext context) {
            var dataContext = context ?? new DataContext();
            var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.RepeatableRead });
            try {
                var sourceRow = dataContext.UrlSegment.Find(Parameters.GetValue<int>("source"));
                var targetRow = dataContext.UrlSegment.Find(Parameters.GetValue<int>("target"));
                var isAfter = Parameters.GetValue<bool>("isAfter");

                sourceRow.ParentId = targetRow.ParentId;
                sourceRow.Ordinal = targetRow.Ordinal;

                var query = dataContext.UrlSegment.Where(e => e.ParentId == targetRow.ParentId && e.Id != sourceRow.Id);
                if (targetRow.Ordinal.HasValue) query = query.Where(e => e.Ordinal != null && e.Ordinal >= targetRow.Ordinal.Value);

                int? currentOrdinal = null;
                var rows = query.OrderBy(e => e.Ordinal).ToList();
                for (var i = 0; i < rows.Count; i++) {
                    var currentRow = rows[i];
                    var nextRow = i < rows.Count - 1 ? rows[i + 1] : null;
                    if (currentOrdinal.HasValue) {
                        if (nextRow != null && nextRow.Ordinal - currentOrdinal > 2) {
                            currentOrdinal += (nextRow.Ordinal - currentOrdinal) / 2;
                            break;
                        }
                        else {
                            try {
                                currentRow.Ordinal = (currentOrdinal += 100).Value;
                            }
                            catch {
                                var difference = int.MaxValue - currentOrdinal.Value;
                                if (difference > 0) currentRow.Ordinal = currentOrdinal++;
                                else currentOrdinal = currentRow.Ordinal = int.MaxValue;
                            }
                        }
                    }
                    else {
                        if (currentRow.Id == targetRow.Id) {
                            var afterRow = isAfter ? sourceRow : targetRow;
                            var difference = (nextRow?.Ordinal ?? 0) - (targetRow.Ordinal ?? 0);
                            if (nextRow != null && difference >= 2) {
                                currentOrdinal = afterRow.Ordinal = (afterRow.Ordinal ?? 0) + difference / 2;
                                break;
                            }
                            else {
                                try {
                                    currentOrdinal = afterRow.Ordinal = (afterRow.Ordinal ?? 0) + 100;
                                }
                                catch {
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
            }
            catch (FrameworkException ce) {
                throw new FrameworkException(ce.Message, ce.InnerException, MessageType.Info);
            }
            catch (System.Data.DataException due) {
                throw new DataException("資料異動中，請稍後再試", due, MessageType.Warning);
            }
            catch (Exception e) {
                throw new FrameworkException("目前無法進行相關操作，請聯繫維護人員!", e, MessageType.Error);
            }
            finally {
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
}
