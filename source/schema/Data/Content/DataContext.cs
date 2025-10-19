using System.Linq;
using Microsoft.EntityFrameworkCore;
using PHStatistics.Community;
using PHStatistics.Content;

// ReSharper disable UnusedAutoPropertyAccessor.Global

// ReSharper disable once CheckNamespace
namespace PHStatistics;

/// <summary>
/// 資料脈絡
/// </summary>
public partial class DataContext {
    #region Content Module Database Sets

    /// <summary>
    /// 廣告位置
    /// </summary>
    public DbSet<BannerPosition> BannerPosition { get; set; }

    /// <summary>
    /// 廣告資料
    /// </summary>
    public DbSet<Banner> Banner { get; set; }

    /// <summary>
    /// 新聞資料
    /// </summary>
    public DbSet<News> News { get; set; }

    /// <summary>
    /// 新聞標籤
    /// </summary>
    public DbSet<NewsTag> NewsTag { get; set; }

    /// <summary>
    /// 分校
    /// </summary>
    public DbSet<School> School { get; set; }

    /// <summary>
    /// 班系
    /// </summary>
    public DbSet<CourseDepartment> CourseDepartment { get; set; }

    /// <summary>
    /// 課程
    /// </summary>
    public DbSet<Course> Course { get; set; }

    /// <summary>
    /// 班級
    /// </summary>
    public DbSet<Class> Class { get; set; }

    /// <summary>
    /// 人數表
    /// </summary>
    public DbSet<StudentPopulation> StudentPopulation { get; set; }

    /// <summary>
    /// 人數表
    /// </summary>
    public DbSet<StudentPopulationItem> StudentPopulationItem { get; set; }


    /// <summary>
    /// 分校人員指派
    /// </summary>
    public DbSet<SchoolAssignment> SchoolAssignment { get; set; }

    /// <summary>
    /// 網址區段
    /// </summary>
    public DbSet<UrlSegment> UrlSegment { get; set; }

    /// <summary>
    /// 人數表Log
    /// </summary>
    public DbSet<StudentPopulationItemLog> StudentPopulationItemLog { get; set; }

    /// <summary>
    /// 學年度
    /// </summary>
    public DbSet<SchoolYear> SchoolYear { get; set; }

    /// <summary>
    /// 分校開班情況
    /// </summary>
    public DbSet<SchoolClass> SchoolClass { get; set; }

    /// <summary>
    /// 分校開班情況
    /// </summary>
    public DbSet<Mapping> Mapping { get; set; }

    



    #endregion

