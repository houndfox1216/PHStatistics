using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using System.Framework.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PHStatistics.Content {
    /// <summary>
    /// 課程資料
    /// </summary>
    [Description("課程資料"), DataContract(IsReference = true)]
    public class Course : IEntityData, IOperability {
        #region IEntityData 成員

        object IEntityData.Id { get { return Id; } }
        string IEntityData.Name { get { return this.Name; } }

        #endregion

        #region IOperability 成員
        /// <summary>
        /// 建立時間
        /// </summary>
        [Display(Name = "建立時間")]
        public DateTime? CreatedTime { get; set; }

        /// <summary>
        /// 更新時間
        /// </summary>
        [Display(Name = "更新時間")]
        public DateTime? UpdatedTime { get; set; }

        /// <summary>
        /// 資料模式
        /// </summary>
        [Display(Name = "資料模式")]
        public DataMode DataMode { get; set; }
        #endregion

        /// <summary>
        /// 識別碼
        /// </summary>
        [Display(Name = "識別碼"), DataMember]
        public int Id { get; set; }

        /// <summary>
        /// 名稱
        /// </summary>
        [Display(Name = "名稱"), DataMember]
        [MaxLength(128)]
        public string Name { get; set; }

        /// <summary>
        /// 班系資料識別碼
        /// </summary>
        [Display(Name = "班系資料識別碼"), DataMember]
        public int? DepartmentId { get; set; }

        /// <summary>
        /// 班系
        /// </summary>
        [Display(Name = "班系"), DataMember]
        public CourseDepartment Department { get; set; }

        /// <summary>
        /// 加總
        /// </summary>
        [Display(Name = "加總"), DataMember]
        public bool IsSum { get; set; }

        /// <summary>
        /// 排列順序
        /// </summary>
        [Display(Name = "排列順序"), DataMember]
        public int Ordinal { get; set; }

        /// <summary>
        /// 所屬單位
        /// </summary>
        [Display(Name = "所屬單位"), DefaultValue(StudentPopulationType.PH), DataMember]
        public StudentPopulationType Type { get; set; }

        /// <summary>
        /// 顯示名稱（名稱＋所屬單位＋班系，供下拉選單/介面顯示避免同名課程混淆——同單位下常有多個班系用同樣的年級/課程名稱）
        /// </summary>
        [Display(Name = "顯示名稱"), DataMember]
        [NotMapped]
        public string DisplayName => string.IsNullOrEmpty(Department?.Name)
            ? $"{Name}［{Type.GetDisplayName()}］"
            : $"{Name}［{Type.GetDisplayName()}-{Department.Name}］";

        /// <summary>
        /// 適用班別
        /// </summary>
        [Display(Name = "適用班別"), DataMember]
        [MaxLength(128)]
        public string ClassType { get; set; }

        /// <summary>
        /// 已發佈
        /// </summary>
        [Display(Name = "已發佈"), DataMember]
        public bool Published { get; set; }

        /// <summary>
        /// 統計計算類型
        /// </summary>
        [Display(Name = "統計計算類型"), DataMember]
        public StatisticsType? StatisticsType { get; set; }

        /// <summary>
        /// 統計來源班系Id，JSON陣列格式
        /// </summary>
        [Display(Name = "統計來源班系Id"), DataMember]
        [MaxLength(500)]
        public string SourceDepartmentIds { get; set; }

        /// <summary>
        /// 統計來源課程Id，JSON陣列格式
        /// </summary>
        [Display(Name = "統計來源課程Id"), DataMember]
        [MaxLength(500)]
        public string SourceCourseIds { get; set; }

        /// <summary>
        /// 統計來源班系Id（供介面以勾選標籤方式編輯，實際仍儲存於 SourceDepartmentIds）
        /// </summary>
        [Display(Name = "統計來源班系"), DataMember]
        [NotMapped]
        public int[] SourceDepartmentIdValues {
            get {
                if (string.IsNullOrEmpty(SourceDepartmentIds)) return Array.Empty<int>();
                try { return JsonConvert.DeserializeObject<int[]>(SourceDepartmentIds) ?? Array.Empty<int>(); }
                catch { return Array.Empty<int>(); }
            }
            set => SourceDepartmentIds = (value == null || value.Length == 0) ? null : JsonConvert.SerializeObject(value);
        }

        /// <summary>
        /// 統計來源課程Id（供介面以勾選標籤方式編輯，實際仍儲存於 SourceCourseIds）
        /// </summary>
        [Display(Name = "統計來源課程"), DataMember]
        [NotMapped]
        public int[] SourceCourseIdValues {
            get {
                if (string.IsNullOrEmpty(SourceCourseIds)) return Array.Empty<int>();
                try { return JsonConvert.DeserializeObject<int[]>(SourceCourseIds) ?? Array.Empty<int>(); }
                catch { return Array.Empty<int>(); }
            }
            set => SourceCourseIds = (value == null || value.Length == 0) ? null : JsonConvert.SerializeObject(value);
        }

        /// <summary>
        /// 統計扣除來源課程Id，JSON陣列格式
        /// </summary>
        [Display(Name = "統計扣除來源課程Id"), DataMember]
        [MaxLength(500)]
        public string NegativeSourceCourseIds { get; set; }

        /// <summary>
        /// 統計扣除來源課程Id（供介面以勾選標籤方式編輯，實際仍儲存於 NegativeSourceCourseIds）
        /// </summary>
        [Display(Name = "扣除來源課程"), DataMember]
        [NotMapped]
        public int[] NegativeSourceCourseIdValues {
            get {
                if (string.IsNullOrEmpty(NegativeSourceCourseIds)) return Array.Empty<int>();
                try { return JsonConvert.DeserializeObject<int[]>(NegativeSourceCourseIds) ?? Array.Empty<int>(); }
                catch { return Array.Empty<int>(); }
            }
            set => NegativeSourceCourseIds = (value == null || value.Length == 0) ? null : JsonConvert.SerializeObject(value);
        }

        /// <summary>
        /// 是否依班別分組計算
        /// </summary>
        [Display(Name = "是否依班別分組計算"), DataMember]
        public bool GroupByClassType { get; set; }

        /// <summary>
        /// 適用班別
        /// </summary>
        [Display(Name = "適用班別"), DataMember]
        public ClassType? ApplicableClassType { get; set; }

        /// <summary>
        /// 統計來源學科
        /// </summary>
        [Display(Name = "統計來源學科"), DataMember]
        public CourseSubject? SourceSubject { get; set; }

        /// <summary>
        /// 統計來源報表類型（跨Type抓值專用，例如PS抓PSJ當週/上週資料）
        /// </summary>
        [Display(Name = "統計來源報表類型"), DataMember]
        public StudentPopulationType? SourceStudentPopulationType { get; set; }

    }
}
