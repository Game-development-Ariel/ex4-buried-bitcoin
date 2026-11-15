using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// The course rules and the rules in our README, checked automatically by reading the source files
/// and the compiled assemblies. When one of these fails, either fix the code or, if the exception is
/// on purpose, add it to the allow-list here and explain it in the README.
/// </summary>
public class ArchitectureTests
{
    private static readonly Assembly Game = typeof(LandfillRound).Assembly;
    private static readonly Assembly EditorTools = typeof(LandfillSceneBuilder).Assembly;

    // ---------- (a) The game doesn't know the UI ----------

    [Test]
    public void GameCode_DoesNotUseAnyUiClass()
    {
        string[] uiTypes = SourceFiles.Runtime().Where(path => path.StartsWith("UI/")).Select(SourceFiles.TypeName).ToArray();
        var problems = new List<string>();

        foreach (string path in SourceFiles.Runtime().Where(path => !path.StartsWith("UI/")))
        {
            string code = SourceFiles.Code(path);
            foreach (string uiType in uiTypes)
            {
                if (Regex.IsMatch(code, @"\b" + uiType + @"\b"))
                {
                    problems.Add($"{path} uses {uiType}");
                }
            }
            if (code.Contains("using UnityEngine.UI;"))
            {
                problems.Add($"{path} uses UnityEngine.UI");
            }
        }

        CollectionAssert.IsEmpty(problems, "The UI listens to the game, never the other way around");
    }

    [Test]
    public void GameCode_DoesNotWriteSentencesForThePlayer()
    {
        // Gameplay sends a reason (LossReason, MachineShop.Refusal) and the UI chooses the words.
        string[] gameplay = { "Game/LandfillRound.cs", "Shop/MachineShop.cs", "Sorting/SortingDecisions.cs", "Sorting/TableContents.cs" };
        foreach (string path in gameplay)
        {
            // Inspector attributes like [Tooltip("...")] are for us, not for the player.
            string[] codeLines = SourceFiles.Code(path).Split('\n').Where(line => !line.Trim().StartsWith("[")).ToArray();
            Assert.IsFalse(Regex.IsMatch(string.Join("\n", codeLines), "\"[^\"]*\\s[^\"]*\""), path + " has a sentence in a string");
        }
    }

    [Test]
    public void Regression_TheTargetIsChosenOnlyInTheDaySettings()
    {
        // The target used to be named in several scripts. Now only Day1Settings.target says what it is.
        foreach (string path in SourceFiles.Runtime())
        {
            StringAssert.DoesNotContain("toolbox", SourceFiles.Code(path).ToLowerInvariant(), path);
        }
    }

    // ---------- (b) Every subscription is undone ----------

    // Subscriptions outside OnEnable that the README explains:
    // TableContents listens to an item only while it is on the table (undone in Remove and OnDestroy).
    private static readonly HashSet<string> SubscriptionsOutsideOnEnable = new HashSet<string>
    {
        "Sorting/TableContents.cs: item.Clicked += OnItemClicked",
    };

    private static readonly Regex EventAdd = new Regex(@"([\w\.]+\.\w+)\s*\+=\s*(\w+)\s*;");
    private static readonly Regex Subscribe = new Regex(@"([\w\.]+)\.Subscribe\((\w+)\)");

    [TestCaseSource(typeof(SourceFiles), nameof(SourceFiles.Runtime))]
    public void EverySubscriptionInOnEnable_IsUndoneInOnDisable(string path)
    {
        string code = SourceFiles.Code(path);
        string onEnable = SourceFiles.MethodBody(code, "OnEnable");
        string onDisable = SourceFiles.MethodBody(code, "OnDisable");
        var missing = new List<string>();

        foreach (Match add in EventAdd.Matches(onEnable))
        {
            string remove = $"{add.Groups[1].Value} -= {add.Groups[2].Value};";
            if (!onDisable.Contains(remove))
            {
                missing.Add(remove);
            }
        }
        foreach (Match subscribe in Subscribe.Matches(onEnable))
        {
            string remove = $"{subscribe.Groups[1].Value}.Unsubscribe({subscribe.Groups[2].Value})";
            if (!onDisable.Contains(remove))
            {
                missing.Add(remove);
            }
        }

        CollectionAssert.IsEmpty(missing, path + ": OnDisable should undo these");
    }

