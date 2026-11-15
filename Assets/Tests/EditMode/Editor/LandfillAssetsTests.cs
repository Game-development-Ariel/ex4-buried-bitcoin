using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class LandfillAssetsTests
{
    private TestUtils utils;

    [SetUp]
    public void SetUp()
    {
        utils = new TestUtils();
    }

    [TearDown]
    public void TearDown()
    {
        utils.DestroyAll();
    }

    private static void Report(LandfillDaySettings day)
    {
        TestUtils.CallStatic(typeof(LandfillAssets), "ReportMissingItems", day, "Assets/Data/Test.asset");
    }

    [Test]
    public void Regression_ExistingSettingsWithoutATarget_AreReportedWhenBuilding()
    {
        LandfillDaySettings day = utils.Settings();
        day.trucks = new[] { TestUtils.Load((utils.ItemData("Can"), 1)) };

        LogAssert.Expect(LogType.Error, new Regex("has no target item"));
        Report(day);
    }

    [Test]
    public void Regression_ATruckEntryWithoutAnItem_IsReportedWhenBuilding()
    {
        LandfillDaySettings day = utils.Settings(utils.ItemData("Toolbox"));
        day.trucks = new[] { TestUtils.Load((utils.ItemData("Can"), 1)), TestUtils.Load((null, 2)) };

        LogAssert.Expect(LogType.Error, new Regex("Truck 2 .* has an entry with no item"));
        Report(day);
    }

    [Test]
    public void CompleteSettings_AreNotReported()
    {
        LandfillDaySettings day = utils.Settings(utils.ItemData("Toolbox"));
        day.trucks = new[] { TestUtils.Load((utils.ItemData("Can"), 1)) };

        Report(day);

        LogAssert.NoUnexpectedReceived();
    }
}
