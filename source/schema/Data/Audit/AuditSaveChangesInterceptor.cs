using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PHStatistics.Community;
using PHStatistics.Content;

namespace PHStatistics.Audit {
    /// <summary>
    /// 在 EF Core SaveChanges 層級自動攔截核心業務資料表的異動，寫入 ActionLog（ActionType="AutoAudit"）。
    /// 不管寫入是來自前台、後台Admin、批次匯入還是加總引擎，只要有 SaveChanges 都會被記錄，
    /// 不需要在各個 controller/action 手動補 log call。
    /// </summary>
    public class AuditSaveChangesInterceptor : SaveChangesInterceptor {
        // 稽核範圍：聚焦跟人數表填報、帳號權限、分校/課程設定直接相關的核心業務表。
        // CMS/多語系/雜項資料表（News、Banner、Album、StringResource…）刻意不列入，以控制紀錄量與雜訊。
        // 之後要擴大範圍只要在這裡加型別即可。
        private static readonly HashSet<Type> _includedTypes = new() {
            typeof(StudentPopulation), typeof(StudentPopulationItem),
            typeof(Member), typeof(MemberRole),
            typeof(Role), typeof(User), typeof(UserRole),
            typeof(School), typeof(SchoolAssignment), typeof(SchoolYear), typeof(SchoolClass),
            typeof(Course), typeof(CourseDepartment), typeof(Class),
        };

        // 框架自動維護的時間戳，異動時一定會變但不是有意義的業務diff，排除以減少雜訊
        private static readonly HashSet<string> _ignoredProperties = new() { "CreatedTime", "UpdatedTime" };

        // Added 狀態的entity在SavingChanges當下PK尚未產生，記下來等SavedChanges（存檔成功後）才能取得EntityId並補寫
        private readonly List<EntityEntry> _pendingAdded = new();

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result) {
            CaptureChanges(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) {
            CaptureChanges(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result) {
            FlushPendingAdded(eventData.Context);
            return base.SavedChanges(eventData, result);
        }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default) {
            await FlushPendingAddedAsync(eventData.Context, cancellationToken);
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        /// <summary>
        /// 存檔前執行：Modified/Deleted 的PK已知，直接組出 ActionLog 併入同一次 SaveChanges（同交易，確保業務資料跟稽核紀錄同進同出）。
        /// Added 的PK要等存檔完成才知道，先記下來等 SavedChanges 再補寫。
        /// </summary>
        private void CaptureChanges(DbContext context) {
            if (context == null) return;

            foreach (var entry in context.ChangeTracker.Entries()) {
                if (!_includedTypes.Contains(entry.Entity.GetType())) continue;

                switch (entry.State) {
                    case EntityState.Modified:
                    case EntityState.Deleted: {
                        var diffXml = BuildDiff(entry);
                        if (diffXml == null) break; // 沒有欄位真的變化（例如 DbSet.Update() 造成的假陽性），不記錄
                        var action = entry.State == EntityState.Deleted ? "Deleted" : "Modified";
                        context.Add(BuildActionLog(entry, action, diffXml));
                        break;
                    }
                    case EntityState.Added:
                        _pendingAdded.Add(entry);
                        break;
                }
            }
        }

        private void FlushPendingAdded(DbContext context) {
            var logs = BuildPendingAddedLogs();
            if (logs.Count == 0) return;
            context.Set<ActionLog>().AddRange(logs);
            context.SaveChanges();
        }

        private async Task FlushPendingAddedAsync(DbContext context, CancellationToken cancellationToken) {
            var logs = BuildPendingAddedLogs();
            if (logs.Count == 0) return;
            context.Set<ActionLog>().AddRange(logs);
            await context.SaveChangesAsync(cancellationToken);
        }

        private List<ActionLog> BuildPendingAddedLogs() {
            if (_pendingAdded.Count == 0) return new List<ActionLog>();
            var logs = _pendingAdded.Select(entry => BuildActionLog(entry, "Added", BuildAddedDiff(entry))).ToList();
            _pendingAdded.Clear();
            return logs;
        }

        /// <summary>
        /// 比較 Modified/Deleted entity 的 OriginalValue 與 CurrentValue，只收錄真的有變化的欄位。
        /// 注意：不能只看 PropertyEntry.IsModified——這個專案大量用 DbSet.Update(item)，
        /// EF Core對這個API的行為是不管值有沒有真的變都會把整個entity所有屬性標成Modified，
        /// 必須自己比對值才能濾掉這種假陽性，否則幾乎每次存檔都會被誤判成「全部欄位都變了」。
        /// </summary>
        private static string BuildDiff(EntityEntry entry) {
            var changes = new List<XElement>();
            foreach (var prop in entry.Properties) {
                var name = prop.Metadata.Name;
                if (_ignoredProperties.Contains(name)) continue;

                if (entry.State == EntityState.Deleted) {
                    changes.Add(new XElement("Field", new XAttribute("Name", name), new XAttribute("Old", prop.OriginalValue?.ToString() ?? "")));
                    continue;
                }

                if (Equals(prop.OriginalValue, prop.CurrentValue)) continue;
                changes.Add(new XElement("Field",
                    new XAttribute("Name", name),
                    new XAttribute("Old", prop.OriginalValue?.ToString() ?? ""),
                    new XAttribute("New", prop.CurrentValue?.ToString() ?? "")));
            }
            return changes.Count == 0 ? null : new XElement("Changes", changes).ToString(SaveOptions.DisableFormatting);
        }

        private static string BuildAddedDiff(EntityEntry entry) {
            var changes = entry.Properties
                .Where(p => !_ignoredProperties.Contains(p.Metadata.Name) && p.CurrentValue != null)
                .Select(p => new XElement("Field", new XAttribute("Name", p.Metadata.Name), new XAttribute("New", p.CurrentValue.ToString())));
            return new XElement("Changes", changes).ToString(SaveOptions.DisableFormatting);
        }

        private static ActionLog BuildActionLog(EntityEntry entry, string action, string diffXml) {
            var type = entry.Entity.GetType();
            return new ActionLog {
                CreatedTime = DateTime.Now,
                ActionType = "AutoAudit",
                ActionName = $"自動稽核：{ActionDisplayName(action)}",
                UserType = "Member",
                UserId = Truncate(AuditActorContext.CurrentActorId ?? "System", 128),
                UserName = Truncate(AuditActorContext.CurrentActorName, 32),
                EntityType = type.Name,
                EntityTypeName = GetEntityTypeName(type),
                EntityId = Truncate(GetEntityId(entry), 128),
                EntityName = Truncate(GetEntityName(entry), 128),
                Xml = diffXml,
            };
        }

        private static string Truncate(string value, int maxLength) =>
            value == null || value.Length <= maxLength ? value : value[..maxLength];

        private static string ActionDisplayName(string action) => action switch {
            "Added" => "新增",
            "Deleted" => "刪除",
            _ => "修改",
        };

        private static string GetEntityId(EntityEntry entry) {
            var keyProps = entry.Metadata.FindPrimaryKey()?.Properties;
            if (keyProps == null || keyProps.Count == 0) return null;
            return string.Join(",", keyProps.Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? ""));
        }

        private static string GetEntityName(EntityEntry entry) {
            try {
                return (entry.Entity as System.Framework.Data.IEntityData)?.Name;
            }
            catch {
                return null;
            }
        }

        private static string GetEntityTypeName(Type type) =>
            type.GetCustomAttribute<DescriptionAttribute>()?.Description ?? type.Name;
    }
}
