using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Framework.Content;
using System.Framework.Data;
using System.Runtime.Serialization;

namespace PHStatistics {
    /// <summary>
    /// 廣告位置
    /// </summary>
    [Description("廣告位置"), DataContract(IsReference = true)]
    public class BannerPosition : IBannerPositionData, IBannerContractData {
        #region IBannerContractData members

        #region IEntityData members

        object IEntityData.Id => Id;

        #endregion

        int IBannerContractData.RemainingViewTimes => -1;
        int IBannerContractData.RemainingClickTimes => -1;
        IBannerPositionData IBannerContractData.Position => this;

        #endregion

        /// <summary>
        /// 識別碼
        /// </summary>
        [Display(Name = "識別碼"), DataMember]
        public int Id { get; set; }

        /// <summary>
        /// 代碼
        /// </summary>
        [Display(Name = "代碼"), DataMember]
        [MaxLength(64), Index(IsUnique = true)]
        public string Code { get; set; }

        /// <summary>
        /// 名稱
        /// </summary>
        [Display(Name = "名稱"), DataMember]
        [MaxLength(32)]
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
        /// 資料模式
        /// </summary>
        [Display(Name = "資料模式"), DataMember]
        public DataMode DataMode { get; set; }

        /// <summary>
        /// 寬度(像素)
        /// </summary>
        [Display(Name = "寬度(像素)"), DataMember]
        [Range(16, 8192)]
        public int Width { get; set; }

        /// <summary>
        /// 高度(像素)
        /// </summary>
        [Display(Name = "高度(像素)"), DataMember]
        [Range(16, 8192)]
        public int Height { get; set; }

        /// <summary>
        /// 閱覽單價(元/次)
        /// </summary>
        [Display(Name = "閱覽單價(元/次)"), DataMember]
        [Decimal(18, 4), Range(0, int.MaxValue)]
        public decimal ViewUnitPrice { get; set; }

        /// <summary>
        /// 點擊單價(元/次)
        /// </summary>
        [Display(Name = "點擊單價(元/次)"), DataMember]
        [Decimal(18, 4), Range(0, int.MaxValue)]
        public decimal ClickUnitPrice { get; set; }

        /// <summary>
        /// 時間單價(元/分)
        /// </summary>
        [Display(Name = "時間單價(元/分)"), DataMember]
        [Decimal(18, 4), Range(0, int.MaxValue)]
        public decimal TimeSpanUnitPrice { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        [Display(Name = "備註"), DataMember]
        [MaxLength(512)]
        public string Remark { get; set; }

        /// <summary>
        /// 所有廣告
        /// </summary>
        [Display(Name = "所有廣告"), DataMember]
        public ICollection<Banner> Banners { get; set; }
    }
}
