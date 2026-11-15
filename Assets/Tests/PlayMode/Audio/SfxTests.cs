using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class SfxTests
{
    [SetUp]
    public void SetUp()
    {
        DestroyPlayer();
    }

    [TearDown]
    public void TearDown()
    {
        DestroyPlayer();
    }

    private static void DestroyPlayer()
    {
        GameObject player = GameObject.Find("Sound Effects");
        if (player != null)
        {
            Object.DestroyImmediate(player);
        }
    }

    private static Dictionary<string, AudioClip> Clips => TestUtils.GetStaticField<Dictionary<string, AudioClip>>(typeof(Sfx), "clips");

    private static int Players()
    {
        int count = 0;
        foreach (AudioSource source in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
        {
            if (source.name == "Sound Effects")
            {
                count++;
            }
        }
        return count;
    }

    [Test]
    public void Play_CreatesOneHiddenPlayerThatSurvivesSceneLoads()
    {
        Sfx.Play("Click");

        GameObject player = GameObject.Find("Sound Effects");
        Assert.IsNotNull(player);
        Assert.AreEqual("DontDestroyOnLoad", player.scene.name);
    }

    [Test]
    public void Play_ManyTimes_UsesTheSamePlayer()
    {
        Sfx.Play("Click");
        Sfx.Play("Click", 0.5f);
        Sfx.Play("Click");

        Assert.AreEqual(1, Players());
    }

    [Test]
    public void Play_LoadsTheClipOnceAndKeepsIt()
    {
        Sfx.Play("Click");

        Assert.IsTrue(Clips.ContainsKey("Click"));
        Assert.IsNotNull(Clips["Click"], "Assets/Resources/Audio/Click exists");
        Assert.AreSame(Resources.Load<AudioClip>("Audio/Click"), Clips["Click"]);
    }

    [Test]
    public void Play_OfASoundThatDoesNotExist_DoesNothing()
    {
        Assert.DoesNotThrow(() => Sfx.Play("No Such Sound"));
        Assert.IsTrue(Clips.ContainsKey("No Such Sound"));
        Assert.IsNull(Clips["No Such Sound"]);
    }

    [Test]
    public void Play_AfterThePlayerWasDestroyed_MakesANewOne()
    {
        Sfx.Play("Click");
        DestroyPlayer();

        Sfx.Play("Click");

        Assert.AreEqual(1, Players());
    }
}
