using System.Threading;

namespace PHStatistics.Audit {
    /// <summary>
    /// 目前操作人員的環境資訊，供 <see cref="AuditSaveChangesInterceptor"/> 在寫入稽核紀錄時取得「是誰做的」。
    /// 放在 Data 層以避免相依 Web 層型別；由 Portal 層的全域 Filter 在每個 request 一開始寫入。
    /// 使用 AsyncLocal 讓值隨著同一個 request 的非同步呼叫鏈流動，不受 DataContext 是否經由 DI 建立影響。
    /// </summary>
    public static class AuditActorContext {
        private static readonly AsyncLocal<string> _actorId = new();
        private static readonly AsyncLocal<string> _actorName = new();

        /// <summary>
        /// 目前操作人員識別碼（對應 ActionLog.UserId，可能是 Guid 字串或 "System"）。
        /// </summary>
        public static string CurrentActorId {
            get => _actorId.Value;
            set => _actorId.Value = value;
        }

        /// <summary>
        /// 目前操作人員名稱（對應 ActionLog.UserName）。
        /// </summary>
        public static string CurrentActorName {
            get => _actorName.Value;
            set => _actorName.Value = value;
        }
    }
}
