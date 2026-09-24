using System.Collections;
using NUnit.Framework;
using PortfolioMagnetics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PortfolioLab.Tests
{
    // Bunlar otomatik sistem kontrolleridir; gerçek oyuncu testlerinin yerine geçmez.
    public class RelayRoomPlayModeTests
    {
        private const string FirstScene = "03_FirstContact";
        private const string SecondScene = "04_DoubleRelay";
        private const float CaptureTimeoutSeconds = 12f;
        private RelayRoom room;
        private Keyboard simulatedKeyboard;
        private bool inputSettingsOverridden;
        private InputSettings.BackgroundBehavior originalBackgroundBehavior;
#if UNITY_EDITOR
        private InputSettings.EditorInputBehaviorInPlayMode originalEditorInputBehavior;
#endif

        [UnityTest]
        public IEnumerator FirstContact_MagnetPowersGate_AndRestartRestoresEntireRoom()
        {
            yield return LoadRoom(FirstScene);
            Assert.That(room.sockets.Length, Is.EqualTo(1));
            RoomSnapshot initial = new RoomSnapshot(room);
            room.Begin();
            room.magnets[0].SetActive(true);

            yield return WaitForCapture(room.sockets[0]);
            yield return null; // Kapının Update adımının da çalışmasını bekle.
            AssertGateOpen(room.gates[0]);
            Assert.That(room.balls[0].Captured, Is.True);
            Assert.That(room.balls[0].Body.isKinematic, Is.True);
            Assert.That(room.balls[0].GetComponent<SphereCollider>().enabled, Is.False);

            room.player.Body.position += Vector3.right * 1.5f;
            room.RestartRoom();
            AssertFullyReset(initial, 2);
        }

        [UnityTest]
        public IEnumerator UnpoweredRoom_CannotComplete_EvenWhenPlayerIsAtExit()
        {
            yield return LoadRoom(FirstScene);
            room.Begin();
            room.player.Body.position = room.exitPoint.position;
            room.player.transform.position = room.exitPoint.position;
            room.CompleteRoom();
            yield return null;

            Assert.That(room.PoweredCount, Is.Zero);
            Assert.That(room.State, Is.EqualTo(RelayRoom.RoomState.Playing),
                "Çıkışa ulaşmak, devreyi açmadan bölümü bitirmemeli.");
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator PausedRoom_DoesNotAdvancePhysics_AndResumesCapture()
        {
            yield return LoadRoom(FirstScene);
            room.Begin();
            yield return new WaitForFixedUpdate();
            PrepareSimulatedKeyboard();

            // Gerçek kullanıcının ESC akışını dene; özel bir test durumu yaratma.
            InputSystem.QueueStateEvent(simulatedKeyboard, new KeyboardState(Key.Escape));
            for (int frame = 0; frame < 3 && room.State != RelayRoom.RoomState.Paused; frame++)
                yield return null;
            Assert.That(room.State, Is.EqualTo(RelayRoom.RoomState.Paused),
                "ESC üç Update karesinde duraklatmalı. Tuş: " + simulatedKeyboard.escapeKey.isPressed +
                "; güncel klavye: " + (Keyboard.current == simulatedKeyboard));
            Assert.That(Time.timeScale, Is.Zero);
            InputSystem.QueueStateEvent(simulatedKeyboard, new KeyboardState());
            yield return null;

            Vector3 ballBefore = room.balls[0].Body.position;
            Vector3 playerBefore = room.player.Body.position;
            room.magnets[0].SetActive(true);
            yield return new WaitForSecondsRealtime(0.25f);
            AssertPosition(room.balls[0].Body.position, ballBefore, "Duraklatılan top");
            AssertPosition(room.player.Body.position, playerBefore, "Duraklatılan karakter");
            Assert.That(room.PoweredCount, Is.Zero);

            room.Begin();
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            yield return WaitForCapture(room.sockets[0]);
            Assert.That(room.PoweredCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DoubleRelay_RequiresBothCircuits_AndResetsBothAfterCompletion()
        {
            yield return LoadRoom(SecondScene);
            Assert.That(room.sockets.Length, Is.EqualTo(2));
            Assert.That(room.magnets.Length, Is.EqualTo(2));
            RoomSnapshot initial = new RoomSnapshot(room);
            room.Begin();
            room.magnets[0].SetActive(true);
            yield return WaitForCapture(room.sockets[0]);
            yield return null;

            Assert.That(room.PoweredCount, Is.EqualTo(1));
            AssertGateOpen(room.gates[0]);
            Assert.That(room.gates[1].IsOpen, Is.False);
            room.CompleteRoom();
            Assert.That(room.State, Is.EqualTo(RelayRoom.RoomState.Playing),
                "İkinci bölüm yalnızca tek devreyle tamamlanmamalı.");

            room.magnets[1].SetActive(true);
            yield return WaitForCapture(room.sockets[1]);
            yield return null;
            Assert.That(room.PoweredCount, Is.EqualTo(2));
            AssertGateOpen(room.gates[1]);
            room.CompleteRoom();
            Assert.That(room.State, Is.EqualTo(RelayRoom.RoomState.Complete));
            Assert.That(Time.timeScale, Is.Zero);

            room.RestartRoom();
            AssertFullyReset(initial, 2);
        }

        [UnityTest]
        public IEnumerator CompletedFirstRoom_ContinueStartsDoubleRelayWithoutAnotherIntroduction()
        {
            yield return LoadRoom(FirstScene);
            room.Begin();
            room.magnets[0].SetActive(true);
            yield return WaitForCapture(room.sockets[0]);
            room.CompleteRoom();
            Assert.That(room.State, Is.EqualTo(RelayRoom.RoomState.Complete));

            room.Continue();
            yield return null;
            yield return null; // Yeni odanın Start adımı tamamlanmalı.
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SecondScene));
            room = Object.FindFirstObjectByType<RelayRoom>();
            Assert.That(room, Is.Not.Null);
            Assert.That(room.showIntroduction, Is.False);
            Assert.That(room.State, Is.EqualTo(RelayRoom.RoomState.Playing));
            Assert.That(room.PoweredCount, Is.Zero);
            Assert.That(room.Attempts, Is.EqualTo(1));
            Assert.That(room.sockets.Length, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator FallingPlayer_RequestsFullRoomRestart_IncludingCapturedBall()
        {
            yield return LoadRoom(FirstScene);
            RoomSnapshot initial = new RoomSnapshot(room);
            room.Begin();
            room.magnets[0].SetActive(true);
            yield return WaitForCapture(room.sockets[0]);
            Assert.That(room.player.ManagedRestart, Is.True);

            Vector3 belowLevel = initial.PlayerPosition + Vector3.down * 100f;
            room.player.Body.position = belowLevel;
            room.player.transform.position = belowLevel;
            float deadline = Time.realtimeSinceStartup + 2f;
            while (room.Attempts == 1 && Time.realtimeSinceStartup < deadline)
                yield return new WaitForFixedUpdate();

            Assert.That(room.Attempts, Is.EqualTo(2), "Düşüş tam oda sıfırlamasını tetiklemeli.");
            // Fizik sıfırlama karesinde yerçekimi bir kez uygulanabilir.
            AssertFullyReset(initial, 2, 0.03f);
        }

        [UnityTest]
        public IEnumerator KeyboardMovement_WalksStopsAndJumps_WithoutAnExtraAirJump()
        {
            yield return LoadRoom(FirstScene);
            room.Begin();
            PrepareSimulatedKeyboard();
            for (int step = 0; step < 100 && !room.player.IsGrounded; step++)
                yield return new WaitForFixedUpdate();
            Assert.That(room.player.IsGrounded, Is.True, "Karakter başlangıç zeminine oturmalı.");

            Vector3 start = room.player.Body.position;
            InputSystem.QueueStateEvent(simulatedKeyboard, new KeyboardState(Key.D));
            yield return null;
            yield return null;
            for (int step = 0; step < 10; step++) yield return new WaitForFixedUpdate();
            InputSystem.QueueStateEvent(simulatedKeyboard, new KeyboardState());
            yield return null;
            yield return null;
            yield return new WaitForFixedUpdate();
            Assert.That(room.player.Body.position.x - start.x, Is.GreaterThan(0.4f));
            Assert.That(Mathf.Abs(room.player.Body.linearVelocity.x), Is.LessThan(0.01f),
                "Yön tuşu bırakılınca karakter yatayda durmalı.");

            float groundHeight = room.player.Body.position.y;
            InputSystem.QueueStateEvent(simulatedKeyboard, new KeyboardState(Key.Space));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(simulatedKeyboard, new KeyboardState());
            for (int step = 0; step < 8; step++) yield return new WaitForFixedUpdate();
            Assert.That(room.player.Body.position.y, Is.GreaterThan(groundHeight + 0.3f));
            Assert.That(room.player.IsGrounded, Is.False);

            float upwardSpeed = room.player.Body.linearVelocity.y;
            InputSystem.QueueStateEvent(simulatedKeyboard, new KeyboardState(Key.Space));
            yield return null;
            yield return null;
            yield return new WaitForFixedUpdate();
            Assert.That(room.player.Body.linearVelocity.y, Is.LessThanOrEqualTo(upwardSpeed + 0.05f),
                "Havadaki ikinci Space yeni bir yukarı hız vermemeli.");
            InputSystem.QueueStateEvent(simulatedKeyboard, new KeyboardState());
            for (int step = 0; step < 100 && !room.player.IsGrounded; step++)
                yield return new WaitForFixedUpdate();
            Assert.That(room.player.IsGrounded, Is.True, "Karakter tekrar zemine inmeli.");
        }

        private IEnumerator LoadRoom(string sceneName)
        {
            Time.timeScale = 1f;
            AsyncOperation loading = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            Assert.That(loading, Is.Not.Null, sceneName + " build sahne listesinde bulunmalı.");
            yield return loading;
            yield return null;
            room = Object.FindFirstObjectByType<RelayRoom>();
            Assert.That(room, Is.Not.Null, sceneName + " içinde RelayRoom bulunamadı.");
            Assert.That(room.player, Is.Not.Null);
            Assert.That(room.exitPoint, Is.Not.Null);
            Assert.That(room.sockets, Is.Not.Empty);
            Assert.That(room.magnets.Length, Is.EqualTo(room.sockets.Length));
            Assert.That(room.balls.Length, Is.EqualTo(room.sockets.Length));
            Assert.That(room.gates.Length, Is.EqualTo(room.sockets.Length));
            Assert.That(room.State, Is.EqualTo(room.showIntroduction
                ? RelayRoom.RoomState.Introduction : RelayRoom.RoomState.Playing));
        }

        private void PrepareSimulatedKeyboard()
        {
            // Batch testinde odaklı Game penceresi yok. Yalnızca test süresince
            // sanal klavyenin oyun döngüsüne gitmesini sağla; gerçek Update akışı kullanılır.
            originalBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            originalEditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            inputSettingsOverridden = true;
            simulatedKeyboard = InputSystem.AddDevice<Keyboard>();
            simulatedKeyboard.MakeCurrent();
        }

        private IEnumerator WaitForCapture(GoalSocket socket)
        {
            float deadline = Time.realtimeSinceStartup + CaptureTimeoutSeconds;
            int attemptsBefore = room.Attempts;
            while (!socket.IsSatisfied && Time.realtimeSinceStartup < deadline)
            {
                Assert.That(Time.timeScale, Is.GreaterThan(0f), "Yakalama sırasında fizik durdu.");
                Assert.That(room.Attempts, Is.EqualTo(attemptsBefore),
                    "Top hedefe ulaşmadan oda beklenmedik biçimde sıfırlandı.");
                yield return new WaitForFixedUpdate();
            }
            Vector3 target = socket.capturePoint != null ? socket.capturePoint.position : socket.transform.position;
            Assert.That(socket.IsSatisfied, Is.True,
                "Mıknatıs 12 saniyede topu yakalayamadı. Top: " + socket.targetBall.Body.position +
                "; hedef: " + target + "; uzaklık: " + Vector3.Distance(socket.targetBall.Body.position, target));
        }

        private static void AssertGateOpen(RelayGate gate)
        {
            Assert.That(gate.IsOpen, Is.True);
            Collider blocker = gate.panel.GetComponent<Collider>();
            Assert.That(blocker, Is.Not.Null, "Kapının engelleyici çarpışma yüzeyi olmalı.");
            Assert.That(blocker.enabled, Is.False, "Açık kapı karakterin yolunu kesmemeli.");
        }

        private void AssertFullyReset(RoomSnapshot initial, int expectedAttempts, float tolerance = 0.002f)
        {
            Assert.That(room.State, Is.EqualTo(RelayRoom.RoomState.Playing));
            Assert.That(room.Attempts, Is.EqualTo(expectedAttempts));
            Assert.That(room.PoweredCount, Is.Zero);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            AssertPosition(room.player.Body.position, initial.PlayerPosition, "Karakter başlangıcı", tolerance);
            Assert.That(room.player.Body.isKinematic, Is.False);
            for (int i = 0; i < room.balls.Length; i++)
            {
                Assert.That(room.sockets[i].IsSatisfied, Is.False, "Yuva " + i);
                Assert.That(room.balls[i].Captured, Is.False, "Top " + i);
                Assert.That(room.balls[i].Body.isKinematic, Is.False, "Top fiziği " + i);
                Assert.That(room.balls[i].GetComponent<SphereCollider>().enabled, Is.True);
                Assert.That(room.balls[i].Body.collisionDetectionMode,
                    Is.EqualTo(CollisionDetectionMode.ContinuousDynamic));
                AssertPosition(room.balls[i].Body.position, initial.BallPositions[i], "Top başlangıcı " + i, tolerance);
                Assert.That(room.magnets[i].IsActive, Is.False, "Mıknatıs " + i);
                Assert.That(room.gates[i].IsOpen, Is.False, "Kapı " + i);
                Assert.That(room.gates[i].panel.GetComponent<Collider>().enabled, Is.True);
                AssertPosition(room.gates[i].panel.localPosition, initial.GatePositions[i], "Kapı başlangıcı " + i, tolerance);
            }
        }

        private static void AssertPosition(Vector3 actual, Vector3 expected, string message, float tolerance = 0.002f)
            => Assert.That(Vector3.Distance(actual, expected), Is.LessThanOrEqualTo(tolerance), message);

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            if (simulatedKeyboard != null && simulatedKeyboard.added)
                InputSystem.RemoveDevice(simulatedKeyboard);
            simulatedKeyboard = null;
            if (inputSettingsOverridden)
            {
                InputSystem.settings.backgroundBehavior = originalBackgroundBehavior;
#if UNITY_EDITOR
                InputSystem.settings.editorInputBehaviorInPlayMode = originalEditorInputBehavior;
#endif
                inputSettingsOverridden = false;
            }
            Time.timeScale = 1f;
            if (room != null)
            {
                // Bir testteki aktif mıknatıs bir sonraki testi etkilemesin.
                foreach (var magnet in room.magnets)
                    if (magnet != null) magnet.SetActive(false);
            }
            yield return null;
            Time.timeScale = 1f;
        }

        private sealed class RoomSnapshot
        {
            public readonly Vector3 PlayerPosition;
            public readonly Vector3[] BallPositions;
            public readonly Vector3[] GatePositions;

            public RoomSnapshot(RelayRoom source)
            {
                // İkinci oda hemen oynadığı için birkaç fizik adımı geçmiş olabilir.
                // Beklenen konumları, asıl başlangıca sıfırladıktan sonra kaydet.
                source.player.ResetToSpawn();
                foreach (var ball in source.balls) ball.ResetBall();
                foreach (var gate in source.gates) gate.ResetGate();
                PlayerPosition = source.player.Body.position;
                BallPositions = new Vector3[source.balls.Length];
                GatePositions = new Vector3[source.gates.Length];
                for (int i = 0; i < BallPositions.Length; i++)
                    BallPositions[i] = source.balls[i].Body.position;
                for (int i = 0; i < GatePositions.Length; i++)
                    GatePositions[i] = source.gates[i].panel.localPosition;
            }
        }
    }
}
