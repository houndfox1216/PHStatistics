using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Framework.Data;
using System.Runtime.Serialization;

namespace EmptyProject {
    /// <summary>
    /// 類別資訊
    /// </summary>
    public class Category : ICategoryData, ISortable {
        #region ICategoryData 成員

        #region IEntityData 成員

        object IEntityData.Id => Id;
        string IEntityData.Name =>  this.GetDefaultName<Category>();

        #endregion

        ICategoryData ICategoryData.Parent => Parent;
        ICollection<ICategoryData> ICategoryData.Children => new List<ICategoryData>(Children ?? Array.Empty<Category>());

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
        /// 名稱
        /// </summary>
        [Display(Name = "名稱"), DataMember]
        [MaxLength(32)]
        public string Name { get; set; }

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
        /// 父類別外鍵
        /// </summary>
        public int? ParentId { get; set; }

        /// <summary>
        /// 父類別
        /// </summary>
        [Display(Name = "父類別"), DataMember]
        public Category Parent { get; set; }

        /// <summary>
        /// 擁有子類別
        /// </summary>
        [Display(Name = "擁有子類別"), DataMember]
        public bool HasChild { get; set; }

        /// <summary>
        /// 子類別
        /// </summary>
        [Display(Name = "子類別"), DataMember]
        public ICollection<Category> Children { get; set; }

        /// <summary>
        /// 圖片
        /// </summary>
        [Display(Name = "圖片"), DataMember]
        public Picture Picture { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        [Display(Name = "備註"), DataMember]
        [MaxLength(512)]
        public string Remark { get; set; }
    }
}