    [Test]
    public void EventSubscriptions_HappenOnlyInOnEnable_ExceptTheDocumentedOnes()
    {
        var found = new List<string>();
        foreach (string path in SourceFiles.Runtime())
        {
            string code = SourceFiles.Code(path);
            string onEnable = SourceFiles.MethodBody(code, "OnEnable");
            string outside = onEnable.Length == 0 ? code : code.Replace(onEnable, "");
            foreach (Match add in EventAdd.Matches(outside))
            {
                found.Add($"{path}: {add.Groups[1].Value} += {add.Groups[2].Value}");
            }
            foreach (Match subscribe in Subscribe.Matches(outside))
            {
                found.Add($"{path}: {subscribe.Value}");
            }
        }

        CollectionAssert.AreEquivalent(SubscriptionsOutsideOnEnable, found);
    }

    [Test]
    public void ButtonListeners_AreAddedOnceInAwakeOrWhenTheButtonIsCreated()
    {
        foreach (string path in SourceFiles.Runtime())
        {
            string code = SourceFiles.Code(path);
            Assert.IsFalse(SourceFiles.MethodBody(code, "OnEnable").Contains("AddListener"), path + " would add the listener again every time it is enabled");
            Assert.IsFalse(SourceFiles.MethodBody(code, "Update").Contains("AddListener"), path);
        }
    }

    // ---------- (c) No singleton, and only the statics the README lists ----------

    // Mutable statics, the same as the table in the README ("סינגלטון ומשתנים סטטיים").
    private static readonly HashSet<string> AllowedStatics = new HashSet<string>
    {
        "Sfx.source",
        "Sfx.clips",
        "ScreenFader.isFading",
    };

    /// <summary>The types we wrote, without the ones the compiler and Unity generate.</summary>
    private static IEnumerable<Type> OurTypes(Assembly assembly)
    {
        return assembly.GetTypes().Where(type => !type.Name.Contains("<") && !type.Name.StartsWith("UnitySourceGenerated"));
    }

    private static IEnumerable<Type> TopLevelTypes(Assembly assembly)
    {
        return OurTypes(assembly).Where(type => !type.IsNested && type.Namespace == null);
    }

    private static string MemberName(FieldInfo field)
    {
        Match backing = Regex.Match(field.Name, @"^<(\w+)>k__BackingField$");
        return backing.Success ? backing.Groups[1].Value : field.Name;
    }

    [Test]
    public void TheGame_HasNoSingleton()
    {
        var problems = new List<string>();
        foreach (Type type in OurTypes(Game).Where(type => !type.IsEnum))
        {
            const BindingFlags statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            foreach (FieldInfo field in type.GetFields(statics).Where(field => field.FieldType == type))
            {
                problems.Add($"{type.Name}.{MemberName(field)}");
            }
            foreach (PropertyInfo property in type.GetProperties(statics).Where(property => property.PropertyType == type || property.Name == "Instance"))
            {
                problems.Add($"{type.Name}.{property.Name}");
            }
        }

        CollectionAssert.IsEmpty(problems, "A static field that holds its own type is a singleton");
    }

    [Test]
    public void EveryMutableStatic_IsInTheReadmeList()
    {
        var found = new List<string>();
        foreach (Type type in OurTypes(Game))
        {
            const BindingFlags statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            foreach (FieldInfo field in type.GetFields(statics))
            {
                if (field.IsLiteral)
                {
                    continue; // A const never changes.
                }
                bool canChange = !field.IsInitOnly || (!field.FieldType.IsValueType && field.FieldType != typeof(string));
                if (canChange)
                {
                    found.Add($"{type.Name}.{MemberName(field)}");
                }
            }
        }

        CollectionAssert.AreEquivalent(AllowedStatics, found, "A new static has to be explained in the README");
    }

    [Test]
    public void TheReadme_ExplainsEveryClassWithStaticState()
    {
        string readme = File.ReadAllText("README.md");
        int start = readme.IndexOf("## סינגלטון ומשתנים סטטיים", StringComparison.Ordinal);
        Assert.GreaterOrEqual(start, 0, "The README has a statics section");
        int end = readme.IndexOf("\n## ", start + 1, StringComparison.Ordinal);
        string section = end < 0 ? readme.Substring(start) : readme.Substring(start, end - start);

        StringAssert.Contains("אין סינגלטון", section);
        foreach (string owner in AllowedStatics.Select(name => name.Split('.')[0]).Distinct())
        {
            StringAssert.Contains(owner, section);
        }
    }

