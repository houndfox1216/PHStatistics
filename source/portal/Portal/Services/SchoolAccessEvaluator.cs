using System.Collections.Generic;
using System.Linq;

namespace PHStatistics.Portal.Services {
    public static class SchoolAccessEvaluator {
        /// <summary>
        /// 瀏覽/匯出用的存取判斷。
        /// </summary>
        public static bool CanAccessSchool(bool hasViewAllSchools, IEnumerable<int> accessibleSchoolIds, int targetSchoolId) {
            if (hasViewAllSchools) return true;
            return accessibleSchoolIds.Contains(targetSchoolId);
        }

        /// <summary>
        /// 寫入用的存取判斷。RestrictedToPrimarySchool 只認主要轄校，
        /// 不會被 ViewAllSchools 放寬；其餘角色沿用現行「被指派分校即可寫」。
        /// </summary>
        public static bool CanEditSchool(bool isAdministrator, bool isRestrictedToPrimarySchool, int? primarySchoolId,
                bool hasViewAllSchools, IEnumerable<int> accessibleSchoolIds, int targetSchoolId) {
            if (isAdministrator) return true;
            if (isRestrictedToPrimarySchool) return primarySchoolId.HasValue && primarySchoolId.Value == targetSchoolId;
            return CanAccessSchool(hasViewAllSchools, accessibleSchoolIds, targetSchoolId);
        }
    }
}
