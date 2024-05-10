// ReSharper disable CheckNamespace

using System.Framework;
using System.Framework.Data;
using EmptyProject;

/// <summary>
/// 測試 Admin API，須於部署後測試，或以不同執行環境或個體啟動專案與測試
/// </summary>
[TestFixture]
public class Admin {
    private readonly HttpClient _client = new();

    /// <summary>
    /// 啟動測試時時需啟用或建立的資源
    /// </summary>
    [SetUp]
    public void Setup() {
        _client.DefaultRequestHeaders.Clear();

        using var context = new DataContext();
        IUserData admin = context.User.Single(e => e.Account == "Admin");
        _client.DefaultRequestHeaders.Add("Authorization", $"Basic {admin.BuildBasicCredential()}");
    }
    
    [TestCase("https://localhost:44300")]
    public void ActionLogApi(string baseUri) {
        _client.BaseAddress = new Uri(baseUri);
        var response = _client.GetAsync("/api/ActionLog").WaitForResult();
        Assert.That(response.IsSuccessStatusCode, Is.True);
    }

    [TestCase("https://localhost:44300")]
    public void AttributeApi(string baseUri) {
        _client.BaseAddress = new Uri(baseUri);
        var response = _client.GetAsync("/api/Attribute").WaitForResult();
        Assert.That(response.IsSuccessStatusCode, Is.True);
    }

    [TestCase("https://localhost:44300")]
    public void BannerApi(string baseUri) {
        _client.BaseAddress = new Uri(baseUri);
        var response = _client.GetAsync("/api/Banner").WaitForResult();
        Assert.That(response.IsSuccessStatusCode, Is.True);
    }
    
    /// <summary>
    /// 結束測試時須拆除或終結的資源
    /// </summary>
    [TearDown]
    public void TearDown() => _client.Dispose();
}