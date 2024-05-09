using System.Framework.EntityFrameworkCore;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace EmptyProject {
    /// <summary>
    /// 資料脈絡
    /// </summary>
    public partial class DataContext : EntityFrameworkContext {
        #region Manufacture Module Database Sets

        /// <summary>
        /// 商品資料
        /// </summary>
        public DbSet<Product> Product { get; set; }

        /// <summary>
        /// 包裝內容物
        /// </summary>
        public DbSet<PackedContent> PackedContent { get; set; }

        /// <summary>
        /// 產品屬性值
        /// </summary>
        public DbSet<ProductAttributeValue> ProductAttributeValue { get; set; }

        /// <summary>
        /// 商品類別
        /// </summary>
        public DbSet<ProductCategory> ProductCategory { get; set; }

        /// <summary>
        /// 商品標籤
        /// </summary>
        public DbSet<ProductTag> ProductTag { get; set; }

        #endregion

        /// <summary>
        /// 當產生內容模組之資料模型時需要執行的內容
        /// </summary>
        /// <param name="modelBuilder">針對建立的內容定義模型的產生器</param>
        private static partial void OnManufactureModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<Product>().HasOne(e => e.Base).WithMany().HasForeignKey(e => e.BaseId);

            modelBuilder.Entity<PackedContent>().HasOne(e => e.Product).WithMany(e => e.PackedContents).HasForeignKey(e => e.ProductId);
            modelBuilder.Entity<PackedContent>().HasOne(e => e.Content).WithMany().HasForeignKey(e => e.ContentId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductAttributeValue>().HasKey(e => new { e.ProductId, e.AttributeValueId });
            modelBuilder.Entity<ProductAttributeValue>().HasOne(e => e.Product).WithMany(e => e.ProductAttributeValues).HasForeignKey(e => e.ProductId);
            modelBuilder.Entity<ProductAttributeValue>().HasOne(e => e.AttributeValue).WithMany().HasForeignKey(e => e.AttributeValueId);

            modelBuilder.Entity<ProductCategory>().HasKey(e => new { e.ProductId, e.CategoryId });
            modelBuilder.Entity<ProductCategory>().HasOne(e => e.Product).WithMany(e => e.ProductCategories).HasForeignKey(e => e.ProductId);
            modelBuilder.Entity<ProductCategory>().HasOne(e => e.Category).WithMany().HasForeignKey(e => e.CategoryId);

            modelBuilder.Entity<ProductTag>().HasKey(e => new { e.ProductId, e.TagId });
            modelBuilder.Entity<ProductTag>().HasOne(e => e.Product).WithMany(e => e.ProductTags).HasForeignKey(e => e.ProductId);
            modelBuilder.Entity<ProductTag>().HasOne(e => e.Tag).WithMany().HasForeignKey(e => e.TagId);
        }

        private partial void InitializeManufactureData() {
            #region Product Attribute Value Seed Data

            if (!AttributeValue.Any()) {
                var attribute = Add(new Attribute { Code = "Color", Name = "顏色", Required = true, Selectable = true, Ordinal = 0 });
                Add(new AttributeValue { Attribute = attribute, TextValue = "藍色", DecimalValue = 0, Value = "#0369A1" });
                Add(new AttributeValue { Attribute = attribute, TextValue = "綠色", DecimalValue = 1, Value = "#065F46" });
                Add(new AttributeValue { Attribute = attribute, TextValue = "紅色", DecimalValue = 2, Value = "#9D174D" });
                attribute = Add(new Attribute { Code = "PlacceOfOrigin", Name = "產地", Selectable = true, Multiple = true, Ordinal = 1 });
                Add(new AttributeValue { Attribute = attribute, TextValue = "台灣", DecimalValue = 0, Value = "Taiwan" });
                Add(new AttributeValue { Attribute = attribute, TextValue = "日本", DecimalValue = 1, Value = "Japan" });
                Add(new AttributeValue { Attribute = attribute, TextValue = "韓國", DecimalValue = 2, Value = "Korea" });
                Add(new Attribute { Code = "Endorser", Name = "代言人", Ordinal = 2 });
                SaveChanges();
            }

            #endregion 
        }
    }
}
