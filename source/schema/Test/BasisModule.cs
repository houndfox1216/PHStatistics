// ReSharper disable CheckNamespace

using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.Logging;
using System.Framework.Security;
using PHStatistics;
using PHStatistics.Actions;

/// <summary>
/// 測試基礎模組
/// </summary>
[TestFixture]
public class BasisModule {
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
    /// 測試商品相關Actions
    /// </summary>
    /// <param name="name">名稱</param>
    /// <param name="account">帳號</param>
    /// <param name="password">密碼</param>
    [TestCase("NameTest", "AccountTest", "PasswordTest")]
    public void TestUserActions(string name, string account, string password) => Assert.Multiple(() => {
        if (_context.User.Any(e => e.DataMode == DataMode.Debug && e.Account == account)) {
            _context.Remove(_context.User.Single(e => e.DataMode == DataMode.Debug && e.Account == account));
            _context.SaveChanges();
        }

        // 新增用戶測試
        var newUser = new UserCreateAction(SystemUser.Default, _context).Create(
            new User {
                DataMode = DataMode.Debug, Name = name, Account = account, Password = password
            }
        );
        Assert.That(newUser.Password, Is.EqualTo(password.ComputeHashStringWithSha().ToBase64())); // 密碼需經過 SHA 256 Hash

        // 讀取用戶測試
        var foundUser = new UserReadAction(SystemUser.Default, _context, true).Find(newUser.Id);
        Assert.That(foundUser, Is.Not.Null);

        // 修改用戶測試
        foundUser.Name = "FoundUser";
        new UserUpdateAction(SystemUser.Default, _context).Update(newUser);
        Assert.That(newUser.Name, Is.EqualTo(foundUser.Name));

        //刪除用戶測試
        new UserDeleteAction(SystemUser.Default, _context).Delete(foundUser);
        foundUser = new UserReadAction(SystemUser.Default, _context).Find(newUser.Id);
        Assert.That(foundUser, Is.Null);
    });

    /// <summary>
    /// 結束測試時須拆除或終結的資源
    /// </summary>
    [TearDown]
    public void TearDown() => _context.Dispose();
}