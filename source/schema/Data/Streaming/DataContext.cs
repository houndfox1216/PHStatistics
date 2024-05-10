using System.Framework.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EmptyProject {
    /// <summary>
    /// 資料脈絡
    /// </summary>
    public partial class DataContext : EntityFrameworkContext {
        #region Manufacture Module Database Sets

        /// <summary>
        /// 媒體檔案
        /// </summary>
        public DbSet<MediaFile> MediaFile { get; set; }

        /// <summary>
        /// 直撥來源
        /// </summary>
        public DbSet<LiveSource> LiveSource { get; set; }

        #endregion

        /// <summary>
        /// 當產生內容模組之資料模型時需要執行的內容
        /// </summary>
        /// <param name="modelBuilder">針對建立的內容定義模型的產生器</param>
        private static partial void OnStreamingModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<MediaFile>().HasOne(e => e.Publisher).WithMany().HasForeignKey(e => e.PublisherId);
            modelBuilder.Entity<LiveSource>().HasOne(e => e.Publisher).WithMany().HasForeignKey(e => e.PublisherId);
        }

        private partial void InitializeStreamingData() { }
    }
}
