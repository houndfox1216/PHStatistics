using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.Community;
using System.Framework.Data;
using System.Framework.EntityFrameworkCore;
using System.Framework.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using PHStatistics.Actions;
using PHStatistics.Content;
using Environment = System.Framework.Environment;

// ReSharper disable once CheckNamespace
namespace PHStatistics.Actions;

/// <summary>
/// 新增人數表項目資料之操作。
/// </summary>
[Description("新增人數表項目資料")]
public class StudentPopulationItemCreateAction : CreateActionBase<StudentPopulationItem, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => [SystemPermission.StudentPopulation];

    /// <summary>
    /// 建構 StudentPopulationItemCreateAction
    /// </summary>
    /// <param name="user">請求操作的會員</param>
    /// <param name="context">資料脈絡ㄑ</param>
    public StudentPopulationItemCreateAction(IUser user, DataContext context = null) : base("新增人數表項目資料", user, context) {
        SetActionLog<ActionLogCommand<ActionLog, DataContext>>(context);
    }

    protected override void OnTransacting(DataContext context, StudentPopulationItem data) {

    }

    protected override void OnCreating(DataContext context, StudentPopulationItem data) {
        
    }
}