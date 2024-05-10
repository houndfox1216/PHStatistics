// ReSharper disable CheckNamespace

using System.Diagnostics.CodeAnalysis;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

/// <summary>
/// 測試頁面
/// </summary>
[TestFixture]
public class Page {
    private IWebDriver _chrome;

    /// <summary>
    /// 啟動測試時時需啟用或建立的資源
    /// </summary>
    [SetUp]
    public void Setup() {
        _chrome = new ChromeDriver();
    }

    /// <summary>
    /// 結束測試時須拆除或終結的資源
    /// </summary>
    [TearDown]
    public void TearDown() {
        _chrome.Close();
    }

    /// <summary>
    /// 測試首頁是否正常呈現
    /// </summary>
    [Test]
    [SuppressMessage("ReSharper", "UnusedVariable")]
    public void Home() {
        var url = "https://localhost:5001";
        _chrome.Navigate().GoToUrl(url);
        var wait = new WebDriverWait(_chrome, TimeSpan.FromSeconds(15));
        var element = wait.Until(chrome => chrome.FindElement(By.XPath("//a[@class='logo-wrap']")));
        var @class = element.GetAttribute("class");
        var href = element.GetDomAttribute("href");
        //element.Click();
        var source = _chrome.PageSource;
        var title = _chrome.Title;
        Assert.That(title, Is.EqualTo("EmptyProject - 首頁"));
    }
}
