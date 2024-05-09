using System.Collections.Generic;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace EmptyProject.Actions;

/// <summary>
/// 更新商品資料之操作。
/// </summary>
[Description("更新商品資料")]
public class ProductUpdateAction : UpdateActionBase<Product, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.Product };

    /// <summary>
    /// 建構 ProductCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public ProductUpdateAction(IUser user, DataContext dbContext = null) : base("更新商品資料", user, dbContext) {
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext);
            RequiredIncludes = new[] { 
                "Picture", 
                "ProductAttributeValues.AttributeValue.Attribute", 
                "ProductCategories.Category", 
                "ProductTags", 
                "PackedContents.Content", 
                "Album.Pictures", 
                "Description" 
            };
        }

    protected override void OnUpdating(DataContext context, Product data, Product current) {
        if (data.Picture.HasValue()) current.Picture.Uri = data.Picture.Uri;

        #region Update Categories

        if (data.CategoryIds != null) {
            var removedProductCategories = current.ProductCategories.Where(e => !data.CategoryIds.Contains(e.CategoryId));
            foreach (var reference in removedProductCategories) current.ProductCategories.Remove(reference);
            var addedProductCategories = data.CategoryIds.Except(current.CategoryIds);
            foreach (var reference in addedProductCategories) current.ProductCategories.Add(new ProductCategory { ProductId = current.Id, CategoryId = reference });
        }

        #endregion

        #region Update Tags

        if (data.TagIds != null) {
            var removedProductTags = current.ProductTags.Where(e => !data.TagIds.Contains(e.TagId));
            foreach (var reference in removedProductTags) current.ProductTags.Remove(reference);
            var addedProductTags = data.TagIds.Except(current.TagIds);
            foreach (var reference in addedProductTags) current.ProductTags.Add(new ProductTag { ProductId = current.Id, TagId = reference });
        }

        #endregion

        #region Update Attribute Values

        if (data.ProductAttributeValues != null) {
            var removableAttributeValues = new HashSet<ProductAttributeValue>();
            foreach (var item in current.ProductAttributeValues) {
                if (!data.ProductAttributeValues.Any(e => e.AttributeValueId == item.AttributeValueId)) removableAttributeValues.Add(item);
            }
            foreach (var item in data.ProductAttributeValues.Where(e => !e.AttributeValue.Value.HasValue()).ToList()) {
                var attribute = context.Attribute.Find(item.AttributeValue.AttributeId);
                if (attribute.Selectable) continue;
                data.ProductAttributeValues.Remove(item);
                if (item.AttributeValueId.HasValue()) removableAttributeValues.Add(item);
            }

            foreach (var item in data.ProductAttributeValues) {
                item.ProductId = current.Id;
                item.Product = null;
                var attribute = context.Attribute.Find(item.AttributeValue.AttributeId);
                if (attribute.Selectable) {
                    var reference = current.ProductAttributeValues.FirstOrDefault(e => e.AttributeValueId == item.AttributeValueId);
                    if (reference == null && item.AttributeValueId.HasValue()) {
                        item.AttributeValue = null;
                        context.Add(item); // create reference
                    }
                } else {
                    item.AttributeValue.TextValue ??= item.AttributeValue.Value;
                    if (!item.AttributeValue.DecimalValue.HasValue && decimal.TryParse(item.AttributeValue.Value, out decimal decimalValue)) {
                        item.AttributeValue.DecimalValue = decimalValue;
                    }
                    if (!item.AttributeValueId.HasValue()) context.ProductAttributeValue.Add(item); // create value
                    else if (!current.ProductAttributeValues.Any(e => e.AttributeValueId == item.AttributeValueId)) { // create reference
                        item.AttributeValue = null;
                        context.ProductAttributeValue.Add(item);
                    } else { // update value
                        var currentValue = context.AttributeValue.Include("Attribute").SingleOrDefault(e => e.Id == item.AttributeValueId);
                        context.Entry(currentValue).CurrentValues.SetValues(item.AttributeValue);
                    }
                }
            }

            var errorMessage = new StringBuilder();
            foreach (var item in removableAttributeValues) {
                item.AttributeValue = context.AttributeValue.Include("Attribute").SingleOrDefault(e => e.Id == item.AttributeValueId);
                var attribute = item.AttributeValue.Attribute;
                if (attribute.Required) {
                    if (attribute.Selectable) {
                        if (!item.AttributeValueId.HasValue()) {
                            if (errorMessage.Length > 0) errorMessage.Append('、');
                            errorMessage.Append(attribute.Name);
                        }
                    } else {
                        if (!item.AttributeValue.Value.HasValue()) {
                            if (errorMessage.Length > 0) errorMessage.Append('、');
                            errorMessage.Append(attribute.Name);
                        }
                    }
                }
                if (attribute.Selectable) {
                    var reference = current.ProductAttributeValues.FirstOrDefault(e => e.AttributeValueId == item.AttributeValueId);
                    if (reference != null) context.Remove(reference);
                } else {
                    var id = item.AttributeValueId.HasValue() ? item.AttributeValueId : item.AttributeValue.Id;
                    if (id.HasValue()) context.AttributeValue.Remove(context.AttributeValue.Find(id));
                }
            }

            if (errorMessage.Length > 0) {
                errorMessage.Append("為必填屬性");
                throw new OperationException(errorMessage.ToString());
            }
        }

        #endregion

        data.Base = null;
        if (data.BaseId == current.Id) throw new OperationException("無法設置自己為基本款");
        else if (data.BaseId.HasValue() && context.Product.Find(data.BaseId) is Product @base) { // 檢查 Base 是否產生循環參考
            var path = new List<int>(new[] { @base.Id });
            while (@base.BaseId.HasValue) {
                @base = context.Product.Find(@base.BaseId);
                if (path.Contains(@base.Id)) throw new OperationException("無法設置此商品為基本款，因產生循環");
                path.Add(@base.Id);
            }
        } else data.BaseId = null;

        data.Album = null;
        if (data.Description?.Content != null) current.Description.Content = data.Description.Content;
    }
}