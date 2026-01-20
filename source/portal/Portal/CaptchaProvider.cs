using System;
using System.Framework.Security;
using System.Linq;
using SixLabors.Fonts;

namespace PHStatistics.Portal;

/// <summary>
/// 驗證碼提供者
/// </summary>
public static class CaptchaProvider {
    private static readonly CaptchaFactory Factory;

    static CaptchaProvider() {
        var installedFonts = SystemFonts.Families.ToList();
        var fontFamilies = installedFonts.Where(e => new[] { "Arial Black", "Verdana", "Helvetica" }.Contains(e.Name)).ToArray(); // Microsoft Fonts
        if (fontFamilies.Length == 0)
            fontFamilies = installedFonts.Where(e => new[] { "DejaVu Serif", "DejaVu Sans Mono", "DejaVu Sans" }.Contains(e.Name)).ToArray(); // DejaVu Fonts
        // ReSharper disable StringLiteralTypo
        Factory = new CaptchaFactory(
            TimeSpan.FromMinutes(3), fontFamilies, null, null, "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToArray(), 20, 50
        );
        // ReSharper restore StringLiteralTypo
    }

    /// <summary>
    /// 取得驗證碼
    /// </summary>
    /// <param name="length">驗證碼長度</param>
    /// <param name="maxFontSize">最大字型尺寸</param>
    /// <param name="minFontSize">最小字型尺寸</param>
    /// <param name="token">令牌，可自行指定或由系統產生長度256以內的字串</param>
    /// <returns>驗證碼</returns>
    public static Captcha GetCaptcha(int length, float maxFontSize, float minFontSize = 12, string token = null) =>
        Factory.GetCaptcha(length, maxFontSize, minFontSize, token);

    /// <summary>
    /// 取得驗證碼
    /// </summary>
    /// <param name="length">產生的驗證碼長度</param>
    /// <param name="token">檢驗令牌，檢查時需提供</param>
    /// <returns></returns>
    public static Captcha GetCaptcha(int length, string token) => Factory.GetCaptcha(length, token);

    /// <summary>
    /// 檢查驗證碼
    /// </summary>
    /// <param name="token">檢驗令牌，需與取得驗證碼時相同</param>
    /// <param name="captchaCode">用戶輸入的驗證碼</param>
    /// <returns></returns>
    public static bool Check(string token, string captchaCode) => Factory.Check(token, captchaCode);
}