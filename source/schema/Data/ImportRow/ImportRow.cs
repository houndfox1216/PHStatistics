using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using PHStatistics.Community;

namespace PHStatistics.ImportRow {
    /// <summary>
    /// 來料檢驗
    /// </summary>
    [Serializable]
    [XmlRootAttribute("ImportRow", Namespace = "http://cloudfun.com.tw", IsNullable = false)]
    public class ImportRow {

        public ImportRow() { }

        /// <summary>
        /// 行數
        /// </summary>
        [Display(Name = "行數")]
        public int RowNo { get; set; }

        /// <summary>
        /// 欄數
        /// </summary>
        [Display(Name = "欄數")]
        public int CellsNo { get; set; }

        /// <summary>
        /// 內容
        /// </summary>
        [Display(Name = "內容")]
        public string CellsContent { get; set; }
    }
}
