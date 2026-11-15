using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

/// <summary>Clicks with a fake mouse from the Input System test fixture.</summary>
public class WorldClickerTests : InputTestFixture
{
    private TestUtils utils;
    private Camera worldCamera;
    private ClickCounter target;
    private Mouse mouse;

    public override void Setup()
    {
        base.Setup();
        utils = new TestUtils();
        GameObject cameraObject = utils.NewObject("Test Camera");
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        worldCamera = cameraObject.AddComponent<Camera>();
        worldCamera.orthographic = true;
        worldCamera.orthographicSize = 5f;
        WorldClicker clicker = cameraObject.AddComponent<WorldClicker>();
        TestUtils.SetField(clicker, "worldCamera", worldCamera);

        GameObject box = utils.NewObject("Clickable Box");
        box.transform.position = new Vector3(2f, 1f, 0f);
        box.AddComponent<BoxCollider2D>();
        target = box.AddComponent<ClickCounter>();
    }

    public override void TearDown()
    {
        utils.DestroyAll();
        base.TearDown();
    }

    private IEnumerator ClickAt(Vector2 world)
    {
        Vector2 screen = worldCamera.WorldToScreenPoint(world);
        Set(mouse.position, screen);
        yield return null;
        Press(mouse.leftButton, queueEventOnly: true);
        yield return null;
        Release(mouse.leftButton, queueEventOnly: true);
        yield return null;
    }

    [UnityTest]
    public IEnumerator AClickOnAnObject_CallsItsOnClicked()
    {
        mouse = InputSystem.AddDevice<Mouse>();

        yield return ClickAt(new Vector2(2f, 1f));

        Assert.AreEqual(1, target.Clicks);
    }

    [UnityTest]
    public IEnumerator AClickOnEmptySpace_ClicksNothing()
    {
        mouse = InputSystem.AddDevice<Mouse>();

        yield return ClickAt(new Vector2(-3f, -2f));

        Assert.AreEqual(0, target.Clicks);
    }

    [UnityTest]
    public IEnumerator HoldingTheButton_ClicksOnlyOnce()
    {
        mouse = InputSystem.AddDevice<Mouse>();
        Set(mouse.position, (Vector2)worldCamera.WorldToScreenPoint(new Vector2(2f, 1f)));
        yield return null;

        Press(mouse.leftButton, queueEventOnly: true);
        for (int i = 0; i < 5; i++)
        {
            yield return null;
        }

        Assert.AreEqual(1, target.Clicks);
    }

    [UnityTest]
    public IEnumerator ATurnedOffClicker_ClicksNothing()
    {
        mouse = InputSystem.AddDevice<Mouse>();
        worldCamera.GetComponent<WorldClicker>().enabled = false;

        yield return ClickAt(new Vector2(2f, 1f));

        Assert.AreEqual(0, target.Clicks);
    }

    [UnityTest]
    public IEnumerator WithoutAMouseOrTouch_NothingHappens()
    {
        yield return null;

        Assert.AreEqual(0, target.Clicks);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator AClickOverTheUi_DoesNotClickTheWorldBehindIt()
    {
        mouse = InputSystem.AddDevice<Mouse>();
        UiFactory.EnsureEventSystem();
        Canvas canvas = UiFactory.CreateCanvas("Blocking Canvas", 0);
        UnityEngine.UI.Image cover = UiFactory.CreateImage(canvas.transform, "Cover", null, Color.black);
        UiFactory.Stretch(cover.rectTransform);
        cover.raycastTarget = true;
        yield return null;

        yield return ClickAt(new Vector2(2f, 1f));

        Assert.AreEqual(0, target.Clicks);
        UiTestUtils.DestroyRootUi();
    }
}
