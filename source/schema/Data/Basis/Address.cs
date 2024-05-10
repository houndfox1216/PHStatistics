using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Framework.Data;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore;

<<<<<<< HEAD
namespace EmptyProject {
=======
namespace PHStatistics {
>>>>>>> origin/develop/schema
    /// <summary>
    /// 地址資料
    /// </summary>
    [Description("地址資料"), DataContract(IsReference = true)]
    public class Address : IAddressData {
        #region IAddressData 成員

        string IAddressData.Country => null;
        string IAddressData.Province => null;

        #region IEntityData 成員

        object IEntityData.Id { get { return Id; } }
        string IEntityData.Name { get { return this.GetDefaultName<Address>(); } }

        #endregion

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
        /// 類型
        /// </summary>
        [Display(Name = "類型"), DataMember]
        public AddressType Type { get; set; }

        /// <summary>
        /// 郵遞區號
        /// </summary>
        [Display(Name = "郵遞區號"), DataMember]
        [MaxLength(10), Unicode(false)]
        public string PostalCode { get; set; }

        /// <summary>
        /// 區域
        /// </summary>
        [Display(Name = "區域"), DataMember]
        [MaxLength(32)]
        public string Region { get; set; }

        /// <summary>
        /// 城市
        /// </summary>
        [Display(Name = "城市"), DataMember]
        [MaxLength(32)]
        public string City { get; set; }

        /// <summary>
        /// 地區
        /// </summary>
        [Display(Name = "地區"), DataMember]
        [MaxLength(32)]
        public string District { get; set; }

        /// <summary>
        /// 地址
        /// </summary>
        [Display(Name = "地址"), DataMember]
        [MaxLength(256)]
        public string Line { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        [Display(Name = "描述"), DataMember]
        [MaxLength(512)]
        public string Description { get; set; }

        /// <summary>
        /// 完整地址
        /// </summary>
        [Display(Name = "地址")]
        [NotMapped]
        public string Full { get { return string.Format("{0}({1}){2}{3}", City, PostalCode, District, Line); } }

        /// <summary>
        /// 完整地址
        /// </summary>
        [Display(Name = "地址")]
        [NotMapped]
        public string FullAddress { get { return string.Format("{0}{1}{2}", City, District, Line); } }
    }
}
