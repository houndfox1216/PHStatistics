using System;
using System.Framework.Data;
using System.Framework.EntityFrameworkCore;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace EmptyProject {
    /// <summary>
    /// 資料脈絡
    /// </summary>
    public partial class DataContext : EntityFrameworkContext {
        #region Content Module Database Sets

        /// <summary>
        /// 訂單資料
        /// </summary>
        public DbSet<Order> Order { get; set; }

        /// <summary>
        /// 收單行
        /// </summary>
        public DbSet<Acquirer> Acquirer { get; set; }

        /// <summary>
        /// 繳費資料
        /// </summary>
        public DbSet<Payment> Payment { get; set; }

        /// <summary>
        /// 支付記錄
        /// </summary>
        public DbSet<PaymentRecord> PaymentRecord { get; set; }

        #endregion

        /// <summary>
        /// 當產生銷售模組之資料模型時需要執行的內容
        /// </summary>
        /// <param name="modelBuilder">針對建立的內容定義模型的產生器</param>
        private static partial void OnSalesModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<Order>().HasOne(e => e.Member).WithMany().HasForeignKey(e => e.MemberId);

            modelBuilder.Entity<OrderItem>().HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId);
            modelBuilder.Entity<OrderItem>().HasOne(e => e.Order).WithMany(e => e.Items).HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>().HasOne(e => e.Acquier).WithMany().HasForeignKey(e => e.AcquierId);
            modelBuilder.Entity<Payment>().HasOne(e => e.Confirmor).WithMany().HasForeignKey(e => e.ConfirmorId);
            modelBuilder.Entity<Payment>().HasOne(e => e.Order).WithMany(e => e.Payments).HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Payment>().HasMany(e => e.Records).WithOne(e => e.Payment).HasForeignKey(e => e.PaymentId).OnDelete(DeleteBehavior.Cascade);
        }

        private partial void InitializeSalesData() {
            #region Acquirer Seed Data

            if (!Acquirer.Any()) {
                Add(new Acquirer { DataMode = DataMode.System, Name = "台新商業銀行", PaymentType = PaymentType.CreditCard, ShortCode = "Taishin" });
                Add(new Acquirer { DataMode = DataMode.System, Name = "綠界科技ECPay", PaymentType = PaymentType.CreditCard, ShortCode = "EcPay" });
                Add(new Acquirer { DataMode = DataMode.System, Name = "LinePay", PaymentType = PaymentType.ThirdParty, ShortCode = "LinePay" });
                SaveChanges();
            }

            #endregion
        }
    }
}
