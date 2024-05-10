using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Framework.Content;
using System.Framework.Data;
using System.Runtime.Serialization;

<<<<<<< HEAD
namespace EmptyProject {
=======
namespace PHStatistics {
>>>>>>> origin/develop/schema
    /// <summary>
    /// 廣告資料
    /// </summary>
    [Description("廣告資料"), DataContract(IsReference = true)]
    public class Banner : IBannerData {
        #region IBannerData 成員

        #region IEntityData 成員

        object IEntityData.Id { get { return Id; } }

        #endregion

        #region IBinaryResourceData 成員

        bool IResourceData.Downloadable { get { return false; } }

        #endregion

        #region ICountable 成員

        int ICountable.CheckoutCount { get { return 0; } }
        int ICountable.DownloadCount { get { return 0; } }
        int ICountable.SelectedCount { get { return 0; } }

        #endregion

        IBannerContractData IBannerData.Contract { get { return Position; } }

        #endregion

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
        /// 識別碼
        /// </summary>
        [Display(Name = "識別碼"), DataMember]
        public long Id { get; set; }

        /// <summary>
        /// 名稱
        /// </summary>
        [Display(Name = "名稱"), DataMember]
        [MaxLength(32)]
        public string Name { get; set; }

        /// <summary>
        /// 內容類型
        /// </summary>
        [Display(Name = "內容類型"), DataMember]
        [MaxLength(32)]
        public string ContentType { get; set; }

        /// <summary>
        /// 網址
        /// </summary>
        [Display(Name = "網址"), DataMember]
        [MaxLength(2000)]
        public string Uri { get; set; }

        /// <summary>
        /// 鏈結網址
        /// </summary>
        [Display(Name = "鏈結網址"), DataMember]
        [MaxLength(2000)]
        public string LinkUrl { get; set; }

        /// <summary>
        /// 點擊數
        /// </summary>
        [Display(Name = "點擊數"), DataMember]
        public int ClickCount { get; set; }

        /// <summary>
        /// 閱覽數
        /// </summary>
        [Display(Name = "閱覽數"), DataMember]
        public int ViewCount { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        [Display(Name = "備註"), DataMember]
        [MaxLength(512)]
        public string Remark { get; set; }

        /// <summary>
        /// 位置識別碼
        /// </summary>
        [Display(Name = "位置"), DataMember]
        public int? PositionId { get; set; }

        /// <summary>
        /// 位置
        /// </summary>
        [Display(Name = "位置"), DataMember]
        public BannerPosition Position { get; set; }

        /// <summary>
        /// 目錄
        /// </summary>
        public string Directory => $"~/files/banners/{PositionId ?? Position?.Id}";
    }
}