using System.Collections.Generic;
using PHStatistics.Content;
using PHStatistics.Portal.Services.Import.ImportSupport;

namespace PHStatistics.Portal.Test.Services.Import;

[TestFixture]
public class LastWeekNumberMatcherTests {
    private static StudentPopulationItem MakeItem(long id, int courseId, ClassType classType, int number, string studentRemark = null) {
        return new StudentPopulationItem {
            Id = id,
            Class = new Class { CourseId = courseId, Type = classType },
            Number = number,
            StudentRemark = studentRemark,
        };
    }

    [Test]
    public void Apply_SingleClassPerGroup_CarriesOverPreviousNumber() {
        var prev = new List<StudentPopulationItem> { MakeItem(1, 10, ClassType.SubGroup, 6) };
        var cur = new List<StudentPopulationItem> { MakeItem(101, 10, ClassType.SubGroup, 0) };

        LastWeekNumberMatcher.Apply(cur, prev);

        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(6));
    }

    [Test]
    public void Apply_MultipleClassesSameCourseAndType_NoLongerZerosOutAllButFirst() {
        // This is the reported bug: a course with 2 sections under the same ClassType used to end up
        // with only the first item getting the previous total and every other item forced to 0.
        var prev = new List<StudentPopulationItem> {
            MakeItem(1, 10, ClassType.SubGroup, 5),
            MakeItem(2, 10, ClassType.SubGroup, 6),
            MakeItem(3, 10, ClassType.SubGroup, 5),
        };
        var cur = new List<StudentPopulationItem> {
            MakeItem(101, 10, ClassType.SubGroup, 3),
            MakeItem(102, 10, ClassType.SubGroup, 5),
            MakeItem(103, 10, ClassType.SubGroup, 4),
        };

        LastWeekNumberMatcher.Apply(cur, prev);

        // No StudentRemark on either side, so falls back to creation-order pairing (Id ascending).
        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(5));
        Assert.That(cur[1].LastWeekNumber, Is.EqualTo(6));
        Assert.That(cur[2].LastWeekNumber, Is.EqualTo(5));
    }

    [Test]
    public void Apply_MatchingStudentRemark_TakesPriorityOverOrder() {
        var prev = new List<StudentPopulationItem> {
            MakeItem(1, 10, ClassType.SubGroup, 10, "陳鼎諺 顧祐綺"),
            MakeItem(2, 10, ClassType.SubGroup, 6, "洪煒翔 屠建雄"),
        };
        // Current week's rows are in the opposite order from last week's — a naive order-based
        // pairing would mismatch them, but the matching remark must still win.
        var cur = new List<StudentPopulationItem> {
            MakeItem(101, 10, ClassType.SubGroup, 0, "洪煒翔 屠建雄"),
            MakeItem(102, 10, ClassType.SubGroup, 0, "陳鼎諺 顧祐綺"),
        };

        LastWeekNumberMatcher.Apply(cur, prev);

        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(6));
        Assert.That(cur[1].LastWeekNumber, Is.EqualTo(10));
    }

    [Test]
    public void Apply_BlankRemarkFallsBackToOrderAmongRemainingAfterRemarkMatchesRemoved() {
        var prev = new List<StudentPopulationItem> {
            MakeItem(1, 10, ClassType.SubGroup, 10, "甲班名單"),
            MakeItem(2, 10, ClassType.SubGroup, 20),
            MakeItem(3, 10, ClassType.SubGroup, 30),
        };
        var cur = new List<StudentPopulationItem> {
            MakeItem(101, 10, ClassType.SubGroup, 0), // blank remark, order-fallback candidate
            MakeItem(102, 10, ClassType.SubGroup, 0, "甲班名單"), // exact remark match
            MakeItem(103, 10, ClassType.SubGroup, 0), // blank remark, order-fallback candidate
        };

        LastWeekNumberMatcher.Apply(cur, prev);

        Assert.That(cur[1].LastWeekNumber, Is.EqualTo(10), "備註完全相同必須優先配對，不受順序影響");
        // Remaining prev items (2,3 -> Number 20,30) pair by order with remaining current items (101,103).
        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(20));
        Assert.That(cur[2].LastWeekNumber, Is.EqualTo(30));
    }

    [Test]
    public void Apply_MoreCurrentItemsThanPrevious_ExtraNewClassesGetZero() {
        var prev = new List<StudentPopulationItem> { MakeItem(1, 10, ClassType.SubGroup, 8) };
        var cur = new List<StudentPopulationItem> {
            MakeItem(101, 10, ClassType.SubGroup, 0),
            MakeItem(102, 10, ClassType.SubGroup, 0), // genuinely new class added this week
        };

        LastWeekNumberMatcher.Apply(cur, prev);

        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(8));
        Assert.That(cur[1].LastWeekNumber, Is.EqualTo(0));
    }

    [Test]
    public void Apply_NoPreviousGroupForCourseAndClassType_AllCurrentItemsGetZero() {
        var prev = new List<StudentPopulationItem> { MakeItem(1, 99, ClassType.SubGroup, 8) };
        var cur = new List<StudentPopulationItem> { MakeItem(101, 10, ClassType.SubGroup, 0) };

        LastWeekNumberMatcher.Apply(cur, prev);

        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(0));
    }

    private static StudentPopulationItem MakeItemWithClassId(long id, int classId, int number, string studentRemark = null) {
        return new StudentPopulationItem {
            Id = id,
            Class = new Class { Id = classId },
            Number = number,
            StudentRemark = studentRemark,
        };
    }

    [Test]
    public void ApplyByClassId_SameClassMultipleItems_EachGetsOwnPreviousNumber() {
        // 農十六 P3-中階：3 個小組班共用同一個 Class，上週 6/3/4，不可被覆寫成 6/6/6。
        var prev = new List<StudentPopulationItem> {
            MakeItemWithClassId(1, 1967, 6),
            MakeItemWithClassId(2, 1967, 3),
            MakeItemWithClassId(3, 1967, 4),
        };
        var cur = new List<StudentPopulationItem> {
            MakeItemWithClassId(101, 1967, 0),
            MakeItemWithClassId(102, 1967, 0),
            MakeItemWithClassId(103, 1967, 0),
        };

        LastWeekNumberMatcher.ApplyByClassId(cur, prev);

        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(6));
        Assert.That(cur[1].LastWeekNumber, Is.EqualTo(3));
        Assert.That(cur[2].LastWeekNumber, Is.EqualTo(4));
    }

    [Test]
    public void ApplyByClassId_MatchingStudentRemark_TakesPriorityOverOrder() {
        var prev = new List<StudentPopulationItem> {
            MakeItemWithClassId(1, 1971, 3, "甲"),
            MakeItemWithClassId(2, 1971, 2, "乙"),
        };
        var cur = new List<StudentPopulationItem> {
            MakeItemWithClassId(101, 1971, 0, "乙"),
            MakeItemWithClassId(102, 1971, 0, "甲"),
        };

        LastWeekNumberMatcher.ApplyByClassId(cur, prev);

        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(2));
        Assert.That(cur[1].LastWeekNumber, Is.EqualTo(3));
    }

    [Test]
    public void ApplyByClassId_ClassNotInPreviousWeek_LeavesLastWeekNumberUntouched() {
        var prev = new List<StudentPopulationItem> { MakeItemWithClassId(1, 5, 8) };
        var cur = new List<StudentPopulationItem> { MakeItemWithClassId(101, 99, 0) };
        cur[0].LastWeekNumber = 7;

        LastWeekNumberMatcher.ApplyByClassId(cur, prev);

        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(7));
    }

    [Test]
    public void ApplyByClassId_MoreCurrentItemsThanPrevious_ExtraItemsGetZero() {
        var prev = new List<StudentPopulationItem> { MakeItemWithClassId(1, 5, 8) };
        var cur = new List<StudentPopulationItem> {
            MakeItemWithClassId(101, 5, 0),
            MakeItemWithClassId(102, 5, 0),
        };

        LastWeekNumberMatcher.ApplyByClassId(cur, prev);

        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(8));
        Assert.That(cur[1].LastWeekNumber, Is.EqualTo(0));
    }

    [Test]
    public void Apply_DifferentClassTypeSameCourse_TreatedAsSeparateGroups() {
        var prev = new List<StudentPopulationItem> {
            MakeItem(1, 10, ClassType.SubGroup, 4),
            MakeItem(2, 10, ClassType.V3, 9),
        };
        var cur = new List<StudentPopulationItem> {
            MakeItem(101, 10, ClassType.SubGroup, 0),
            MakeItem(102, 10, ClassType.V3, 0),
        };

        LastWeekNumberMatcher.Apply(cur, prev);

        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(4));
        Assert.That(cur[1].LastWeekNumber, Is.EqualTo(9));
    }
}
