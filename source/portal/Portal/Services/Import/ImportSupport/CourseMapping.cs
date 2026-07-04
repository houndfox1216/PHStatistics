using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NPOI.SS.UserModel;
using PHStatistics.Content;

namespace PHStatistics.Portal.Services.Import.ImportSupport;

public static class CourseMapping {
    public static readonly string[] GradeOrder = {
        "一年級","二年級","三年級","四年級","五年級","六年級",
        "國一","國二","國三","高一","高二","高三"
    };

    public static readonly Dictionary<string, int[]> PsjCourseIds = new() {
        ["MP"] = new[]{145,146,147,148,149,150,151,152,153,154,155,156},
        ["MS"] = new[]{145,146,147,148,149,150,151,152,153,154,155,156},
        ["SP"] = new[]{195,196,197,198,199,200,201,202,203,204,205,206},
        ["SS"] = new[]{195,196,197,198,199,200,201,202,203,204,205,206},
        ["N"]  = new[]{171,172,173,174,175,176,177,178,179,180,181,182},
        ["L"]  = new[]{183,184,185,186,187,188,189,190,191,192,193,194},
        ["W"]  = new[]{159,160,161,162,163,164,165,166,167,168,169,170},
    };

    public static readonly Dictionary<string, int[]> AsCourseIds = new() {
        ["AS"] = new[]{245,246,247,248,249,250,251,252,253,254,255,256},
        ["EP"] = new[]{295,296,297,298,299,300,301,302,303,305,306,307},
        ["EG"] = new[]{295,296,297,298,299,300,301,302,303,305,306,307},
        ["N"]  = new[]{271,272,273,274,275,276,277,278,279,280,281,282},
        ["L"]  = new[]{283,284,285,286,287,288,289,290,291,292,293,294},
        ["W"]  = new[]{259,260,261,262,263,264,265,266,267,268,269,270},
    };

    public static readonly Guid DefaultSubmitterId = Guid.Parse("23858D7E-F622-4D15-4A74-08DC7A5137DD");

    // 排除在 PH 匯入之外的頁籤名稱（英檢、舊版本、非人數表頁籤）
    public static readonly HashSet<string> PhExcludeSheets = new(StringComparer.OrdinalIgnoreCase) {
        "英檢", "舊版本", "南區 (舊版本)", "中北區（舊版本）", "Rocky班", "各校開班數"
    };

    // PH: Excel column index → DB Course ID (hardcoded from 百瀚英語南區 multi-level header layout)
    public static readonly Dictionary<int, int> PhColCourseId = new() {
        [9]=1,  [10]=1,                                                    // P1-初階
        [11]=2, [12]=2, [13]=2,                                            // P2-先階
        [14]=3, [15]=3, [16]=3, [17]=3, [18]=3,                            // P3-中階
        [19]=4, [20]=4, [21]=4, [22]=4,                                    // P4-進階
        [23]=5, [24]=5, [25]=5,                                            // P5-高階
        [26]=6, [27]=6, [28]=6,                                            // P6-優階
        [29]=7, [30]=8,                                                    // SAT Junior A/B
        [32]=10,[33]=10,[34]=10,[35]=10,[36]=10,                           // 國一準特/特訓
        [37]=11,[38]=11,[39]=11,[40]=11,[41]=11,[42]=11,                   // 國二準特/特訓
        [43]=12,[44]=12,[45]=12,[46]=12,[47]=12,                           // 國三準特/特訓
        [48]=13,[49]=14,[50]=15,                                           // 海外特訓班-TOEFL/SSAT/PSAT
        [52]=17,[53]=17,                                                   // Elite/英檢/sat班系
        [54]=18,[55]=18,[56]=18,[57]=18,[58]=18,                           // 高中小組班-高一
        [59]=19,[60]=19,[61]=19,[62]=19,[63]=19,[64]=19,[65]=19,           // 高中小組班-高二
        [66]=20,[67]=20,[68]=20,[69]=20,                                   // 高中小組班-高三
        [72]=23,[73]=24,[74]=25,[75]=26,                                   // 英文個別指導
        [77]=28,[78]=29,[79]=30,[80]=31,                                   // 英文合作開班1-4
        [89]=39,[90]=39,[91]=39,[92]=39,[93]=39,                           // 國語文國小三力
        [94]=40,[95]=40,                                                   // 國語文國小中階
        [96]=41,[97]=41,[98]=41,                                           // 國語文國小攻略
        [99]=42,[100]=42,                                                  // 國語文國一班
        [101]=43,[102]=43,                                                 // 國語文國二班
        [103]=44,[104]=44,                                                 // 國語文國三班
        [105]=45,[106]=46,[107]=47,                                        // 國語文高一/二/三班
        [110]=50,[111]=51,[112]=52,[113]=53,                               // 國語文個別指導
        [115]=55,[116]=56,[117]=57,[118]=58,                               // 國語文合作開班
    };

