using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Framework.Data;
using System.Framework.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace PHStatistics {
    /// <summary>
    /// 多語言圖像
    /// </summary>
    [Description("文字資源"), DataContract(IsReference = true)]
    public class MultilingualImage : IMultilingualImageData {
        #region IMultilingualImageData members

        #region IEntityData 成員

        object IEntityData.Id => Id;
        string IEntityData.Name => this.GetDefaultName<MultilingualImage>();

        #endregion

        ICollection<IPictureData> IMultilingualImageData.Images => new List<IPictureData>(Images ?? Array.Empty<Picture>()).AsReadOnly();
        IPictureData IMultilingualImageData.GetImage(string culture) => GetImage(culture);

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
        /// 多語言圖像集合
        /// </summary>
        [Display(Name = "多語言圖像集合"), DataMember]
        public ICollection<Picture> Images { get; set; }

        /// <summary>
        /// 預設圖像URI
        /// </summary>
        [Display(Name = "預設圖像URI"), DataMember]
        [MaxLength(2000)]
        public string DefaultImageUri { get; set; }

        /// <summary>
        ///  取得以指定文字代碼呈現的圖像
        /// </summary>
        /// <param name="culture">文化特性</param>
        /// <returns>以指定文化特性呈現的圖像</returns>
        public Picture GetImage(string culture) => Images?.FirstOrDefault(e => e.Culture == culture);
    }
}
