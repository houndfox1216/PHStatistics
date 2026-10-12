using System.Reflection;
using System.Xml.Linq;
using NUnit.Framework;
using PHStatistics.Audit;

namespace PHStatistics.Portal.Test.Services;

// 河堤備註含 U+000B 會讓稽核攔截器組 XML 丟例外，進而中斷建立人數表（合計列沒建出來）。
[TestFixture]
public class AuditXmlSafeTests {

    private static string XmlSafe(string value) {
        var method = typeof(AuditSaveChangesInterceptor).GetMethod("XmlSafe", BindingFlags.NonPublic | BindingFlags.Static);
        return (string)method.Invoke(null, new object[] { value });
    }

    [Test]
    public void ControlCharacter_IsRemoved_AndXmlSerializes() {
        var cleaned = XmlSafe("曾郁綾\u000B曾郁綺\u000B鄭宇軒");
        Assert.That(cleaned, Is.EqualTo("曾郁綾曾郁綺鄭宇軒"));
        Assert.DoesNotThrow(() => new XElement("Changes", new XElement("Field", new XAttribute("New", cleaned))).ToString(SaveOptions.DisableFormatting));
    }

    [Test]
    public void RawControlCharacter_ThrowsWithoutSanitizing() {
        // 佐證根因：未清理的 U+000B 序列化會丟例外
        Assert.Throws<System.ArgumentException>(() =>
            new XElement("Changes", new XElement("Field", new XAttribute("New", "a\u000Bb"))).ToString(SaveOptions.DisableFormatting));
    }

    [Test]
    public void NormalText_TabNewlineAndSurrogatePairs_AreKept() {
        Assert.That(XmlSafe("a\tb\r\nc😀"), Is.EqualTo("a\tb\r\nc😀"));
    }

    [Test]
    public void NullOrEmpty_ReturnsEmpty() {
        Assert.That(XmlSafe(null), Is.EqualTo(""));
        Assert.That(XmlSafe(""), Is.EqualTo(""));
    }
}
