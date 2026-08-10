using System;
using System.Framework.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PHStatistics.Audit;

namespace PHStatistics {
    /// <summary>
    /// 資料脈絡
    /// </summary>
    public partial class DataContext : EntityFrameworkContext {

        /// <summary>
        /// 建構資料脈絡
        /// </summary>
        public DataContext() : base("DataContext") {
            UtcOffset = new TimeSpan(8, 0, 0);
            TrackingEnabled = true;
        }

        /// <summary>
        /// 建構資料脈絡
        /// </summary>
        /// <param name="options">選項</param>
        public DataContext(DbContextOptions<DataContext> options) : base(options) => UtcOffset = new TimeSpan(8, 0, 0);

        /// <summary>
        /// 掛載全域稽核攔截器：不管是 new DataContext() 或透過 DI 建立，
        /// 只要呼叫 SaveChanges 就會自動記錄核心業務表的異動，見 PHStatistics.Audit.AuditSaveChangesInterceptor。
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.AddInterceptors(new AuditSaveChangesInterceptor());
        }

        #region OnModelCreating methods

        /// <summary>
        /// 當建立遷移(Migration)時執行，產生與 Basis Module 相關規則
        /// </summary>
        /// <param name="modelBuilder">針對建立的內容定義模型的產生器</param>
        private static partial void OnBasisMigrationCreating(ModelBuilder modelBuilder);

        /// <summary>
        /// 當建立遷移(Migration)時執行，產生與 Content Module 相關規則
        /// </summary>
        /// <param name="modelBuilder">針對建立的內容定義模型的產生器</param>
        private static partial void OnContentModelCreating(ModelBuilder modelBuilder);

        /// <summary>
        /// 當建立遷移(Migration)時執行，產生與 Community Module 相關規則
        /// </summary>
        /// <param name="modelBuilder">針對建立的內容定義模型的產生器</param>
        private static partial void OnCommunityModelCreating(ModelBuilder modelBuilder);

        /// <summary>
        /// 當建立遷移(Migration)時執行。 此方法的預設實作不會做任何事，但是可以在衍生類別中覆寫它，以便可以進一步設定此模型然後再將它鎖定。
        /// </summary>
        /// <param name="modelBuilder">針對建立的內容定義模型的產生器</param>
        /// <remarks>如果透過 UseModel 指定 Model 則因為不須建立 Model 而不會執行此方法</remarks>
        protected override void OnMigrationCreating(ModelBuilder modelBuilder) {
            // 預設以中文筆畫排序不區分大小寫，須區分則改成 Chinese_Taiwan_Stroke_BIN
            modelBuilder.UseCollation("Chinese_Taiwan_Stroke_CI_AS"); 
            OnBasisMigrationCreating(modelBuilder);
            OnContentModelCreating(modelBuilder);
            OnCommunityModelCreating(modelBuilder);
        }

        #endregion

        #region InitializeData methods

        /// <summary>
        /// 建立 Basis Module 之基本資料
        /// </summary>
        private partial void InitializeBasisData();

        /// <summary>
        /// 建立 Content Module 之基本資料
        /// </summary>
        private partial void InitializeContentData();

        /// <summary>
        /// 建立 Content Module 之基本資料
        /// </summary>
        private partial void InitializeCommunityData();

        /// <summary>
        /// 建立基本資料
        /// </summary>
        public override void InitializeData() {
            InitializeBasisData();
            InitializeContentData();
            InitializeCommunityData();
        }

        #endregion
    }
}
