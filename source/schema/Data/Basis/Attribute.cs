using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Framework.Data;
using System.Runtime.Serialization;

namespace EmptyProject {
    /// <summary>
    /// 屬性資料
    /// </summary>
    [Description("屬性資料"), DataContract(IsReference = true)]
    public class Attribute : IAttributeData {
        #region IEntityData 成員

        object IEntityData.Id { get { return Id; } }

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
        /// 代碼
        /// </summary>
        [Display(Name = "代碼"), DataMember]
        [MaxLength(16), Index(IsUnique = true)]
        public string Code { get; set; }

        /// <summary>
        /// 名稱
        /// </summary>
        [Display(Name = "名稱"), DataMember]
        [MaxLength(32)]
        public string Name { get; set; }

        /// <summary>
        /// 可選擇
        /// </summary>
        [Display(Name = "可選擇"), DataMember]
        public bool Selectable { get; set; }

        /// <summary>
        /// 可複選，須同時為可選擇
        /// </summary>
        [Display(Name = "可複選"), DataMember]
        public bool Multiple { get; set; }

        /// <summary>
        /// 此屬性是否為必填屬性
        /// </summary>
        [Display(Name = "必填"), DataMember]
        public bool Required { get; set; }

        /// <summary>
        /// 排序
        /// </summary>
        [Display(Name = "排序"), DataMember]
        public int? Ordinal { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        [Display(Name = "備註"), DataMember]
        [MaxLength(512)]
        public string Remark { get; set; }

        /// <summary>
        /// 屬性值
        /// </summary>
        [Display(Name = "屬性值"), DataMember]
        public ICollection<AttributeValue> Values { get; set; }
    }
}
