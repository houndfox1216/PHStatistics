using System;
using System.Collections.Generic;
using System.Framework;
using System.Framework.Data;
using System.Framework.EntityFrameworkCore;
using System.Framework.Web;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PHStatistics.Community;
using PHStatistics.Content;
using PHStatistics.Models;
using PHStatistics.Portal.Actions;

namespace PHStatistics.Portal.Models {
    public class Model : HttpModelBase<DataContext> {
        /// <summary>
        /// 過濾條件，Model取回資料時的過濾條件(除了Find)。
        /// </summary>
        public Condition Filter { get; set; }

        /// <summary>
        /// 建構 Model
        /// </summary>
        /// <param name="user">Model 將以此用戶進行各項操作</param>
        public Model() : base("Model") { }

        /// <summary>
        /// 廣告位置領域模型
        /// </summary>
        public BannerPositionModel BannerPosition { get { return GetSubModel<BannerPositionModel>(); } }

        /// <summary>
        /// 透過帳號與密碼認證用戶
        /// </summary>
        /// <param name="account">帳號</param>
        /// <param name="password">密碼</param>
        /// <returns>經過認證的用戶資料，當認證失敗時為空值</returns>
        public Member Authorize(string account, string password) {
            var action = new AuthorizationAction(CurrentUser, DataContext);
            action.Parameters["account"] = account;
            action.Parameters["password"] = password;
            action.Execute();
            return action.GetResult<Member>();
        }

        /// <summary>
        /// 變更目前用戶的密碼
        /// </summary>
        /// <param name="oldPassword">舊密碼</param>
        /// <param name="newPassword">新密碼</param>
        /// <returns></returns>
        public Member ChangePassword(string oldPassword, string newPassword) {
            var action = new ChangePasswordAction(CurrentUser, DataContext);
            var parameters = new Parameters {
                { "old-password", oldPassword },
                { "new-password", newPassword }
            };
            return action.Update(this.CurrentUser.Data as Member, parameters);
        }

        /// <summary>
        /// 以指定的代碼取得文化特性
        /// </summary>
        /// <param name="code">代碼</param>
        public Culture GetCulture(string code = null) {
            if (!DataContext.Culture.Any()) return new Culture { Id = "zh-TW", Codes = "zh-TW" };
            return DataContext.Culture.Find(code) ?? DataContext.Culture.FirstOrDefault(e => e.IsDefault) ?? DataContext.Culture.First();
        }

        /// <summary>
        /// 取得使用者分校資料
        /// </summary>
        /// <param name="mId">分校人員識別碼</param>
        /// <returns></returns>
        public List<SchoolAssignment> GetMemberSchool(string mId) {
            Guid checkId = Guid.Parse(mId);
            return DataContext.SchoolAssignment.Include("School").Include("Member").Where(e => e.Member.Id == checkId).ToList();
        }


        /// <summary>
        /// 變更目前用戶的密碼
        /// </summary>
        /// <param name="SchoolId">分校代碼</param>
        /// <param name="year">學年度</param>
        /// <param name="week">週次</param>
        /// <returns></returns>
        public StudentPopulation GetStudentPopulation(int SchoolId, int year, int week) {
            return DataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").Where(e => e.School.Id == SchoolId && e.Year == year && e.Week == week).FirstOrDefault();
        }

        /// <summary>
        /// 變更目前用戶的密碼
        /// </summary>
        /// <param name="SchoolId">分校代碼</param>
        /// <param name="year">學年度</param>
        /// <param name="week">週次</param>
        /// <returns></returns>
        public StudentPopulation GetLastStudentPopulation(int SchoolId, int year) {
            return DataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").Where(e => e.School.Id == SchoolId && e.Year == year ).OrderByDescending(e => e.Year).OrderByDescending(e => e.Week).FirstOrDefault();
        }
    }
}
