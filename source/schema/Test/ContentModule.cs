// ReSharper disable CheckNamespace

using System.Framework;
using System.Framework.Application;
using System.Framework.Logging;
using PHStatistics;
using PHStatistics.Actions;

/// <summary>
/// 測試內容模組
/// </summary>
[TestFixture]
public class ContentModule {
    private TestDataContext _context;

    /// <summary>
    /// 啟動測試時時需啟用或建立的資源
    /// </summary>
    [SetUp]
    public void Setup() {
        _context = new TestDataContext();
        _context.InitializeData();
        LogManager.SetContext<VoidLogContext>();
    }

    /// <summary>
    /// 測試新聞相關Actions
    /// </summary>
    [Test]
    public void TestNewsActions() => Assert.Multiple(() => {
        var uuid = ShortUid.NewId;
        var title = new MultilingualText {
            DefaultText = uuid,
            Texts = new List<StringResource>(new StringResource[] { new() { Content = uuid } })
        };
        var content = new MultilingualText {
            DefaultText = uuid,
            Texts = new List<StringResource>(new StringResource[] { new() { Content = uuid } })
        };
        // 新增新聞測試
        var newNews = new NewsCreateAction(SystemUser.Default, _context).Create(new News { Title = title, Content = content });
        Assert.That(newNews.Title.DefaultText, Is.EqualTo(uuid));

        // 讀取新聞測試
        var foundNews = new NewsReadAction(SystemUser.Default, _context, true).Find(newNews.Id);
        Assert.That(foundNews, Is.Not.Null);

        // 修改新聞測試
        foundNews.Title.DefaultText = "FoundNews";
        foundNews.Title.Texts.First().Content = "FoundNews";
        new NewsUpdateAction(SystemUser.Default, _context).Update(newNews);
        Assert.That(newNews.Title.DefaultText, Is.EqualTo(foundNews.Title.Texts.First().Content));

        //刪除新聞測試
        new NewsDeleteAction(SystemUser.Default, _context).Delete(foundNews);
        foundNews = new NewsReadAction(SystemUser.Default, _context).Find(newNews.Id);
        Assert.That(foundNews, Is.Null); // 確認新聞已被刪除
        Assert.That(_context.MultilingualText.Find(title.Id), Is.Null); // 確認多國語系已刪除
        Assert.That(_context.StringResource.Find(content.Texts.First().Id), Is.Null); // 確認文字資源已刪除
    });

    /// <summary>
    /// 結束測試時須拆除或終結的資源
    /// </summary>
    [TearDown]
    public void TearDown() => _context.Dispose();
}