    // PS: Excel column header → DB Course ID (aliases for multi-variant class names)
    public static readonly Dictionary<string, int> PsHeaderCourseId = new(StringComparer.OrdinalIgnoreCase) {
        ["一資"]=108, ["一特"]=109,
        ["二資"]=110, ["二特"]=111, ["二PS特"]=111, ["二特2"]=111,
        ["三資"]=112, ["三特"]=113, ["三特Ps"]=113, ["三特1"]=113, ["三特2"]=113,
        ["四資"]=114, ["四特"]=115, ["四ps特"]=115, ["四P特"]=115, ["四S特"]=115, ["四特1"]=115, ["四特2"]=115,
        ["五資"]=116, ["五特"]=117, ["五P特"]=117, ["五S特"]=117, ["五資1"]=116, ["五特2"]=117,
        ["六資"]=118, ["六特"]=119, ["六P特"]=119, ["六資1"]=118, ["六資2"]=118, ["六特1"]=119, ["六特2"]=119,
        ["七資"]=121, ["七特"]=122, ["七PS特"]=122,
        ["八資"]=123, ["八特"]=124, ["八特1"]=124, ["八特2"]=124, ["八特3"]=124, ["八課內"]=124,
        ["九資"]=125, ["九特"]=126, ["九資1"]=125, ["九資2"]=125,
        ["高一特"]=128, ["高二特"]=129, ["高三特"]=130,
    };

    // CourseIds that use EM1 logic: count = # individual students, each gets its own class record of 1
    public static readonly HashSet<int> Em1CourseIds = new() { 23, 24, 25, 26, 50, 51, 52, 53 };

    public static ClassType PsjColumnType(string code) => code switch {
        "MP" or "SP" => ClassType.Personal,
        "MS" or "SS" => ClassType.SubGroup,
        _ => ClassType.General,
    };

    public static ClassType AsColumnType(string code) => code switch {
        "EP" or "MP" or "SP" => ClassType.Personal,
        "ES" or "MS" or "SS" => ClassType.SubGroup,
        _ => ClassType.General,
    };

    public static int ReadCellNumber(IRow row, int col) {
        var cell = row.GetCell(col);
        if (cell == null) return 0;
        try {
            if (cell.CellType == CellType.Formula) {
                cell.SetCellType(CellType.Numeric);
                return (int)Math.Round(cell.NumericCellValue, MidpointRounding.AwayFromZero);
            }
            return int.TryParse(cell.ToString().Trim(), out int v) ? v : 0;
        }
        catch {
            return 0;
        }
    }

    public static string AsChineseNumerals(string s) {
        static string Convert(int n) {
            string[] d = { "", "一", "二", "三", "四", "五", "六", "七", "八", "九" };
            if (n < 10) return d[n];
            if (n < 20) return "十" + (n % 10 == 0 ? "" : d[n % 10]);
            return d[n / 10] + "十" + (n % 10 == 0 ? "" : d[n % 10]);
        }
        return Regex.Replace(s, @"\d+", m =>
            int.TryParse(m.Value, out int n) && n >= 1 && n <= 99 ? Convert(n) : m.Value);
    }
}
