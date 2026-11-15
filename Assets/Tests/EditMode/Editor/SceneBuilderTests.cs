using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Runs Tools > Rebuild Scene (ProjectBuilder.BuildScene) and checks what it builds.
/// The files it writes are copied first and put back afterwards, so the test leaves the project as it was.
/// </summary>
public class SceneBuilderTests
{
    private const string SettingsPath = "Assets/Data/Day1Settings.asset";

    // Everything the builder can write. The test puts these files back exactly as they were.
    private static readonly string[] WatchedFolders = { "Assets/Scenes", "Assets/Prefabs", "Assets/Data", "Assets/Sprites" };
    private const string BuildSettingsFile = "ProjectSettings/EditorBuildSettings.asset";

    private Dictionary<string, byte[]> savedFiles;
    private int objectsAfterFirstBuild;
    private int objectsAfterSecondBuild;
    private byte[] settingsBeforeBuild;
    private byte[] settingsAfterBuild;
    private float tunedBlowerForce;
    private float blowerForceInPrefab;
    private float tunedSievePrice;
    private float sievePriceAfterBuild;

    private static IEnumerable<string> WatchedFiles()
    {
        return WatchedFolders.SelectMany(folder => Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
            .Select(path => path.Replace('\\', '/'))
            .Concat(new[] { BuildSettingsFile });
    }

    [OneTimeSetUp]
    public void BuildTwice()
    {
        savedFiles = WatchedFiles().ToDictionary(path => path, File.ReadAllBytes);

        // Tune two values the way a designer would in the Inspector. The rebuild must keep them.
        LandfillDaySettings day = AssetDatabase.LoadAssetAtPath<LandfillDaySettings>(SettingsPath);
        tunedBlowerForce = day.blowerForce + 1.5f;
        day.blowerForce = tunedBlowerForce;
        EditorUtility.SetDirty(day);
        MachineData sieve = AssetDatabase.LoadAssetAtPath<MachineData>("Assets/Data/Machines/Sieve.asset");
        tunedSievePrice = sieve.price + 7;
        sieve.price = (int)tunedSievePrice;
        EditorUtility.SetDirty(sieve);
        AssetDatabase.SaveAssets();
        settingsBeforeBuild = File.ReadAllBytes(SettingsPath);

        ProjectBuilder.BuildScene();
        objectsAfterFirstBuild = CountObjects(SceneManager.GetActiveScene());
        settingsAfterBuild = File.ReadAllBytes(SettingsPath);
        blowerForceInPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Blower.prefab").GetComponent<AreaEffector2D>().forceMagnitude;
        sievePriceAfterBuild = AssetDatabase.LoadAssetAtPath<MachineData>("Assets/Data/Machines/Sieve.asset").price;

        ProjectBuilder.BuildScene();
        objectsAfterSecondBuild = CountObjects(SceneManager.GetActiveScene());
    }

    [OneTimeTearDown]
    public void PutTheFilesBack()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        foreach (string path in WatchedFiles().Where(path => !savedFiles.ContainsKey(path)))
        {
            AssetDatabase.DeleteAsset(path); // Something the builder created that wasn't there before.
        }
        foreach (KeyValuePair<string, byte[]> file in savedFiles)
        {
            if (!File.Exists(file.Key) || !File.ReadAllBytes(file.Key).SequenceEqual(file.Value))
            {
                File.WriteAllBytes(file.Key, file.Value);
            }
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        foreach (string path in savedFiles.Keys.Where(path => path.StartsWith("Assets/") && !path.EndsWith(".meta")))
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }

    private static int CountObjects(Scene scene)
    {
        return scene.GetRootGameObjects().Sum(root => root.GetComponentsInChildren<Transform>(true).Length);
    }

    private static T One<T>() where T : Component
    {
        T[] found = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.AreEqual(1, found.Length, typeof(T).Name);
        return found[0];
    }

    [Test]
    public void TheBuiltScene_IsSavedAsTheLandfillScene()
    {
        Assert.AreEqual("Assets/Scenes/Landfill.unity", SceneManager.GetActiveScene().path);
        Assert.IsFalse(SceneManager.GetActiveScene().isDirty);
    }

    [Test]
    public void TheBuiltScene_HasNoMissingScriptsOrEmptyFields()
    {
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();

        CollectionAssert.IsEmpty(WiringCheck.MissingScripts(roots));
        CollectionAssert.IsEmpty(WiringCheck.EmptyReferences(roots, new HashSet<string>()));
    }

    [Test]
    public void BuildingTwice_GivesTheSameScene()
    {
        Assert.Greater(objectsAfterFirstBuild, 30);
        Assert.AreEqual(objectsAfterFirstBuild, objectsAfterSecondBuild);
    }

    [Test]
    public void TheShopBar_ListsEveryMachineAsset()
    {
        MachineData[] listed = TestUtils.GetField<MachineData[]>(One<ShopBar>(), "machines");

        CollectionAssert.AreEqual(LandfillAssets.AllMachines(), listed);
        Assert.AreEqual(AssetDatabase.FindAssets("t:MachineData").Length, listed.Length);
    }

    [Test]
    public void Regression_RebuildingKeepsTheTunedDaySettings()
    {
        CollectionAssert.AreEqual(settingsBeforeBuild, settingsAfterBuild, "Day1Settings must not be written again when it exists");
    }

    [Test]
    public void Regression_TheBlowerPrefabTakesItsForceFromTheDaySettings()
    {
        Assert.AreEqual(tunedBlowerForce, blowerForceInPrefab, 0.0001f);
    }

    [Test]
    public void Regression_RebuildingKeepsTheTunedMachinePrice()
    {
        Assert.AreEqual(tunedSievePrice, sievePriceAfterBuild);
    }

    [Test]
    public void EveryMachineAsset_PointsToItsPrefab()
    {
        foreach (MachineData machine in LandfillAssets.AllMachines())
        {
            Assert.IsNotNull(machine.prefab, machine.name);
            Assert.AreEqual(machine.name, machine.prefab.name);
        }
    }

    [Test]
    public void TheBuildSettings_StartWithTheLandfillScene()
    {
        Assert.AreEqual(1, EditorBuildSettings.scenes.Length);
        Assert.AreEqual("Assets/Scenes/Landfill.unity", EditorBuildSettings.scenes[0].path);
    }
}
