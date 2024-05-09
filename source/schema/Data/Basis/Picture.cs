using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Framework;
using System.Framework.Data;
using System.Runtime.Serialization;

using Environment = System.Framework.Environment;

namespace EmptyProject {
    /// <summary>
    /// 圖片資料
    /// </summary>
    [Description("圖片資料"), DataContract(IsReference = true)]
    public class Picture : IPictureData {
        #region IPictureData 成員

        #region IEntityData 成員

        object IEntityData.Id => Id;
        string IEntityData.Name => Name ?? this.GetDefaultName<Picture>();

        #endregion

        #region IResourceData 成員

        bool IResourceData.Downloadable => false;
        bool IPictureData.IsDefault => false;

        #endregion

        #region IPublishable 成員

        DaysOfWeek? IPublishable.AllowedDays => null;
        DateTime? IPublishable.StartTime => StartDate;
        DateTime? IPublishable.EndTime => EndDate;
        TimeSpan? IPublishable.DailyStartTime => null;
        TimeSpan? IPublishable.DailyEndTime => null;

        #endregion

        IPictureGalleryData IPictureData.Gallery => Album;
        IBinaryResourceData IPictureData.Binary => null;

        #endregion

        /// <summary>
        /// 識別碼
        /// </summary>
        [Display(Name = "識別碼"), DataMember]
        public int Id { get; set; }

        /// <summary>
        /// 名稱
        /// </summary>
        [Display(Name = "名稱"), DataMember]
        [MaxLength(64)]
        public string Name { get; set; }

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
        /// 文化特性
        /// </summary>
        [Display(Name = "文化特性"), DataMember]
        public string Culture { get; set; }

        /// <summary>
        /// 類型
        /// </summary>
        [Display(Name = "類型"), DataMember]
        public PictureType Type { get; set; }

        /// <summary>
        /// 內容類型
        /// </summary>
        [Display(Name = "內容類型"), DataMember]
        public string ContentType { get; set; }

        /// <summary>
        /// 網址
        /// </summary>
        [Display(Name = "網址"), DataMember]
        [MaxLength(2000)]
        public string Uri { get; set; }

        /// <summary>
        /// 縮圖網址
        /// </summary>
        [Display(Name = "縮圖網址"), DataMember]
        [MaxLength(2000)]
        public string ThumbnailUri { get; set; }

        /// <summary>
        /// 鏈結網址
        /// </summary>
        [Display(Name = "鏈結網址"), DataMember]
        [MaxLength(2000)]
        public string LinkUrl { get; set; }

        /// <summary>
        /// 已發佈
        /// </summary>
        [Display(Name = "已發佈"), DataMember]
        public bool Published { get; set; }

        /// <summary>
        /// 開始時間
        /// </summary>
        [Display(Name = "開始時間"), DataMember]
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// 結束時間
        /// </summary>
        [Display(Name = "結束時間"), DataMember]
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// 排列順序
        /// </summary>
        [Display(Name = "排列順序"), DataMember]
        public int Ordinal { get; set; }

        /// <summary>
        /// 高度(Pixel)
        /// </summary>
        [Display(Name = "高度"), DataMember]
        public int? Height { get; set; }

        /// <summary>
        /// 寬度(Pixel)
        /// </summary>
        [Display(Name = "寬度"), DataMember]
        public int? Width { get; set; }

        /// <summary>
        /// 解析度
        /// </summary>
        [Display(Name = "解析度"), DataMember]
        public int? Dpi { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        [Display(Name = "備註"), DataMember]
        [MaxLength(512)]
        public string Remark { get; set; }

        /// <summary>
        /// 相簿識別碼
        /// </summary>
        [Display(Name = "相簿"), DataMember]
        public int? AlbumId { get; set; }

        /// <summary>
        /// 相簿
        /// </summary>
        [Display(Name = "相簿"), DataMember]
        public Album Album { get; set; }

        /// <summary>
        /// 目錄
        /// </summary>
        public string Directory => Album.HasValue() ? $"~/files/albums/{Album.Number}" : Environment.Directory.ImagePath;
    }
}
