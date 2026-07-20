using System.Linq;
using PHStatistics.Portal.Services.Import.ImportSupport;

namespace PHStatistics.Portal.Test.Services.Import;

[TestFixture]
public class CourseMappingAsMathScienceTests {
    [Test]
    public void AsCourseIds_MathKeys_Are12ContiguousIdsStartingAt378() {
        Assert.That(CourseMapping.AsCourseIds["MP"], Is.EqualTo(Enumerable.Range(378, 12).ToArray()));
        Assert.That(CourseMapping.AsCourseIds["MG"], Is.EqualTo(Enumerable.Range(378, 12).ToArray()));
    }

    [Test]
    public void AsCourseIds_ScienceKeys_Are12ContiguousIdsStartingAt428() {
        Assert.That(CourseMapping.AsCourseIds["SP"], Is.EqualTo(Enumerable.Range(428, 12).ToArray()));
        Assert.That(CourseMapping.AsCourseIds["SG"], Is.EqualTo(Enumerable.Range(428, 12).ToArray()));
    }

    [Test]
    public void AsColumnType_MapsPersonalCodesToPersonal_AndGeneralCodesToGeneral() {
        Assert.That(CourseMapping.AsColumnType("MP"), Is.EqualTo(PHStatistics.Content.ClassType.Personal));
        Assert.That(CourseMapping.AsColumnType("SP"), Is.EqualTo(PHStatistics.Content.ClassType.Personal));
        Assert.That(CourseMapping.AsColumnType("MG"), Is.EqualTo(PHStatistics.Content.ClassType.General));
        Assert.That(CourseMapping.AsColumnType("SG"), Is.EqualTo(PHStatistics.Content.ClassType.General));
    }
}
