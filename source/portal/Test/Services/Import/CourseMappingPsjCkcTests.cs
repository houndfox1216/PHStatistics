using System.Linq;
using PHStatistics.Portal.Services.Import.ImportSupport;

namespace PHStatistics.Portal.Test.Services.Import;

[TestFixture]
public class CourseMappingPsjCkcTests {
    [Test]
    public void PsjGradeOrder_Has11GradesStartingAtGrade2NoGrade1() {
        Assert.That(CourseMapping.PsjGradeOrder.Length, Is.EqualTo(11));
        Assert.That(CourseMapping.PsjGradeOrder[0], Is.EqualTo("二年級"));
        Assert.That(CourseMapping.PsjGradeOrder[10], Is.EqualTo("高三"));
        Assert.That(CourseMapping.PsjGradeOrder, Does.Not.Contain("一年級"));
    }

    [Test]
    public void CkcCourseIds_HasThreeSubjectsWith11ContiguousIdsEach() {
        Assert.That(CourseMapping.CkcCourseIds["CKC_E"], Is.EqualTo(Enumerable.Range(345, 11).ToArray()));
        Assert.That(CourseMapping.CkcCourseIds["CKC_C"], Is.EqualTo(Enumerable.Range(356, 11).ToArray()));
        Assert.That(CourseMapping.CkcCourseIds["CKC_M"], Is.EqualTo(Enumerable.Range(367, 11).ToArray()));
    }

    [Test]
    public void PsjSchoolNameAliases_MapsGaomeiToGaomeiguan() {
        Assert.That(CourseMapping.PsjSchoolNameAliases["高美"], Is.EqualTo("高美館"));
    }
}
