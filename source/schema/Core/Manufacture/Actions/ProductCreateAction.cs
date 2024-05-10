using System.Collections.Generic;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Text;

namespace EmptyProject.Actions;

/// <summary>
/// 新增商品資料之操作。
/// </summary>
[Description("新增商品資料")]
public class ProductCreateAction : CreateActionBase<Product, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.Product };

    /// <summary>
    /// 建構 ProductCreateAction
    /// </summary>
    /// <param name="user">請求操作的會員</param>
    public ProductCreateAction(IUser user, DataContext dbContext = null) : base("新增商品資料", user, dbContext) { 
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext); 
        }

    protected override void OnTransacting(DataContext context, Product data) {
            data.Picture ??= new Picture();
            data.Number ??= new SequenceGenerateCommand<Sequence>(context).Generate(EntityType.Product);
            data.Picture ??= new Picture();
            data.Album ??= new Album();
            data.Album.Number = new SequenceGenerateCommand<Sequence>(context).Generate(EntityType.PictureGallery, null, null, "{3:yyMM}{4:D4}", SequenceResetType.Monthly);
            
            data.Description ??= new StringResource { Content = "" };
            if (data.Description.Content == null) data.Description.Content = "";
        }

    protected override void OnCreating(DataContext context, Product data) {
        data.Base = null;
        if (data.BaseId.HasValue()) { // 檢查 Base 是否產生循環參考
            var @base = context.Product.Find(data.BaseId);
            var path = new List<int>(new[] { @base.Id });
            while (@base.BaseId.HasValue()) {
                @base = context.Product.Find(@base.BaseId);
                if (path.Contains(@base.Id)) throw new OperationException("無法設置此商品為基本款，因產生循環");
                path.Add(@base.Id);
            }
        }

        #region Create Product Attribute Value 

        var removableAttributeValues = new HashSet<ProductAttributeValue>();
        data.ProductAttributeValues ??= new List<ProductAttributeValue>();
        foreach (var item in data.ProductAttributeValues) {
            item.Product = data;
            if (!item.AttributeValue.HasValue()) {
                item.AttributeValue.TextValue ??= item.AttributeValue.Value;
                if (!item.AttributeValue.DecimalValue.HasValue && decimal.TryParse(item.AttributeValue.Value, out decimal decimalValue)) {
                    item.AttributeValue.DecimalValue = decimalValue;
                }
            }
            if (item.AttributeValueId.HasValue()) item.AttributeValue = null;
            else if (item.AttributeValue is AttributeValue attributeValue && !attributeValue.Value.HasValue()) {
                item.AttributeValue.Attribute = context.Attribute.Find(item.AttributeValue.AttributeId);
                removableAttributeValues.Add(item);
            }
        }

        var errorMessage = new StringBuilder();
        foreach (var item in removableAttributeValues) {
            if (item.AttributeValue.Attribute.Required) {
                if (errorMessage.Length > 0) errorMessage.Append('、');
                errorMessage.Append(item.AttributeValue.Attribute.Name);
            }
            data.ProductAttributeValues.Remove(item);
        }
        if (errorMessage.Length > 0) {
            errorMessage.Append("為必填屬性");
            throw new OperationException(errorMessage.ToString());
        }

        #endregion

        if (data.Categories != null && data.Categories.Any()) {
            var productCategories = new HashSet<ProductCategory>();
            foreach (var categoryId in data.CategoryIds) productCategories.Add(new ProductCategory { Product = data, CategoryId = categoryId });
            data.ProductCategories = productCategories;
        }

        if (data.Tags != null && data.Tags.Any()) {
            var productTags = new HashSet<ProductTag>();
            foreach (var tagId in data.TagIds) productTags.Add(new ProductTag { Product = data, TagId = tagId });
            data.ProductTags = productTags;
        }
    }
}