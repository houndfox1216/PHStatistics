using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Framework.Data;
using System.Framework.Streaming;
using System.Runtime.Serialization;

namespace EmptyProject {
    /// <summary>
    /// 直播來源
    /// </summary>
    [Description("直播來源"), DataContract(IsReference = true)]
    public class LiveSource : ILiveSourceData, IPublishable, ISortable {
        #region ILiveSourceData members

        #region IEntityData 成員

        object IEntityData.Id => Id;
        string IEntityData.Name => Title?.DefaultText ?? this.GetDefaultName<LiveSource>();

        #endregion

        bool IResourceData.Downloadable => true;
        string IResourceData.Uri { get => Uri; set => throw new NotSupportedException(); }
        string IResourceData.ContentType {
            get => MediaType switch {
                StreamingMediaType.Mp3 => "audio/mpeg",
                StreamingMediaType.Flv => "video/x-flv",
                _ => "video/mp4",
            };
            set => throw new NotSupportedException();
        }

        #endregion

        #region IPublishable members

        DaysOfWeek? IPublishable.AllowedDays => null;
        TimeSpan? IPublishable.DailyStartTime => null;
        TimeSpan? IPublishable.DailyEndTime => null;

        #endregion

        #region ISortable members

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
        /// 開始時間
        /// </summary>
        [Display(Name = "開始時間"), DataMember]
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// 結束時間
        /// </summary>
        [Display(Name = "結束時間"), DataMember]
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 標題
        /// </summary>
        [Display(Name = "標題"), DataMember]
        public MultilingualText Title { get; set; }

        /// <summary>
        /// 內容
        /// </summary>
        [Display(Name = "內容"), DataMember]
        public MultilingualText Content { get; set; }

        /// <summary>
        /// 發佈者識別碼
        /// </summary>
        [Display(Name = "發佈者"), DataMember]
        public Guid PublisherId { get; set; }

        /// <summary>
        /// 發佈者
        /// </summary>
        [Display(Name = "發佈者"), DataMember]
        public User Publisher { get; set; }

        /// <summary>
        /// 主機
        /// </summary>
        [Display(Name = "主機"), DataMember]
        public string Host { get; set; }

        /// <summary>
        /// 埠
        /// </summary>
        [Display(Name = "埠"), DataMember]
        public int Port { get; set; }

        /// <summary>
        /// 應用
        /// </summary>
        [Display(Name = "應用"), DataMember]
        public string Application { get; set; }

        /// <summary>
        /// 媒體型別
        /// </summary>
        [Display(Name = "媒體型別"), DataMember]
        public StreamingMediaType? MediaType { get; set; }

        /// <summary>
        /// 進入點
        /// </summary>
        [Display(Name = "進入點"), DataMember]
        public string Entry { get; set; }

        /// <summary>
        /// 令牌
        /// </summary>
        [Display(Name = "令牌"), DataMember]
        public string Token { get; set; }

        /// <summary>
        /// 縮圖網址
        /// </summary>
        [Display(Name = "縮圖網址"), DataMember]
        [MaxLength(2000)]
        public string ThumbnailUri { get; set; }

        /// <summary>
        /// 推送網址
        /// </summary>
        [Display(Name = "推送網址"), DataMember]
        [NotMapped]
        public string PushUri => this.GetPushUrl();

        /// <summary>
        /// 網址
        /// </summary>
        [Display(Name = "網址"), DataMember]
        [NotMapped]
        public string Uri => this.GetUri();

        /// <summary>
        /// MPEG-DASH 網址
        /// </summary>
        [Display(Name = "MPEG-DASH 網址")]
        [NotMapped]
        public string MpegDashUri => this.GetMpegDashUrl();

        /// <summary>
        /// HLS URL
        /// </summary>
        [Display(Name = "HLS網址")]
        [NotMapped]
        public string HlsUrl => this.GetHlsUrl();
    }
}