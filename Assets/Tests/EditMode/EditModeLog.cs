using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Outside Play mode, Object.Destroy only logs an error and leaves the object alive.
/// The game calls Destroy when an item is sorted or a machine is sold, so EditMode tests of those paths
/// say up front that this one error is expected. Any other error still fails the test.
/// </summary>
public static class EditModeLog
{
    public static void ExpectDestroy()
    {
        LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));
    }
}
