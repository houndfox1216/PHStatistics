using System.Framework;
using System.Framework.Community;
using System.Framework.Data;
using System.Framework.EntityFrameworkCore;
using System.Framework.Security;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace PHStatistics {
    /// <summary>
    /// 資料脈絡
    /// </summary>
    public partial class DataContext : EntityFrameworkContext {
        #region Content Module Database Sets

        /// <summary>
        /// 會員資料
        /// </summary>
        public DbSet<Member> Member { get; set; }

        #endregion

        /// <summary>
        /// 當產生內容模組之資料模型時需要執行的內容
        /// </summary>
        /// <param name="modelBuilder">針對建立的內容定義模型的產生器</param>
        private static partial void OnCommunityModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<Member>().HasOne(e => e.Person).WithMany().HasForeignKey(e => e.PersonId);
            modelBuilder.Entity<Member>().HasMany(e => e.SchoolAssignment).WithOne(e => e.Member).HasForeignKey(e => e.MemberId).OnDelete(DeleteBehavior.Cascade);
        }

        private partial void InitializeCommunityData() {
            #region Banner Position Seed Data

            if (!Member.Any()) {
                Add(new Member {
                    DataMode = DataMode.System,
                    Nickname = "雲方客服",
                    Email = "service@cloudfun.com.tw",
                    Password = "cloudfun".ComputeHashStringWithSha().ToBase64(),
                    Token = ShortUid.NewId,
                    Person = Person.Single(e => e.DataMode == DataMode.System && e.Nickname == "Operator"),
                    Photo = new Picture { Uri = "/resources/cloudfun.png" },
                    Status = MemberStatus.Enabled,
                });
            }

            #endregion
        }
    }
}