    [Test]
    public void ScreenFader_ResetsItsStaticOnEveryPlay_BecauseDomainReloadIsOff()
    {
        MethodInfo reset = typeof(ScreenFader).GetMethod("ResetStatics", BindingFlags.Static | BindingFlags.NonPublic);

        Assert.IsNotNull(reset);
        var attribute = reset.GetCustomAttribute<RuntimeInitializeOnLoadMethodAttribute>();
        Assert.IsNotNull(attribute);
        Assert.AreEqual(RuntimeInitializeLoadType.SubsystemRegistration, attribute.loadType);
    }

    // ---------- (d) One type per file, named like the file ----------

    private static readonly Regex TopLevelDeclaration = new Regex(
        @"^(?:public |internal )?(?:static |abstract |sealed |partial )*(?:class|struct|interface|enum) (\w+)", RegexOptions.Multiline);

    [TestCaseSource(typeof(SourceFiles), nameof(SourceFiles.All))]
    public void EveryFile_HasOneTopLevelTypeNamedLikeTheFile(string path)
    {
        string[] declared = TopLevelDeclaration.Matches(SourceFiles.Code(path)).Cast<Match>().Select(match => match.Groups[1].Value).ToArray();

        CollectionAssert.AreEqual(new[] { SourceFiles.TypeName(path) }, declared);
    }

    [Test]
    public void EveryType_HasItsOwnFile()
    {
        HashSet<string> files = new HashSet<string>(SourceFiles.All().Select(SourceFiles.TypeName));
        string[] withoutFile = TopLevelTypes(Game).Concat(TopLevelTypes(EditorTools)).Select(type => type.Name).Where(name => !files.Contains(name)).ToArray();

        CollectionAssert.IsEmpty(withoutFile);
    }

    [Test]
    public void EditorTools_AreOnlyInTheEditorFolder()
    {
        foreach (string path in SourceFiles.Runtime())
        {
            StringAssert.DoesNotContain("using UnityEditor", SourceFiles.Code(path), path + " would break the build");
        }
    }

    // ---------- (e) Every tuning number explains itself ----------

    private static IEnumerable<string> SerializedNumbersWithoutTooltip()
    {
        foreach (Type type in OurTypes(Game))
        {
            bool serializable = typeof(UnityEngine.Object).IsAssignableFrom(type) || type.IsDefined(typeof(SerializableAttribute), false);
            if (!serializable)
            {
                continue;
            }
            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                bool serialized = (field.IsPublic && !field.IsNotSerialized) || field.IsDefined(typeof(SerializeField), false);
                bool isNumber = field.FieldType == typeof(int) || field.FieldType == typeof(float);
                bool explained = field.IsDefined(typeof(TooltipAttribute), false) || field.IsDefined(typeof(UnityEngine.RangeAttribute), false) || field.IsDefined(typeof(MinAttribute), false);
                if (serialized && isNumber && !explained)
                {
                    yield return $"{type.Name}.{field.Name}";
                }
            }
        }
    }

    [Test]
    public void EverySerializedNumber_HasATooltip()
    {
        CollectionAssert.IsEmpty(SerializedNumbersWithoutTooltip().ToArray(), "Add a [Tooltip] so the number explains itself in the Inspector");
    }

    // ---------- (f) The README knows every class ----------

    [Test]
    public void TheReadme_MentionsEveryClass()
    {
        string readme = File.ReadAllText("README.md");
        string[] missing = TopLevelTypes(Game).Concat(TopLevelTypes(EditorTools))
            .Select(type => type.Name)
            .Where(name => !Regex.IsMatch(readme, @"\b" + name + @"\b"))
            .ToArray();

        CollectionAssert.IsEmpty(missing);
    }

    [Test]
    public void TheReadme_ListsEveryFolderOfScripts()
    {
        string readme = File.ReadAllText("README.md");
        foreach (string folder in Directory.GetDirectories(SourceFiles.ScriptsFolder).Select(Path.GetFileName))
        {
            StringAssert.Contains(folder + "/", readme);
        }
    }

    // ---------- (g) Objects are removed through ObjectRemoval ----------

    // Object.Destroy only logs an error outside Play mode. ObjectRemoval picks the right call,
    // so the scene builder and the EditMode tests can run the same code as the game.
    private static readonly Regex DirectDestroy = new Regex(@"(?<![\w.])(Object\.)?Destroy(Immediate)?\s*\(");

    [Test]
    public void GameCode_RemovesObjectsOnlyThroughObjectRemoval()
    {
        string[] direct = SourceFiles.Runtime()
            .Where(path => path != "Core/ObjectRemoval.cs")
            .Where(path => DirectDestroy.IsMatch(SourceFiles.Code(path)))
            .ToArray();

        CollectionAssert.IsEmpty(direct, "Call ObjectRemoval.Remove instead of Destroy");
    }
}
