using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Framework.Data;
using System.Runtime.Serialization;

namespace EmptyProject {
    /// <summary>
    /// 屬性資料
    /// </summary>
    [Description("屬性資料"), DataContract(IsReference = true)]
    public class AttributeValue : IAttributeValueData {
        #region IAttributeValueData 成員

        #region IEntityData 成員

        object IEntityData.Id => Id;
        string IEntityData.Name => this.GetDefaultName<AttributeValue>();

        #endregion

        IAttributeData IAttributeValueData.Attribute => Attribute;

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
        /// 屬性識別碼
        /// </summary>
        [Display(Name = "屬性識別碼"), DataMember]
        public int AttributeId { get; set; }

        /// <summary>
        /// 屬性
        /// </summary>
        [Display(Name = "屬性"), DataMember]
        public Attribute Attribute { get; set; }

        /// <summary>
        /// 值
        /// </summary>
        [Display(Name = "值"), DataMember]
        public string Value { get; set; }

        /// <summary>
        /// 文字
        /// </summary>
        [Display(Name = "文字"), DataMember]
        public string TextValue { get; set; }

        /// <summary>
        /// 數值
        /// </summary>
        [Display(Name = "數值"), DataMember]
        [Decimal(16, 2)]
        public decimal? DecimalValue { get; set; }
    }
}
