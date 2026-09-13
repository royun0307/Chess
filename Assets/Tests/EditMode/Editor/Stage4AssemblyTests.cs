using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Stage4AssemblyTests
{
    [Test]
    public void RulesAndSessionBelongToUnityIndependentCore()
    {
        foreach (var type in new[] { typeof(Board), typeof(Position), typeof(Counting), typeof(Piece),
            typeof(Pawn), typeof(King), typeof(Move), typeof(GameState), typeof(GameSession), typeof(StateString) })
            Assert.AreEqual("Chess.Core", type.Assembly.GetName().Name, type.FullName);
        AssertNoUnityOrPresentationReferences(typeof(Board).Assembly);
    }

    [Test]
    public void EngineDependsOnCoreWithoutUnityOrTraining()
    {
        Assert.AreEqual("Chess.Engine", typeof(SimpleChessEngine).Assembly.GetName().Name);
        Assert.AreSame(typeof(SimpleChessEngine).Assembly, typeof(IChessEngine).Assembly);
        AssertNoUnityOrPresentationReferences(typeof(SimpleChessEngine).Assembly);
        CollectionAssert.Contains(typeof(SimpleChessEngine).Assembly.GetReferencedAssemblies().Select(a => a.Name), "Chess.Core");
    }

    private static void AssertNoUnityOrPresentationReferences(System.Reflection.Assembly assembly)
    {
        foreach (var reference in assembly.GetReferencedAssemblies())
        {
            Assert.IsFalse(reference.Name.StartsWith("Unity", StringComparison.Ordinal), reference.Name);
            Assert.IsFalse(new[] { "Chess.Runtime", "Chess.Training", "Chess.Editor", "Chess.Tests.EditMode" }.Contains(reference.Name), reference.Name);
        }
    }

    [Test]
    public void ViewsAndManagersBelongToRuntimeAndDoNotDependOnTraining()
    {
        foreach (var type in new[] { typeof(GameManager), typeof(BoardManager), typeof(EngineManager),
            typeof(UIManager), typeof(PromotionUI), typeof(Chessman), typeof(MovePlate) })
            Assert.AreEqual("Chess.Runtime", type.Assembly.GetName().Name, type.FullName);
        var references = typeof(GameManager).Assembly.GetReferencedAssemblies().Select(a => a.Name).ToArray();
        CollectionAssert.DoesNotContain(references, "Chess.Training");
        CollectionAssert.DoesNotContain(references, "Unity.ML-Agents");
        CollectionAssert.DoesNotContain(references, "Chess.Tests.EditMode");
    }

    [Test]
    public void TrainingHasItsOwnAssemblyWithoutRuntimeDependency()
    {
        Assert.AreEqual("Chess.Training", typeof(ChessAgent).Assembly.GetName().Name);
        var references = typeof(ChessAgent).Assembly.GetReferencedAssemblies().Select(a => a.Name).ToArray();
        CollectionAssert.Contains(references, "Chess.Core");
        CollectionAssert.Contains(references, "Unity.ML-Agents");
        CollectionAssert.DoesNotContain(references, "Chess.Runtime");
        CollectionAssert.DoesNotContain(references, "Chess.Engine");
    }

    [Test]
    public void EditorAndTestAssembliesAreExcludedFromPlayerCompilation()
    {
        Assert.AreEqual("Chess.Tests.EditMode", typeof(Stage4AssemblyTests).Assembly.GetName().Name);
        Assert.AreEqual("Chess.Editor.dll", CompilationPipeline.GetAssemblyNameFromScriptPath("Assets/Editor/Stage0ValidationBuild.cs"));
        var player = CompilationPipeline.GetAssemblies(AssembliesType.Player);
        CollectionAssert.DoesNotContain(player.Select(a => a.name), "Chess.Editor");
        CollectionAssert.DoesNotContain(player.Select(a => a.name), "Chess.Tests.EditMode");
        foreach (string name in new[] { "Chess.Core", "Chess.Engine", "Chess.Runtime", "Chess.Training" })
            CollectionAssert.Contains(player.Select(a => a.name), name);
        foreach (var assembly in player.Where(a => a.name.StartsWith("Chess.", StringComparison.Ordinal)))
        {
            Assert.IsFalse(assembly.sourceFiles.Any(p => p.Replace('\\', '/').Contains("/Tests/")), assembly.name);
            Assert.IsFalse(assembly.assemblyReferences.Any(a => a.name == "Chess.Tests.EditMode" || a.name == "Chess.Editor"), assembly.name);
        }
    }

    [TestCase("Assets/Scripts/Manager/Chessman.cs", "58fc8b2e10800f5418060795ddf2cdee", typeof(Chessman))]
    [TestCase("Assets/Scripts/Manager/MovePlate.cs", "ea18366895cb1394487455d25a896c1b", typeof(MovePlate))]
    [TestCase("Assets/Scripts/Training/ChessAgent.cs", "6257546806fbe0a4da6c0de35792d72d", typeof(ChessAgent))]
    public void MovedScriptsKeepExistingAssetGuids(string path, string expectedGuid, Type expectedType)
    {
        Assert.AreEqual(expectedGuid, AssetDatabase.AssetPathToGUID(path));
        Assert.AreEqual(expectedType, AssetDatabase.LoadAssetAtPath<MonoScript>(path).GetClass());
    }

    [Test]
    public void AllChessPrefabsHaveNoMissingScripts()
    {
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
        Assert.IsNotEmpty(guids);
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            AssertNoMissingScripts(prefab, path);
        }
    }

    [TestCase("Assets/Scenes/MainScene.unity")]
    [TestCase("Assets/Scenes/TrainingScene.unity")]
    public void ScenesKeepScriptsAndTrainingCanInitialize(string path)
    {
        var scene = SceneManager.GetSceneByPath(path);
        bool alreadyLoaded = scene.IsValid() && scene.isLoaded;
        if (!alreadyLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            foreach (var root in scene.GetRootGameObjects()) AssertNoMissingScripts(root, path);
            if (path.EndsWith("TrainingScene.unity", StringComparison.Ordinal))
            {
                var agent = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ChessAgent>(true)).Single();
                agent.OnEpisodeBegin();
                var state = (GameState)typeof(ChessAgent).GetField("state", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(agent);
                Assert.AreEqual(PlayerColor.White, state.CurrentPlayer);
                Assert.AreEqual(20, state.AllLegalMovesFor(PlayerColor.White).Count());
                Assert.IsFalse(EditorBuildSettings.scenes.Any(s => s.path == path && s.enabled));
            }
        }
        finally
        {
            if (!alreadyLoaded) EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void AssertNoMissingScripts(GameObject root, string context)
    {
        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), context + ": " + transform.name);
    }
}
