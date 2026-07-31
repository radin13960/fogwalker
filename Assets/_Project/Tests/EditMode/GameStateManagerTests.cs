using System.Collections.Generic;
using FogWalker.Core;
using NUnit.Framework;
using UnityEngine;

namespace FogWalker.Tests.EditMode
{
    /// <summary>
    /// تست ماشین‌حالت بازی: مسیرهای مجاز، رد Transition نامعتبر، timeScale و دروازه ورودی.
    /// </summary>
    public class GameStateManagerTests
    {
        [TearDown]
        public void TearDown()
        {
            // تمیزکاری سراسری: هرگز timeScale را صفر رها نکن!
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        [Test]
        public void InitialState_IsBootstrap()
        {
            var manager = new GameStateManager();
            Assert.AreEqual(GameState.Bootstrap, manager.Current);
        }

        [Test]
        public void LegalFlow_TransitionsWork_AndEventsFire()
        {
            var fired = new List<(GameState prev, GameState next)>();
            var manager = new GameStateManager();
            manager.OnStateChanged += (prev, next) => fired.Add((prev, next));

            Assert.IsTrue(manager.SetState(GameState.MainMenu));
            Assert.IsTrue(manager.SetState(GameState.Loading));
            Assert.IsTrue(manager.SetState(GameState.Playing));
            Assert.IsTrue(manager.SetState(GameState.Paused));
            Assert.IsTrue(manager.SetState(GameState.Playing));

            Assert.AreEqual(GameState.Playing, manager.Current);
            Assert.AreEqual(5, fired.Count);
            Assert.AreEqual(GameState.Paused, fired[4].prev);
        }

        [Test]
        public void IllegalTransition_IsBlocked_WithoutSideEffects()
        {
            bool gateCalled = false;
            var manager = new GameStateManager(_ => gateCalled = true);

            bool result = manager.SetState(GameState.Playing); // Bootstrap -> Playing مجاز نیست

            Assert.IsFalse(result);
            Assert.AreEqual(GameState.Bootstrap, manager.Current);
            Assert.IsFalse(gateCalled, "Transition نامعتبر نباید اثر جانبی (دروازه ورودی) داشته باشد.");
        }

        [Test]
        public void SameState_IsNoOp()
        {
            int eventCount = 0;
            var manager = new GameStateManager();
            manager.OnStateChanged += (_, __) => eventCount++;

            Assert.IsFalse(manager.SetState(GameState.Bootstrap));
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void Pause_SetsTimeScaleZero_Resume_Restores()
        {
            var manager = new GameStateManager();
            manager.SetState(GameState.MainMenu);
            manager.SetState(GameState.Loading);
            manager.SetState(GameState.Playing);

            manager.SetState(GameState.Paused);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(AudioListener.pause);

            manager.SetState(GameState.Playing);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(AudioListener.pause);
        }

        [Test]
        public void InputGate_EnabledOnlyInPlaying()
        {
            var gateLog = new List<bool>();
            var manager = new GameStateManager(v => gateLog.Add(v));

            manager.SetState(GameState.MainMenu);   // خاموش
            manager.SetState(GameState.Loading);    // خاموش
            manager.SetState(GameState.Playing);    // روشن
            manager.SetState(GameState.Paused);     // خاموش

            CollectionAssert.AreEqual(new[] { false, false, true, false }, gateLog);
        }

        [Test]
        public void IsGameplayActive_TrueOnlyWhenPlaying()
        {
            var manager = new GameStateManager();
            Assert.IsFalse(manager.IsGameplayActive);
            manager.SetState(GameState.MainMenu);
            manager.SetState(GameState.Loading);
            manager.SetState(GameState.Playing);
            Assert.IsTrue(manager.IsGameplayActive);
            manager.SetState(GameState.LevelComplete);
            Assert.IsFalse(manager.IsGameplayActive);
        }
    }
}
