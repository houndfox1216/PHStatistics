using System.Collections.Generic;
using PHStatistics.Portal.Services;

namespace PHStatistics.Portal.Test.Services;

[TestFixture]
public class SchoolAccessEvaluatorTests {
    [Test]
    public void CanAccessSchool_ReturnsTrue_WhenViewAllSchools_EvenIfNotInAccessibleList() {
        bool result = SchoolAccessEvaluator.CanAccessSchool(
            hasViewAllSchools: true, accessibleSchoolIds: new List<int>(), targetSchoolId: 99);

        Assert.That(result, Is.True);
    }

    [Test]
    public void CanAccessSchool_ReturnsTrue_WhenTargetInAccessibleList() {
        bool result = SchoolAccessEvaluator.CanAccessSchool(
            hasViewAllSchools: false, accessibleSchoolIds: new[] { 1, 2, 3 }, targetSchoolId: 2);

        Assert.That(result, Is.True);
    }

    [Test]
    public void CanAccessSchool_ReturnsFalse_WhenTargetNotInAccessibleList_AndNoViewAllSchools() {
        bool result = SchoolAccessEvaluator.CanAccessSchool(
            hasViewAllSchools: false, accessibleSchoolIds: new[] { 1, 2, 3 }, targetSchoolId: 4);

        Assert.That(result, Is.False);
    }

    [Test]
    public void CanEditSchool_ReturnsTrue_WhenAdministrator_EvenIfNotAssigned() {
        bool result = SchoolAccessEvaluator.CanEditSchool(
            isAdministrator: true, isRestrictedToPrimarySchool: false, primarySchoolId: null,
            hasViewAllSchools: false, accessibleSchoolIds: new List<int>(), targetSchoolId: 99);

        Assert.That(result, Is.True);
    }

    [Test]
    public void CanEditSchool_ReturnsTrue_WhenRestricted_AndTargetIsPrimarySchool() {
        bool result = SchoolAccessEvaluator.CanEditSchool(
            isAdministrator: false, isRestrictedToPrimarySchool: true, primarySchoolId: 5,
            hasViewAllSchools: false, accessibleSchoolIds: new[] { 5, 6, 7 }, targetSchoolId: 5);

        Assert.That(result, Is.True);
    }

    [Test]
    public void CanEditSchool_ReturnsFalse_WhenRestricted_AndTargetIsNotPrimarySchool_EvenIfInAccessibleList() {
        bool result = SchoolAccessEvaluator.CanEditSchool(
            isAdministrator: false, isRestrictedToPrimarySchool: true, primarySchoolId: 5,
            hasViewAllSchools: false, accessibleSchoolIds: new[] { 5, 6, 7 }, targetSchoolId: 6);

        Assert.That(result, Is.False);
    }

    [Test]
    public void CanEditSchool_ReturnsFalse_WhenRestricted_AndPrimarySchoolNotSet() {
        bool result = SchoolAccessEvaluator.CanEditSchool(
            isAdministrator: false, isRestrictedToPrimarySchool: true, primarySchoolId: null,
            hasViewAllSchools: false, accessibleSchoolIds: new[] { 5, 6, 7 }, targetSchoolId: 5);

        Assert.That(result, Is.False);
    }

    [Test]
    public void CanEditSchool_ReturnsTrue_WhenNotRestricted_AndTargetInAccessibleList() {
        // 一般被指派多校的使用者，行為維持現行：被指派分校即可編輯
        bool result = SchoolAccessEvaluator.CanEditSchool(
            isAdministrator: false, isRestrictedToPrimarySchool: false, primarySchoolId: null,
            hasViewAllSchools: false, accessibleSchoolIds: new[] { 5, 6, 7 }, targetSchoolId: 6);

        Assert.That(result, Is.True);
    }

    [Test]
    public void CanEditSchool_ReturnsFalse_WhenRestricted_AndViewAllSchoolsAlsoGranted_ButTargetNotPrimary() {
        // ViewAllSchools 不會放寬 RestrictedToPrimarySchool 的寫入限制（spec 明確邊界情況）
        bool result = SchoolAccessEvaluator.CanEditSchool(
            isAdministrator: false, isRestrictedToPrimarySchool: true, primarySchoolId: 5,
            hasViewAllSchools: true, accessibleSchoolIds: new List<int>(), targetSchoolId: 6);

        Assert.That(result, Is.False);
    }
}
