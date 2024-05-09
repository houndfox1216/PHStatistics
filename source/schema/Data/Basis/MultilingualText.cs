using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Framework.Data;
using System.Framework.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace EmptyProject {
    /// <summary>
    /// 多語言文本
    /// </summary>
    [Description("多語言文本"), DataContract(IsReference = true)]
    public class MultilingualText : IMultilingualTextData {
        #region IMultilingualText members

        #region IEntityData 成員

        object IEntityData.Id => Id;
        string IEntityData.Name => this.GetDefaultName<MultilingualText>();

        #endregion

        ICollection<IStringResourceData> IMultilingualTextData.Texts => new List<IStringResourceData>(Texts ?? Array.Empty<StringResource>()).AsReadOnly();
        IStringResourceData IMultilingualTextData.GetText(string culture) => GetText(culture);

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
        /// 多語言文本集合
        /// </summary>
        [Display(Name = "多語言文本集合"), DataMember]
        public ICollection<StringResource> Texts { get; set; }

        /// <summary>
        /// 預設文本
        /// </summary>
        [Display(Name = "預設文本"), DataMember]
        public string DefaultText { get; set; }

        /// <summary>
        ///  取得以指定文字代碼呈現的文本
        /// </summary>
        /// <param name="culture">文化特性</param>
        /// <returns>以指定文化特性呈現的文本</returns>
        public StringResource GetText(string culture) => Texts?.FirstOrDefault(e => e.Culture == culture);
    }
}
