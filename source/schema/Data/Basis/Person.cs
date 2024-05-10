using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Framework.Data;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore;
using IndexAttribute = System.Framework.Data.IndexAttribute;

<<<<<<< HEAD
namespace EmptyProject {
=======
namespace PHStatistics {
>>>>>>> origin/develop/schema
    /// <summary>
    /// 個人資料
    /// </summary>
    [Description("個人資料"), DataContract(IsReference = true)]
    public class Person : INaturalPersonData, IOperability {
        #region INaturalPersonData 成員

        #region IPersonData 成員

        #region IContactData 成員

        #region IEntityData 成員

        object IEntityData.Id { get { return Id; } }

        #endregion

        IAddressData IContactData.Address => Address;
        string IContactData.ShortName => null;
        string IContactData.Url => null;

        #endregion

        string IPersonData.Number => PersonalId;
        ICertificateData IPersonData.Certificate => null;

        #endregion

        string INaturalPersonData.Surname { get { return null; } }
        Gender? INaturalPersonData.Gender => null;
        ICollection<IRelatedPersonData> INaturalPersonData.Relationship => new List<IRelatedPersonData>().AsReadOnly();

        #endregion

        /// <summary>
        /// 識別碼
        /// </summary>
        [Display(Name = "識別碼"), DataMember]
        public Guid Id { get; set; }

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
        /// 姓名
        /// </summary>
        [Display(Name = "姓名"), DataMember]
        [MaxLength(32)]
        public string Name { get; set; }

        /// <summary>
        /// 暱稱
        /// </summary>
        [Display(Name = "暱稱"), DataMember]
        [MaxLength(32)]
        public string Nickname { get; set; }

        /// <summary>
        /// 照片
        /// </summary>
        [Display(Name = "照片"), DataMember]
        public Picture Photo { get; set; }

        /// <summary>
        /// 性別
        /// </summary>
        [Display(Name = "性別"), DataMember]
        public Sex? Sex { get; set; }

        /// <summary>
        /// 護照號碼
        /// </summary>
        [Display(Name = "護照號碼"), DataMember]
        [MaxLength(10), Unicode(false), Index]
        public string PersonalId { get; set; }

        /// <summary>
        /// 出生日期
        /// </summary>
        [Display(Name = "出生日期"), DataMember]
        public DateTime? BirthDate { get; set; }

        /// <summary>
        /// 電話號碼
        /// </summary>
        [Display(Name = "電話號碼"), DataMember]
        [MaxLength(32), Unicode(false)]
        public string Phone { get; set; }

        /// <summary>
        /// 傳真號碼
        /// </summary>
        [Display(Name = "傳真號碼"), DataMember]
        [MaxLength(32), Unicode(false)]
        public string Fax { get; set; }

        /// <summary>
        /// 行動電話
        /// </summary>
        [Display(Name = "行動電話"), DataMember]
        [MaxLength(32), Unicode(false)]
        public string MobilePhone { get; set; }

        /// <summary>
        /// Email
        /// </summary>
        [Display(Name = "Email"), DataMember]
        [MaxLength(320)]
        public string Email { get; set; }

        /// <summary>
        /// 地址
        /// </summary>
        [Display(Name = "地址"), DataMember]
        public Address Address { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        [Display(Name = "備註"), DataMember]
        [MaxLength(512)]
        public string Remark { get; set; }
    }
}
