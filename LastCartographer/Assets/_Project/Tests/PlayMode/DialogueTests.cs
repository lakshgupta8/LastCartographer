#nullable enable
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.Narrative;
using UnityEngine;
using UnityEngine.TestTools;
using Yarn.Unity;

namespace OWSBG.Tests
{
    /// <summary>Auto-advancing presenter that records lines and picks the option whose text contains a keyword.</summary>
    sealed class RecordingPresenter : DialoguePresenterBase
    {
        public readonly List<string> Lines = new List<string>();
        public string PreferOption = "";

        public override YarnTask OnDialogueStartedAsync() => YarnTask.CompletedTask;
        public override YarnTask OnDialogueCompleteAsync() => YarnTask.CompletedTask;

        public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            Lines.Add((line.CharacterName ?? "") + "|" + line.TextWithoutCharacterName.Text);
            return YarnTask.CompletedTask;
        }

        public override YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, LineCancellationToken cancellationToken)
        {
            foreach (var o in dialogueOptions)
                if (o.IsAvailable && o.Line.TextWithoutCharacterName.Text.Contains(PreferOption))
                    return YarnTask<DialogueOption?>.FromResult(o);
            foreach (var o in dialogueOptions)
                if (o.IsAvailable) return YarnTask<DialogueOption?>.FromResult(o);
            return DialogueRunner.NoOptionSelected;
        }
    }

    /// <summary>
    /// The dialogue runtime end to end: a script compiled at runtime, run through DialogueService,
    /// with the &lt;&lt;flag&gt;&gt; command and $variables landing in WorldState, and options resolving.
    /// </summary>
    public class DialogueTests
    {
        const string Script = @"
title: Start
---
<<declare $met_sable = false>>
Sable: Wings high, stranger.
<<flag test.started 1>>
-> Where is this?
    Sable: The Saltmarrow.
    <<flag test.choice 1>>
-> ...
    Sable: Quiet one.
    <<flag test.choice 2>>
<<set $met_sable = true>>
Sable: Everything here has a price.
===
";

        GameObject? _go;

        [SetUp]
        public void SetUp() { GameState.NewGame(); }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.Destroy(_go);
            GameState.NewGame();
        }

        [UnityTest]
        public IEnumerator ScriptRunsCommandsSetFlagsAndVariablesPersistToWorldState()
        {
            var project = RuntimeYarnBuilder.Build(Script);
            _go = new GameObject("Dialogue");
            var presenter = _go.AddComponent<RecordingPresenter>();
            presenter.PreferOption = "Where";
            var service = _go.AddComponent<DialogueService>();
            service.Initialize(project, presenter);

            bool completed = false;
            service.Completed += () => completed = true;

            Assert.IsTrue(service.StartNode("Start"), "node should exist and start");
            float t = 0f;
            while (!completed && t < 5f) { t += Time.deltaTime; yield return null; }

            Assert.IsTrue(completed, "dialogue should complete");
            Assert.AreEqual(1, GameState.World.Get("test.started"), "<<flag>> command should write WorldState");
            Assert.AreEqual(1, GameState.World.Get("test.choice"), "chosen option branch should run");
            Assert.IsTrue(GameState.World.Is("$met_sable"), "$variables should land in WorldState flags");
            Assert.AreEqual(3, presenter.Lines.Count, "three lines on the chosen path: " + string.Join(" / ", presenter.Lines));
            StringAssert.StartsWith("Sable|", presenter.Lines[0]);
            Assert.IsFalse(service.IsRunning);
        }

        [UnityTest]
        public IEnumerator UnknownNodeIsRefusedWithoutStarting()
        {
            var project = RuntimeYarnBuilder.Build(Script);
            _go = new GameObject("Dialogue");
            var presenter = _go.AddComponent<RecordingPresenter>();
            var service = _go.AddComponent<DialogueService>();
            service.Initialize(project, presenter);
            yield return null;
            Assert.IsFalse(service.StartNode("Nope"));
            Assert.IsFalse(service.IsRunning);
        }
    }
}
