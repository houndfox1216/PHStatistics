using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Framework.Data;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore;

<<<<<<< HEAD
namespace EmptyProject {
=======
namespace PHStatistics {
>>>>>>> origin/develop/schema
    /// <summary>
    /// 相簿
    /// </summary>
    [Description("相簿"), DataContract(IsReference = true)]
    public class Album : IPictureGalleryData, IPublishable, ISortable, IOperability {
        #region IPictureGalleryData 成員

        #region IEntityData 成員

        object IEntityData.Id => Id;
        string IEntityData.Name => Title;

        #endregion

        ICollection<IPictureData> IPictureGalleryData.Pictures => new List<IPictureData>(Pictures).AsReadOnly();

        #endregion

        #region IPublishable 成員

        DaysOfWeek? IPublishable.AllowedDays => null;
        DateTime? IPublishable.StartTime => StartDate;
        DateTime? IPublishable.EndTime => EndDate;
        TimeSpan? IPublishable.DailyStartTime => null;
        TimeSpan? IPublishable.DailyEndTime => null;

        #endregion

        #region ISortable memebers

        int ISortable.Ordinal => Ordinal ?? 0;

        #endregion

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
        /// 資料模式
        /// </summary>
        [Display(Name = "資料模式"), DataMember]
        public DataMode DataMode { get; set; }

        /// <summary>
        /// 編號
        /// </summary>
        [Display(Name = "編號"), DataMember]
        [Unicode(false), MaxLength(32)]
        public string Number { get; set; }

        /// <summary>
        /// 標題
        /// </summary>
        [Display(Name = "標題"), DataMember]
        [MaxLength(64)]
        public string Title { get; set; }

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
        /// 排序值
        /// </summary>
        [Display(Name = "排序值"), DataMember]
        public int? Ordinal { get; set; }

        /// <summary>
        /// 封面
        /// </summary>
        [Display(Name = "封面"), DataMember]
        public Picture Cover { get; set; }

        /// <summary>
        /// 所有圖片
        /// </summary>
        [Display(Name = "所有圖片"), DataMember]
        public ICollection<Picture> Pictures { get; set; }
    }
}
