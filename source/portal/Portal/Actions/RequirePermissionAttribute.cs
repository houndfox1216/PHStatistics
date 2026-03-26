namespace PHStatistics.Portal;

/// <summary>
/// 標記此 Controller 或 Action 需要指定的 SystemPermission 才能存取。
/// 由 AdminBaseController.OnActionExecuting 統一讀取並檢查。
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple = false)]
public class RequirePermissionAttribute : System.Attribute {
    public SystemPermission Permission { get; }

    public RequirePermissionAttribute(SystemPermission permission) => Permission = permission;
}
