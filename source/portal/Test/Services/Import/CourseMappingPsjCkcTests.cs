using System.Linq;
using PHStatistics.Portal.Services.Import.ImportSupport;

namespace PHStatistics.Portal.Test.Services.Import;

[TestFixture]
public class CourseMappingPsjCkcTests {
    [Test]
    public void PsjSchoolNameAliases_MapsGaomeiToGaomeiguan() {
        Assert.That(CourseMapping.PsjSchoolNameAliases["高美"], Is.EqualTo("高美館"));
    }
}
