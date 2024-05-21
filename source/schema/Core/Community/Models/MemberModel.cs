using PHStatistics.Actions;
using System;
using System.Collections.Generic;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Framework.Security;
using PHStatistics.Community;

namespace PHStatistics.Models {
    public class MemberModel : ModelBase<DataContext> {
        /// <summary>
        /// 過濾條件，Model取回資料時的過濾條件(除了Find)。
        /// </summary>
        public Condition Filter { get; set; }

        /// <summary>
        /// 建構 MemberModel
        /// </summary>
        /// <param name="user">Model 將以此用戶進行各項操作</param>
        public MemberModel() : base("Member") => this.Filter = new Condition();

        /// <summary>
        /// 取得指定編號的會員資料
        /// </summary>
        /// <param name="id">會員編號</param>
        /// <param name="user">執行操作的用戶</param>
        public Member Find(Guid id, bool trackable = true, IUser user = null) => new MemberReadAction(user ?? this.CurrentUser, this.DataContext).Find(id);

       // public Member FindByPhone(string phone, IUser user = null) => new MemberReadAction(user ?? this.CurrentUser, this.DataContext).FindByPhone(phone);

        /// <summary>
        /// 查詢符合指定條件的實體資料
        /// </summary>
        /// <param name="keyword">關鍵字</param>
        /// <param name="condition">查詢條件</param>
        /// <param name="sortings">排序方式</param>
        /// <param name="user">執行操作的用戶</param>
        /// <param name="includes">須積極取回之相關資料</param>
        public IQueryable<Member> Query(string keyword = null, Condition condition = null, Sorting[] sortings = null, IUser user = null, params string[] includes) {
            condition ??= new Condition();
            condition.And(Filter);
            if (keyword.HasValue()) {
                var keywordCondition = new Condition("Name", Operator.Contains, keyword);
                condition.And(keywordCondition);
            }
            return new MemberReadAction(user ?? this.CurrentUser, this.DataContext).Query(condition, sortings, includes);
        }

        /// <summary>
        /// 依資料範本新增實體資料
        /// </summary>
        /// <param name="data">資料範本</param>
        /// <param name="user">執行操作的用戶</param>
        public Member Create(Member data, Parameters parameters = null, IUser user = null) => new MemberCreateAction(user ?? this.CurrentUser, this.DataContext).Create(data, parameters);

        /// <summary>
        /// 依資料範本更新實體資料
        /// </summary>
        /// <param name="data">資料範本</param>
        /// <param name="user">執行操作的用戶</param>
        /// <returns>影響的資料筆數</returns>
        public Member Update(Member data, Parameters parameters = null, IUser user = null) => new MemberUpdateAction(user ?? this.CurrentUser, this.DataContext).Update(data, parameters);

        /// <summary>
        /// 依指定識別碼刪除實體資料
        /// </summary>
        /// <param name="id">識別碼</param>
        /// <param name="user">執行操作的用戶</param>
        public void Delete(Guid id, Parameters parameters = null, IUser user = null) => new MemberDeleteAction(user ?? this.CurrentUser, this.DataContext).Delete(new Member { Id = id }, parameters);

        public Member Authorize(string account, string password) {
            try {
                var hashedPassword = password;
                return DataContext.Member.Single(e => e.Account == account && e.Password == hashedPassword);
            }
            catch (Exception e) { throw new SecurityException("會員帳號或密碼錯誤!", e); }
        }
    }
}
