// ReSharper disable CheckNamespace

using System.Framework.Application;
using System.Framework.Logging;
using PHStatistics;
using PHStatistics.Actions;

/// <summary>
/// 測試生產模組
/// </summary>
[TestFixture]
public class ManufactureModule {
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
    /// <param name="name">商品名稱</param>
    [TestCase("NameTest")]
    public void TestProductActions(string name) {
        Assert.Multiple(() => {
            // 新增商品測試
            var newProduct = new ProductCreateAction(SystemUser.Default, _context).Create(new Product { Name = name });
            Assert.That(newProduct.Number, Is.Not.Null); // 須產生產品編號

            // 讀取商品測試
            var foundProduct = new ProductReadAction(SystemUser.Default, _context, true).Find(newProduct.Id);
            Assert.That(foundProduct, Is.Not.Null);

            // 修改商品測試
            foundProduct.Name = "FoundProduct";
            new ProductUpdateAction(SystemUser.Default, _context).Update(newProduct);
            Assert.That(newProduct.Name, Is.EqualTo(foundProduct.Name));

            //刪除商品測試
            new ProductDeleteAction(SystemUser.Default, _context).Delete(foundProduct);
            foundProduct = new ProductReadAction(SystemUser.Default, _context).Find(newProduct.Id);
            Assert.That(foundProduct, Is.Null);
        });
    }

    /// <summary>
    /// 結束測試時須拆除或終結的資源
    /// </summary>
    [TearDown]
    public void TearDown() => _context.Dispose();
}