    /// <summary>
    /// 當產生內容模組之資料模型時需要執行的內容
    /// </summary>
    /// <param name="modelBuilder">針對建立的內容定義模型的產生器</param>
    private static partial void OnContentModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<BannerPosition>().HasMany(e => e.Banners).WithOne(e => e.Position).HasForeignKey(e => e.PositionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UrlSegment>().HasOne(e => e.Parent).WithMany(e => e.Children).HasForeignKey(e => e.ParentId);
        modelBuilder.Entity<UrlSegment>().HasOne(e => e.Page).WithMany().HasForeignKey(e => e.PageId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<News>().HasOne(e => e.Category).WithMany().HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<NewsTag>().HasKey(e => new { e.NewsId, e.TagId });
        modelBuilder.Entity<NewsTag>().HasOne(e => e.News).WithMany(e => e.NewsTags).HasForeignKey(e => e.NewsId);
        modelBuilder.Entity<NewsTag>().HasOne(e => e.Tag).WithMany().HasForeignKey(e => e.TagId);


        modelBuilder.Entity<Course>().HasOne(e => e.Department).WithMany().HasForeignKey(e => e.DepartmentId);
        modelBuilder.Entity<Class>().HasOne(e => e.School).WithMany().HasForeignKey(e => e.SchoolId);
        modelBuilder.Entity<Class>().HasOne(e => e.Course).WithMany().HasForeignKey(e => e.CourseId);
        modelBuilder.Entity<StudentPopulation>().HasOne(e => e.School).WithMany().HasForeignKey(e => e.SchoolId);
        modelBuilder.Entity<StudentPopulation>().HasMany(e => e.Items).WithOne(e => e.StudentPopulation).HasForeignKey(e => e.StudentPopulationId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StudentPopulation>().HasMany(e => e.ItemsLog).WithOne(e => e.StudentPopulation).HasForeignKey(e => e.StudentPopulationId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StudentPopulationItem>().HasOne(e => e.Class).WithMany().HasForeignKey(e => e.ClassId);
        //modelBuilder.Entity<StudentPopulationItem>().HasOne(e => e.StudentPopulation).WithMany().HasForeignKey(e => e.StudentPopulationId);

        modelBuilder.Entity<School>().HasMany(e => e.SchoolAssignment).WithOne(e => e.School).HasForeignKey(e => e.SchoolId).OnDelete(DeleteBehavior.Cascade);
    }

    private partial void InitializeContentData() {
        #region Banner Position Seed Data

        if (BannerPosition.FirstOrDefault(e => e.Code == "Home.Slider") is not { } position) {
            position = Add(new BannerPosition { Code = "Home.Slider", Name = "首頁Slider", Width = 1140, Height = 395 });
            SaveChanges();
        }

        #endregion

        #region Banner Seed Data

        if (!Banner.Any()) {
            Add(new Banner { Position = position, Uri = "/resources/sample-banner-1.png" });
            Add(new Banner { Position = position, Uri = "/resources/sample-banner-2.png" });
            SaveChanges();
        }

        #endregion

        #region Category Seed Data

        if (!Category.Any()) {
            var category = Add(new Category { Name = "智慧家居", HasChild = true });
            Add(new Category { Parent = category, Name = "電視" });
            Add(new Category { Parent = category, Name = "冰箱" });
            Add(new Category { Parent = category, Name = "洗衣機" });
            category = Add(new Category { Name = "體育休閒", HasChild = true });
            Add(new Category { Parent = category, Name = "棒球" });
            Add(new Category { Parent = category, Name = "籃球" });
            Add(new Category { Parent = category, Name = "羽球" });
            SaveChanges();
        }

        #endregion

        #region Tag Seed Data

        if (!Tag.Any()) {
            Add(new Tag { Name = "IoT" });
            Add(new Tag { Name = "節能補助" });
            Add(new Tag { Name = "MLB" });
            Add(new Tag { Name = "NBA" });
            SaveChanges();
        }

        #endregion

        #region UrlSegment Seed Data

        if (!UrlSegment.Any()) {
            var segment = Add(new UrlSegment {
                Name = "custom",
                Title = new MultilingualText {
                    DefaultText = "客製化網頁",
                    Texts = [
                        new StringResource { Culture = "zh-TW", Content = "客製化網頁" },
                        new StringResource { Culture = "en-US", Content = "Custom Page" },
                    ]
                },
                Ordinal = 100
            });
            SaveChanges();
            Add(new UrlSegment {
                ParentId = segment.Id,
                Name = "brand",
                Title = new MultilingualText {
                    DefaultText = "形象網頁",
                    Texts = [
                        new StringResource { Culture = "zh-TW", Content = "形象網頁" },
                        new StringResource { Culture = "en-US", Content = "Branded Page" },
                    ]
                },
                Ordinal = 100,
                Page = new Page {
                    Name = "客製化形象網頁",
                    Published = true,
                    MetaTitle = "客製化形象網頁",
                    MetaKeywords = "brand,page",
                    MetaDescription = "This is a branded page",
                    CanonicalUrl = "/custom/brand",
                    Content = new MultilingualText {
                        Texts = [
                            new StringResource {
                                Culture = "zh-TW",
                                Content = "@await Html.PartialAsync(\"~/Views/Shared/Components/Breadcrumb.cshtml\")" +
                                          "<div class=\"px-3\">" +
                                          "<h3>形象網頁示意圖 - @DateTime.Now</h3>" +
                                          "<img width=\"100%\" src=\"/resources/sample-brand.png\" alt=\"branded page\" />" +
                                          "</div>"
                            },
                            new StringResource {
                                Culture = "en-US",
                                Content = "@await Html.PartialAsync(\"~/Views/Shared/Components/Breadcrumb.cshtml\")" +
                                          "<div class=\"px-3\">" +
                                          "<h3>Branded Page Sketch Map - @DateTime.Now</h3>" +
                                          "<img width=\"100%\" src=\"/resources/sample-brand.png\" alt=\"branded page\" />" +
                                          "</div>"
                            },
                        ]
                    }
                }
            });
            Add(new UrlSegment {
                ParentId = segment.Id,
                Name = "esg",
                Title = new MultilingualText {
                    DefaultText = "ESG網頁",
                    Texts = [
                        new StringResource { Culture = "zh-TW", Content = "ESG網頁" },
                        new StringResource { Culture = "en-US", Content = "ESG Page" },
                    ]
                },
                Ordinal = 200,
                Page = new Page {
                    Name = "客製化ESG網頁",
                    Published = true,
                    MetaTitle = "客製化ESG網頁",
                    MetaKeywords = "esg,page",
                    MetaDescription = "This is an ESG page",
                    CanonicalUrl = "/custom/esg",
                    Content = new MultilingualText {
                        Texts = [
                            new StringResource {
                                Culture = "zh-TW",
                                Content = "@await Html.PartialAsync(\"~/Views/Shared/Components/Breadcrumb.cshtml\")" +
                                          "<div class=\"px-3\">" +
                                          "<h3>ESG網頁示意圖 - @DateTime.Now</h3>" +
                                          "<img width=\"100%\" src=\"/resources/sample-esg.png\" alt=\"ESG page\" />" +
                                          "</div>"
                            },
                            new StringResource {
                                Culture = "en-US",
                                Content = "@await Html.PartialAsync(\"~/Views/Shared/Components/Breadcrumb.cshtml\")" +
                                          "<div class=\"px-3\">" +
                                          "<h3>ESG Page Sketch Map - @DateTime.Now</h3>" +
                                          "<img width=\"100%\" src=\"/resources/sample-esg.png\" alt=\"ESG page\" />" +
                                          "</div>"
                            },
                        ]
                    }
                }
            });
            Add(new UrlSegment {
                ParentId = segment.Id,
                Name = "Cloudfun",
                Title = new MultilingualText {
                    DefaultText = "雲方資訊",
                    Texts = [
                        new StringResource { Culture = "zh-TW", Content = "雲方資訊" },
                        new StringResource { Culture = "en-US", Content = "CloudFun" },
                    ]
                },
                Ordinal = 300,
                LinkUrl = "http://www.cloudfun.com.tw"
            });
            SaveChanges();
        }

        #endregion
    }
}