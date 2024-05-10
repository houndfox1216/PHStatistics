using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.EntityFrameworkCore;
using System.Framework.Globalization;
using System.Framework.Security;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace PHStatistics {
    /// <summary>
    /// 資料脈絡
    /// </summary>
    public partial class DataContext : EntityFrameworkContext {
        #region Basis Module Database Sets

        /// <summary>
        /// 序號資料
        /// </summary>
        public DbSet<Sequence> Sequence { get; set; }

        /// <summary>
        /// 操作紀錄
        /// </summary>
        public DbSet<ActionLog> ActionLog { get; set; }

        /// <summary>
        /// 系統資源
        /// </summary>
        public DbSet<Resource> Resource { get; set; }

        /// <summary>
        /// 文字資源
        /// </summary>
        public DbSet<StringResource> StringResource { get; set; }

        /// <summary>
        /// 文化特性
        /// </summary>
        public DbSet<Culture> Culture { get; set; }

        /// <summary>
        /// 多語言文本
        /// </summary>
        public DbSet<MultilingualText> MultilingualText { get; set; }

        /// <summary>
        /// 圖片資料
        /// </summary>
        public DbSet<Picture> Picture { get; set; }

        /// <summary>
        /// 多語言圖像
        /// </summary>
        public DbSet<MultilingualImage> MultilingualImage { get; set; }

        /// <summary>
        /// 圖片集
        /// </summary>
        public DbSet<Album> Album { get; set; }

        /// <summary>
        /// 地址資料
        /// </summary>
        public DbSet<Address> Address { get; set; }

        /// <summary>
        /// 人務資料
        /// </summary>
        public DbSet<Person> Person { get; set; }

        /// <summary>
        /// 用戶資料
        /// </summary>
        public DbSet<User> User { get; set; }

        /// <summary>
        /// 角色資料
        /// </summary>
        public DbSet<Role> Role { get; set; }

        /// <summary>
        /// 用戶角色對應資料集合
        /// </summary>
        public DbSet<UserRole> UserRole { get; set; }

        /// <summary>
        /// 屬性資料
        /// </summary>
        public DbSet<Attribute> Attribute { get; set; }

        /// <summary>
        /// 屬性值
        /// </summary>
        public DbSet<AttributeValue> AttributeValue { get; set; }

        /// <summary>
        /// 類別資訊
        /// </summary>
        public DbSet<Category> Category { get; set; }

        /// <summary>
        /// 標籤資料
        /// </summary>
        public DbSet<Tag> Tag { get; set; }

        #endregion

        /// <summary>
        /// 當產生內容模組之資料模型時需要執行的內容
        /// </summary>
        /// <param name="modelBuilder">針對建立的內容定義模型的產生器</param>
        private static partial void OnBasisMigrationCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<Sequence>().HasKey(e => new { e.EntityType, e.SubCode, e.ExtendCode });

            modelBuilder.Entity<MultilingualText>().HasMany(e => e.Texts).WithOne().OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MultilingualImage>().HasMany(e => e.Images).WithOne().OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>().HasOne(e => e.Person).WithMany().HasForeignKey(e => e.PersonId);

            modelBuilder.Entity<UserRole>().HasKey(e => new { e.UserId, e.RoleId });
            modelBuilder.Entity<UserRole>().HasOne(e => e.User).WithMany(e => e.UserRoles).HasForeignKey(e => e.UserId);
            modelBuilder.Entity<UserRole>().HasOne(e => e.Role).WithMany().HasForeignKey(e => e.RoleId);

            modelBuilder.Entity<Album>().HasOne(e => e.Cover).WithOne().HasForeignKey<Album>("CoverId");
            modelBuilder.Entity<Album>().HasMany(e => e.Pictures).WithOne(e => e.Album).HasForeignKey(e => e.AlbumId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Attribute>().HasMany(e => e.Values).WithOne(e => e.Attribute).HasForeignKey(e => e.AttributeId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Attribute>().HasOne(e => e.Category).WithMany().HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Category>().HasOne(e => e.Parent).WithMany(e => e.Children).HasForeignKey(e => e.ParentId);
        }

        private partial void InitializeBasisData() {
            #region 文化特性 Seed Data

            if (!Culture.Any()) {
                Add(new Culture {
                    Id = "zh-TW",
                    Name = "繁體中文",
                    Codes = "zh-TW,zh-Hant,zh-Hant-TW,zh-CHT",
                    Language = LanguageCode.zh.ToString(),
                    Script = ScriptCode.Hant.ToString(),
                    ScriptDirection = ScriptDirection.LeftToRight,
                    Region = CountryCode.TW.ToString(),
                    Currency = CurrencyCode.TWD.ToString(),
                    DateFormat = "yyyy/MM/dd",
                    TimeFormat = "HH:mm:ss",
                    NumberFormat = "N0",
                    CurrencyFormat = "NT${0:C}",
                    Picture = new Picture { Uri = "/resources/flags/tw.svg" },
                    Ordinal = 0,
                    IsDefault = true,
                });
                Add(new Culture {
                    Id = "en-US",
                    Name = "英文",
                    Codes = "en-US",
                    Language = LanguageCode.en.ToString(),
                    ScriptDirection = ScriptDirection.LeftToRight,
                    Region = CountryCode.US.ToString(),
                    Currency = CurrencyCode.USD.ToString(),
                    DateFormat = "MM/dd/yyyy",
                    TimeFormat = "HH:mm:ss",
                    NumberFormat = "N0",
                    CurrencyFormat = "${0:C}",
                    Picture = new Picture { Uri = "/resources/flags/us.svg" },
                    Ordinal = 1,
                });
                Add(new Culture {
                    Id = "ja-JP",
                    Name = "日文",
                    Codes = "ja-JP,ja-Jpan,ja-Jpan-JP",
                    Language = LanguageCode.ja.ToString(),
                    Script = ScriptCode.Jpan.ToString(),
                    ScriptDirection = ScriptDirection.LeftToRight,
                    Region = CountryCode.JP.ToString(),
                    Currency = CurrencyCode.JPY.ToString(),
                    DateFormat = "yyyy/MM/dd",
                    TimeFormat = "HH:mm:ss",
                    NumberFormat = "N0",
                    CurrencyFormat = "¥{0:C}",
                    Picture = new Picture { Uri = "/resources/flags/jp.svg" },
                    Ordinal = 2,
                });
                SaveChanges();
            }

            #endregion
            #region 個人資料 Seed Data

            Person @operator = null;
            if (!Person.Any(e => e.DataMode == DataMode.System)) {
                Add(new Person {
                    DataMode = DataMode.System,
                    Name = "匿名",
                    Nickname = "Anonymous",
                    Photo = new Picture { Uri = "/resources/anonymous.jpg" },
                });
                @operator = Add(new Person {
                    DataMode = DataMode.System,
                    Name = "管理人員(匿名)",
                    Nickname = "Operator",
                    Photo = new Picture { Uri = "/resources/operator.png" },
                    Phone = "+886 7 226 9166",
                    Email = "service@cloudfun.com.tw",
                    Address = new Address { PostalCode = "800", City = "高雄市", District = "新興區", Line = "民生一路56號23F-1" }
                });
                SaveChanges();
            }
            @operator ??= Person.Single(e => e.DataMode == DataMode.System && e.Nickname == "Operator");

            #endregion
            #region 角色與用戶資料 Seed Data

            Role role = null;
            User user = null;
            if (!Role.Any(e => e.DataMode == DataMode.System)) { // Role seed sata
                role = Add(new Role {
                    DataMode = DataMode.System,
                    Name = "系統廠商",
                    Description = "系統預設權限，僅供系統人員使用。",
                    PermissionValue = "AQAAAA=="
                });
                var password = ShortUid.Generate("creator");
                user = Add(new User {
                    DataMode = DataMode.System,
                    Name = "系統人員",
                    Account = "creator",
                    Password = password.ComputeHashStringWithSha().ToBase64(),
                    Token = ShortUid.NewId,
                    Person = @operator,
                    Photo = new Picture { Uri = "/resources/cloudfun.png" },
                    Email = "service@cloudfun.com.tw",
                    Status = UserStatus.Enabled,
                });
                UserRole.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
                SaveChanges();
            }
            if (!Role.Any(e => e.DataMode != DataMode.System)) { // Role seed sata
                role = Add(new Role { Name = "系統管理", PermissionValue = "////////////////////" });
                user = Add(new User {
                    Name = "管理人員",
                    Account = "admin",
                    Password = "cloudfun".ComputeHashStringWithSha().ToBase64(),
                    Token = ShortUid.NewId,
                    Person = @operator,
                    Photo = new Picture { Uri = "/resources/operator.png" },
                    Status = UserStatus.Enabled
                });
                UserRole.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
                SaveChanges();
            }

            #endregion
        }
    }
}
