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

    // PSJ/CKC 專用年級順序：11 個年級，不含「一年級」（Excel 北區資料列從未出現這個年級；
    // 南區資料列雖然有「一年級」，但 CKC 三科本身就未涵蓋這個年級，查不到時呼叫端會直接跳過該欄）
    public static readonly string[] PsjGradeOrder = {
        "二年級","三年級","四年級","五年級","六年級",
        "國一","國二","國三","高一","高二","高三"
    };

    public static readonly Dictionary<string, int[]> CkcCourseIds = new() {
        ["CKC_E"] = new[]{345,346,347,348,349,350,351,352,353,354,355},
        ["CKC_C"] = new[]{356,357,358,359,360,361,362,363,364,365,366},
        ["CKC_M"] = new[]{367,368,369,370,371,372,373,374,375,376,377},
    };

    // 南區右半頁籤分校名寫「高美」，但資料庫 School.Name 是「高美館」，精確比對會找不到分校
    // 而靜默漏掉整週資料——比照 AsChineseNumerals 的 fallback 慣例修正。
    public static readonly Dictionary<string, string> PsjSchoolNameAliases = new() {
        ["高美"] = "高美館",
    };

    // PH 全國人數表分校名用簡稱，資料庫 School.Name 是全名，精確比對會找不到分校而靜默漏掉整週資料。
    // 2026-07-30 比對第1/3週Excel發現，確認DB對應全名後修正。
    public static readonly Dictionary<string, string> PhSchoolNameAliases = new() {
        ["板忠"] = "板橋忠孝",
        ["板陽"] = "板橋陽明",
        ["農16"] = "農十六",
        ["天母"] = "天母忠誠",
    };

    public static readonly Dictionary<string, int[]> AsCourseIds = new() {
        ["AS"] = new[]{245,246,247,248,249,250,251,252,253,254,255,256},
        ["EP"] = new[]{295,296,297,298,299,300,301,302,303,305,306,307},
        ["EG"] = new[]{295,296,297,298,299,300,301,302,303,305,306,307},
        ["N"]  = new[]{271,272,273,274,275,276,277,278,279,280,281,282},
        ["L"]  = new[]{283,284,285,286,287,288,289,290,291,292,293,294},
        ["W"]  = new[]{259,260,261,262,263,264,265,266,267,268,269,270},
        ["MP"] = new[]{378,379,380,381,382,383,384,385,386,387,388,389},
        ["MG"] = new[]{378,379,380,381,382,383,384,385,386,387,388,389},
        ["SP"] = new[]{428,429,430,431,432,433,434,435,436,437,438,439},
        ["SG"] = new[]{428,429,430,431,432,433,434,435,436,437,438,439},
    };

    public static readonly Guid DefaultSubmitterId = Guid.Parse("23858D7E-F622-4D15-4A74-08DC7A5137DD");

    // 排除在 PH 匯入之外的頁籤名稱（英檢、舊版本、非人數表頁籤）
    public static readonly HashSet<string> PhExcludeSheets = new(StringComparer.OrdinalIgnoreCase) {
        "英檢", "舊版本", "南區 (舊版本)", "中北區（舊版本）", "Rocky班", "各校開班數"
    };

    // PH 南區: Excel column index → DB Course ID (fallback, 只在Name+Type比對失敗時使用)
    // 2026-07-30 用真正Excel合併儲存格逐欄核對115學年度第3週檔案重建，
    // 取代先前從col52(Elite區塊)開始整段錯位2欄的舊版（該版本會把高中小組班/英文個別指導/國語文個別指導的資料存到隔壁課程）。
    public static readonly Dictionary<int, int> PhColCourseIdNan = new() {
        [9]=1,  [10]=1,                                                    // P1-初階
        [11]=2, [12]=2, [13]=2,                                            // P2-先階
        [14]=3, [15]=3, [16]=3, [17]=3, [18]=3,                            // P3-中階
        [19]=4, [20]=4, [21]=4, [22]=4,                                    // P4-進階
        [23]=5, [24]=5, [25]=5,                                            // P5-高階
        [26]=6, [27]=6, [28]=6,                                            // P6- 優階（Excel標籤含空白，Name比對不到）
        [29]=7,                                                            // SAT Junior A
        [30]=8,                                                            // SAT Junior B
        [32]=10,[33]=10,[34]=10,[35]=10,[36]=10,                          // 國一準特/特訓
        [37]=11,[38]=11,[39]=11,[40]=11,[41]=11,[42]=11,                  // 國二準特/特訓
        [43]=12,[44]=12,[45]=12,[46]=12,[47]=12,                          // 國三準特/特訓
        [48]=13,[49]=14,[50]=15,                                           // 海外特訓班-TOEFL/SSAT/PSAT
        [52]=17,[53]=17,[54]=17,[55]=17,                                   // Elite班系,SAT,英檢班,國際班（合併4欄，非2欄）
        [56]=18,[57]=18,[58]=18,[59]=18,[60]=18,                           // 高中小組班-高一
        [61]=19,[62]=19,[63]=19,[64]=19,[65]=19,[66]=19,[67]=19,           // 高中小組班-高二
        [68]=20,[69]=20,[70]=20,[71]=20,                                   // 高中小組班-高三
        [74]=23,[75]=24,[76]=25,[77]=26,                                   // 英文個別指導(EM1)
        [79]=28,[80]=29,[81]=30,[82]=31,                                   // 英文合作開班1-4
        [84]=33,[85]=34,[86]=35,[87]=36,[88]=37,[89]=38,                   // 英文統計/分析
        [91]=39,[92]=39,[93]=39,[94]=39,[95]=39,                           // 國語文國小三力
        [96]=40,[97]=40,                                                   // 國語文國小中階
        [98]=41,[99]=41,[100]=41,                                          // 國語文國小攻略
        [101]=42,[102]=42,                                                 // 國語文國一班
        [103]=43,[104]=43,                                                 // 國語文國二班
        [105]=44,[106]=44,                                                 // 國語文國三班
        [107]=45,[108]=46,[109]=47,                                        // 國語文高一/二/三班
        [112]=50,[113]=51,[114]=52,[115]=53,                               // 國語文個別指導(EM1)
        [122]=60,[123]=61,                                                 // 本週/上週國語文總人數
        [126]=64,[127]=65,                                                 // 本週國語文新生/流失人數
    };

    // PH 中北區: Excel column index → DB Course ID（獨立版面，欄寬跟南區不同，不能沿用南區索引）
    // 2026-07-30 用真正Excel合併儲存格逐欄核對115學年度第3週中北區頁籤建立。
    public static readonly Dictionary<int, int> PhColCourseIdZhongBei = new() {
        [8]=1,  [9]=1,                                                     // P1-初階
        [10]=2, [11]=2, [12]=2,                                           // P2-先階
        [13]=3, [14]=3, [15]=3, [16]=3,                                   // P3-中階
        [17]=4, [18]=4, [19]=4, [20]=4, [21]=4, [22]=4,                   // P4-進階
        [23]=5, [24]=5, [25]=5, [26]=5,                                   // P5-高階
        [27]=6, [28]=6, [29]=6, [30]=6,                                    // P6- 優階（4欄，比南區多1欄）
        [31]=8,                                                            // SAT Junior B（中北區B欄在A欄之前）
        [32]=7,                                                            // SAT Junior A
        [34]=10,[35]=10,[36]=10,[37]=10,[38]=10,                          // 國一準特/特訓
        [39]=11,[40]=11,[41]=11,[42]=11,[43]=11,[44]=11,                  // 國二準特/特訓
        [45]=12,[46]=12,[47]=12,[48]=12,                                   // 國三準特/特訓
        [49]=13,[50]=14,[51]=15,                                           // 海外特訓班-TOEFL/SSAT/PSAT
        [53]=17,                                                           // Elite班系,SAT,國際班（只有1欄，比南區少）
        [54]=18,[55]=18,[56]=18,[57]=18,                                   // 高中小組班-高一
        [58]=19,[59]=19,[60]=19,[61]=19,                                   // 高中小組班-高二
        [62]=20,[63]=20,[64]=20,                                           // 高中小組班-高三
        [67]=23,[68]=24,[69]=25,[70]=26,                                   // 英文個別指導(EM1)
        [72]=28,[73]=29,[74]=30,[75]=31,                                   // 英文合作開班1-4
        [77]=33,[78]=34,[79]=35,[80]=36,[81]=37,[82]=38,                   // 英文統計/分析
        [84]=39,[85]=39,                                                   // 國語文國小三力
        [86]=40,[87]=40,[88]=40,                                           // 國語文國小中階
        [89]=41,[90]=41,[91]=41,                                           // 國語文國小攻略
        [92]=42,[93]=42,                                                   // 國語文國一班
        [94]=43,[95]=43,                                                   // 國語文國二班
        [96]=44,[97]=44,                                                   // 國語文國三班
        [98]=45,[99]=46,[100]=47,                                          // 國語文高一/二/三班
        [103]=50,[104]=51,[105]=52,[106]=53,                               // 國語文個別指導(EM1)
        [113]=60,[114]=61,                                                 // 本週/上週國語文總人數
        [117]=64,[118]=65,                                                 // 本週國語文新生/流失人數
    };

    // PH: 這幾欄的Excel內層表頭文字跟另一個部門的課程名稱完全撞名（「合作開班1-4」「與上週相比」「去年同期/比」
    // 在英文跟國語文區塊都用一樣的裸文字，差別只在於身處哪個區塊），Name+Type比對永遠會先命中英文課程（Id較小），
    // Type篩選解決不了同Type內部撞名。這裡強制覆蓋，優先權高於Name比對，確保國語文欄位存到正確課程。
    // 「與上週相比」「去年同期/比」目前風險較低（IsSum，StudentPopulationController.cs的chDiffItem/enDiffItem
    // 邏輯會依Department重新計算覆蓋），但既然整段重建，一併修正避免多一筆誤存的班級記錄。
    public static readonly Dictionary<int, int> PhColCourseIdForceNan = new() {
        [117]=55, [118]=56, [119]=57, [120]=58,                            // 國語文合作開班1-4
        [124]=62, [125]=63,                                                 // 國語文的與上週相比/去年同期比
    };

    public static readonly Dictionary<int, int> PhColCourseIdForceZhongBei = new() {
        [108]=55, [109]=56, [110]=57, [111]=58,                            // 國語文合作開班1-4
        [115]=62, [116]=63,                                                 // 國語文的與上週相比/去年同期比
    };

    // 依頁籤名稱挑選對應的欄位對照表；非南區/中北區(例如GEPT頁籤)沿用南區表作為預設，維持既有行為。
    public static Dictionary<int, int> GetPhColCourseIdFallback(string sheetName) =>
        sheetName == "中北區" ? PhColCourseIdZhongBei : PhColCourseIdNan;

    public static Dictionary<int, int> GetPhColCourseIdForce(string sheetName) =>
        sheetName == "中北區" ? PhColCourseIdForceZhongBei : PhColCourseIdForceNan;

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
        ["累積新生詢問"]=143,
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
