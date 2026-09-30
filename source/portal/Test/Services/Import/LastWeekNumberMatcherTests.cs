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

    private static StudentPopulationItem MakeLinked(long id, int classId, int number, long? previousItemId = null, string studentRemark = null) {
        var item = MakeItemWithClassId(id, classId, number, studentRemark);
        item.PreviousItemId = previousItemId;
        return item;
    }

    [Test]
    public void SyncFromLastWeek_LinkedItems_TakeOwnPreviousNumberEvenWhenOrderAndRemarksCannotTell() {
        // 備註空白、本週 Id 順序與上週相反：只有連結能配對正確。
        var prev = new List<StudentPopulationItem> {
            MakeLinked(1, 1967, 6), MakeLinked(2, 1967, 3), MakeLinked(3, 1967, 4),
        };
        var cur = new List<StudentPopulationItem> {
            MakeLinked(101, 1967, 0, previousItemId: 3),
            MakeLinked(102, 1967, 0, previousItemId: 1),
            MakeLinked(103, 1967, 0, previousItemId: 2),
        };

        LastWeekNumberMatcher.SyncFromLastWeek(cur, prev);

        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(4));
        Assert.That(cur[1].LastWeekNumber, Is.EqualTo(6));
        Assert.That(cur[2].LastWeekNumber, Is.EqualTo(3));
    }

    [Test]
    public void SyncFromLastWeek_LinkBeatsRemarkMatch() {
        var prev = new List<StudentPopulationItem> {
            MakeLinked(1, 5, 10, studentRemark: "甲"), MakeLinked(2, 5, 20, studentRemark: "乙"),
        };
        // 備註寫「甲」，但連結明確指向上週的 Id 2。
        var cur = new List<StudentPopulationItem> { MakeLinked(101, 5, 0, previousItemId: 2, studentRemark: "甲") };

        LastWeekNumberMatcher.SyncFromLastWeek(cur, prev);

        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(20));
    }

    [Test]
    public void SyncFromLastWeek_DanglingLink_FallsBackToClassPairingAndRelinks() {
        // 上週人數表被重新匯入，舊的上週 Item(Id 999) 已不存在。
        var prev = new List<StudentPopulationItem> { MakeLinked(50, 5, 8) };
        var cur = new List<StudentPopulationItem> { MakeLinked(101, 5, 0, previousItemId: 999) };

        LastWeekNumberMatcher.SyncFromLastWeek(cur, prev);

        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(8));
        Assert.That(cur[0].PreviousItemId, Is.EqualTo(50), "1:1 配對確定，應改連到新的上週項目");
    }

    [Test]
    public void SyncFromLastWeek_UnlinkedOneToOne_PersistsLink() {
        var prev = new List<StudentPopulationItem> { MakeLinked(1, 5, 8) };
        var cur = new List<StudentPopulationItem> { MakeLinked(101, 5, 0) };

        LastWeekNumberMatcher.SyncFromLastWeek(cur, prev);

        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(8));
        Assert.That(cur[0].PreviousItemId, Is.EqualTo(1));
    }

    [Test]
    public void SyncFromLastWeek_UnlinkedAmbiguousOrderPairing_SetsNumberButDoesNotPersistLink() {
        // 同 Class 多筆、備註空白：依 Id 順序配對只是推測，不可固化成連結。
        var prev = new List<StudentPopulationItem> { MakeLinked(1, 5, 6), MakeLinked(2, 5, 3) };
        var cur = new List<StudentPopulationItem> { MakeLinked(101, 5, 0), MakeLinked(102, 5, 0) };

        LastWeekNumberMatcher.SyncFromLastWeek(cur, prev);

        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(6));
        Assert.That(cur[1].LastWeekNumber, Is.EqualTo(3));
        Assert.That(cur[0].PreviousItemId, Is.Null);
        Assert.That(cur[1].PreviousItemId, Is.Null);
    }

    [Test]
    public void SyncFromLastWeek_TwoItemsLinkedToSamePrevious_SecondFallsBack() {
        var prev = new List<StudentPopulationItem> { MakeLinked(1, 5, 6), MakeLinked(2, 5, 3) };
        var cur = new List<StudentPopulationItem> {
            MakeLinked(101, 5, 0, previousItemId: 1),
            MakeLinked(102, 5, 0, previousItemId: 1), // 重複認領
        };

        LastWeekNumberMatcher.SyncFromLastWeek(cur, prev);

        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(6));
        Assert.That(cur[1].LastWeekNumber, Is.EqualTo(3), "第二筆退回配對，取上週剩下的那筆");
    }

    [Test]
    public void SyncFromLastWeek_AdminEditedLastWeekNumber_IsPickedUp() {
        var previous = MakeLinked(1, 5, 6);
        var cur = new List<StudentPopulationItem> { MakeLinked(101, 5, 0, previousItemId: 1) };
        LastWeekNumberMatcher.SyncFromLastWeek(cur, new List<StudentPopulationItem> { previous });
        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(6));

        previous.Number = 9; // 管理員事後修改上週人數
        LastWeekNumberMatcher.SyncFromLastWeek(cur, new List<StudentPopulationItem> { previous });

        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(9));
    }

    [Test]
    public void SyncFromLastWeek_ExtraNewSameNameClassThisWeek_GetsZero() {
        var prev = new List<StudentPopulationItem> { MakeLinked(1, 5, 8) };
        var cur = new List<StudentPopulationItem> {
            MakeLinked(101, 5, 0, previousItemId: 1),
            MakeLinked(102, 5, 0), // 本週才新增的同名班級，上週沒有
        };
        cur[1].LastWeekNumber = 5; // 舊的殘留值，必須被歸 0，不能原封不動

        LastWeekNumberMatcher.SyncFromLastWeek(cur, prev);

        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(8));
        Assert.That(cur[1].LastWeekNumber, Is.EqualTo(0));
        Assert.That(cur[1].PreviousItemId, Is.Null);
    }

    [Test]
    public void SyncFromLastWeek_DanglingLinkWhenRestOfGroupAlreadyClaimed_GetsZeroNotStaleValue() {
        // 上週該 Class 有 A、B，本週 X 連到 A、Y 連到 B；B 之後被刪除，Y 的連結失效，
        // 而 A 已被 X 認領，Y 沒有可配對的對象，必須歸 0 而不是永遠留著舊值。
        var prev = new List<StudentPopulationItem> { MakeLinked(1, 1967, 6) };
        var cur = new List<StudentPopulationItem> {
            MakeLinked(101, 1967, 0, previousItemId: 1),
            MakeLinked(102, 1967, 0, previousItemId: 2), // 上週 Id 2 已不存在
        };
        cur[1].LastWeekNumber = 3;

        LastWeekNumberMatcher.SyncFromLastWeek(cur, prev);

        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(6));
        Assert.That(cur[1].LastWeekNumber, Is.EqualTo(0));
        Assert.That(cur[1].PreviousItemId, Is.Null);
    }

    [Test]
    public void SyncFromLastWeek_DuplicateClaimWhenGroupFullyClaimed_DuplicateGetsZero() {
        var prev = new List<StudentPopulationItem> { MakeLinked(1, 5, 6) };
        var cur = new List<StudentPopulationItem> {
            MakeLinked(101, 5, 0, previousItemId: 1),
            MakeLinked(102, 5, 0, previousItemId: 1), // 重複認領同一筆，且上週沒有其他項目可配
        };
        cur[1].LastWeekNumber = 6;

        LastWeekNumberMatcher.SyncFromLastWeek(cur, prev);

        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(6));
        Assert.That(cur[1].LastWeekNumber, Is.EqualTo(0), "上週人數不可被重複計入");
    }

    [Test]
    public void SyncFromLastWeek_ClassMissingLastWeek_LeavesValueUntouched() {
        var prev = new List<StudentPopulationItem> { MakeLinked(1, 5, 8) };
        var cur = new List<StudentPopulationItem> { MakeLinked(101, 99, 0) };
        cur[0].LastWeekNumber = 7;

        LastWeekNumberMatcher.SyncFromLastWeek(cur, prev);

        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(7));
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
