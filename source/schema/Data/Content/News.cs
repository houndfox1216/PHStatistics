using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Framework.Content;
using System.Framework.Data;
using System.Linq;
using System.Runtime.Serialization;

namespace PHStatistics {
    /// <summary>
    /// 新聞資料
    /// </summary>
    [Description("新聞資料")]
    public class News : INewsData, ISortable {
        #region INewsData 成員

        #region IArticleData 成員

        #region IEntityData 成員

        object IEntityData.Id => Id;
        string IEntityData.Name => Title?.DefaultText ?? this.GetDefaultName<News>();

        #endregion

        string IArticleData.Author => null;
        string IArticleData.Title => Title?.DefaultText;
        string IArticleData.Introduction => Introduction?.DefaultText;
        string IArticleData.Content => Content?.DefaultText;

        #endregion

        #region IPublishable 成員

        DaysOfWeek? IPublishable.AllowedDays => null;
        DateTime? IPublishable.StartTime => StartDate;
        DateTime? IPublishable.EndTime => EndDate;
        TimeSpan? IPublishable.DailyStartTime => null;
        TimeSpan? IPublishable.DailyEndTime => null;

        #endregion

        #endregion

        #region ISortable memebers

        int ISortable.Ordinal => Ordinal ?? 0;

        #endregion

        private IEnumerable<Tag> tags = null;

        /// <summary>
        /// 新聞與標籤之關聯
        /// </summary>
        [Display(Name = "新聞與標籤之關聯"), DataMember]
        public ICollection<NewsTag> NewsTags { get; set; }

        /// <summary>
        /// 載入相關角色
        /// </summary>
        /// <param name="context">資料脈絡</param>
        public News LoadTags(DataContext context) {
            NewsTags = context.NewsTag.Where(e => e.News.Id == this.Id).ToList();
            return this;
        }

        /// <summary>
        /// 相關標籤
        /// </summary>
        [Display(Name = "相關標籤"), DataMember]
        [NotMapped]
        public IEnumerable<Tag> Tags => NewsTags?.Select(e => e.Tag ?? new Tag { Id = e.TagId }) ?? tags ?? Array.Empty<Tag>();

        /// <summary>
        /// 相關標籤
        /// </summary>
        [Display(Name = "相關標籤"), DataMember]
        [NotMapped]
        public int[] TagIds {
            get => Tags.Select(e => e.Id).ToArray();
            set {
                if (NewsTags != null) {
                    NewsTags.Clear();
                    foreach (var tagId in value) NewsTags.Add(new NewsTag { NewsId = Id, TagId = tagId });
                } else tags = value.Select(e => new Tag { Id = e });
            }
        }

        /// <summary>
        /// 識別碼
        /// </summary>
        [Display(Name = "識別碼"), DataMember]
        public int Id { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        [Display(Name = "建立時間"), DataMember]
        public DateTime? CreatedTime { get; set; }

        /// <summary>
        /// 更新時間
        /// </summary>
        [Display(Name = "更新時間"), DataMember]
        public DateTime? UpdatedTime { get; set; }

        /// <summary>
        /// 發佈開始日期
        /// </summary>
        [Display(Name = "發佈開始日期"), DataMember]
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// 發佈結束日期
        /// </summary>
        [Display(Name = "發佈結束日期"), DataMember]
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// 已發布
        /// </summary>
        [Display(Name = "已發布"), DataMember]
        public bool Published { get; set; }

        /// <summary>
        /// 排列順序
        /// </summary>
        [Display(Name = "排列順序"), DataMember]
        public int? Ordinal { get; set; }

        /// <summary>
        /// 標題
        /// </summary>
        [Display(Name = "標題"), DataMember]
        public MultilingualText Title { get; set; }

        /// <summary>
        /// 簡介
        /// </summary>
        [Display(Name = "簡介"), DataMember]
        public MultilingualText Introduction { get; set; }

        /// <summary>
        /// 內容
        /// </summary>
        [Display(Name = "內容"), DataMember]        
        public MultilingualText Content { get; set; }

        /// <summary>
        /// 類別識別碼
        /// </summary>
        [Display(Name = "類別識別碼"), DataMember]
        public int? CategoryId { get; set; }

        /// <summary>
        /// 類別
        /// </summary>
        [Display(Name = "類別"), DataMember]
        public Category Category { get; set; }

        /// <summary>
        /// 圖片
        /// </summary>
        [Display(Name = "圖片"), DataMember]
        public MultilingualImage Picture { get; set; }
    }
}
