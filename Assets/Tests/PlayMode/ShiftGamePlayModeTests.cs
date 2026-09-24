using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using ShiftGame;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PortfolioLab.Tests
{
    // Bileşen testlerinde Teleport yalnızca önkoşulları hazırlar; rota kanıtı değildir.
    // PhysicalTraversal testleri ise doğduktan sonra yalnızca klavye ve fizik kullanır.
    // Hiçbiri gerçek hedef oyuncularla yapılan kullanılabilirlik testinin yerine geçmez.
    public class ShiftGamePlayModeTests
    {
        private const string FirstScene = "05_ShiftFirstShift";
        private const string SecondScene = "06_ShiftPrepareTheWay";
        private ShiftRoom room;
        private readonly List<GameObject> fixtures = new List<GameObject>();
        private Keyboard keyboard;
        private bool inputSettingsChanged;
        private InputSettings.BackgroundBehavior previousBackgroundBehavior;
#if UNITY_EDITOR
        private InputSettings.EditorInputBehaviorInPlayMode previousEditorInputBehavior;
#endif

        [UnityTest]
        public IEnumerator Station_RequiresNearbyGroundedRobot_AndChangesItsMode()
        {
            yield return LoadRoom(FirstScene);
            room.Begin();
            CreateFloor(new Vector3(100, -0.5f, 0), new Vector3(20, 1, 4));
            ShiftRobot robot = CreateRobot(new Vector3(100, 1.05f, 0));
            ShiftStation station = CreateStation(new Vector3(100, 1, 0));
            robot.SetHeavy(false);
            yield return Settle(robot);
            Assert.That(station.CanUse(robot), Is.True);
            station.TryUse(robot);
            Assert.That(robot.IsHeavy, Is.True, "Yakındaki istasyon modu değiştirmeli.");

            robot.Teleport(new Vector3(104, 1.05f, 0));
            yield return Settle(robot);
            Assert.That(station.CanUse(robot), Is.False, "Uzaktan istasyon kullanılamamalı.");
            station.TryUse(robot);
            Assert.That(robot.IsHeavy, Is.True);

            robot.Teleport(new Vector3(100, 2.1f, 0));
            Assert.That(robot.IsGrounded, Is.False);
            Assert.That(station.CanUse(robot), Is.False, "Yakın olsa bile havada mod değişmemeli.");
            station.TryUse(robot);
            Assert.That(robot.IsHeavy, Is.True);
        }

        [UnityTest]
        public IEnumerator Piston_RequiresHeavyGroundedNearbyRobot_AndStaysLatchedAfterLeaving()
        {
            yield return LoadRoom(FirstScene);
            room.Begin();
            CreateFloor(new Vector3(100, -0.5f, 0), new Vector3(20, 1, 4));
            ShiftRobot robot = CreateRobot(new Vector3(100, 1.05f, 0));
            ShiftPiston piston = CreatePiston(new Vector3(100, 0.5f, 0));
            robot.SetHeavy(false);
            yield return Settle(robot);
            piston.TryActivate(robot);
            Assert.That(piston.IsLatched, Is.False, "Hafif mod pistonu kilitlememeli.");

            robot.SetHeavy(true);
            robot.Teleport(new Vector3(104, 1.05f, 0));
            yield return Settle(robot);
            piston.TryActivate(robot);
            Assert.That(piston.IsLatched, Is.False, "Ağır robot uzaktan etkinleştirememeli.");

            robot.Teleport(new Vector3(100, 1.8f, 0));
            piston.TryActivate(robot);
            Assert.That(piston.IsLatched, Is.False, "Havadaki robot pistonu kullanamamalı.");
            yield return Settle(robot);
            piston.TryActivate(robot);
            Assert.That(piston.IsLatched, Is.True);

            robot.Teleport(new Vector3(104, 1.05f, 0));
            yield return new WaitForFixedUpdate();
            Assert.That(piston.IsLatched, Is.True, "Üzerinden inmek kilidi geri almamalı.");
            piston.ResetPiston();
            Assert.That(piston.IsLatched, Is.False);
        }

        [UnityTest]
        public IEnumerator FragileSurface_SupportsLightRobot_BreaksUnderHeavyRobot_AndResets()
        {
            yield return LoadRoom(FirstScene);
            room.Begin();
            ShiftBreakable panel = CreatePanel(new Vector3(100, -0.5f, 0), new Vector3(5, 1, 4));
            ShiftRobot robot = CreateRobot(new Vector3(100, 1.05f, 0));
            robot.SetHeavy(false);
            yield return Settle(robot);
            for (int step = 0; step < 45; step++) yield return new WaitForFixedUpdate();
            Assert.That(panel.IsBroken, Is.False, "Hafif robot çatlak zeminde güvenle durmalı.");
            Assert.That(panel.surface.enabled, Is.True);

            robot.SetHeavy(true);
            for (int step = 0; step < 100 && !panel.IsBroken; step++)
                yield return new WaitForFixedUpdate();
            Assert.That(panel.IsBroken, Is.True, "Ağır robotun üst teması zemini kırmalı.");
            Assert.That(panel.surface.enabled, Is.False);
            robot.Teleport(new Vector3(108, 5, 0));
            panel.ResetPanel();
            Assert.That(panel.IsBroken, Is.False);
            Assert.That(panel.surface.enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator FragileSurface_HeavySideContactDoesNotBreakIt()
        {
            yield return LoadRoom(FirstScene);
            room.Begin();
            CreateFloor(new Vector3(100, -0.5f, 0), new Vector3(20, 1, 4));
            ShiftBreakable panel = CreatePanel(new Vector3(100, 2, 0), new Vector3(2, 4, 3));
            ShiftRobot robot = CreateRobot(new Vector3(98.55f, 1.05f, 0));
            robot.SetHeavy(true);
            yield return Settle(robot);
            for (int step = 0; step < 50; step++)
            {
                // Yan duvara teması fizik kuvvetiyle koru; üstüne ışınlama yok.
                robot.Body.AddForce(Vector3.right * 40f, ForceMode.Acceleration);
                yield return new WaitForFixedUpdate();
            }
            Assert.That(panel.IsBroken, Is.False, "Yandan ağır temas, üstten basma sayılmamalı.");
            Assert.That(panel.surface.enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator FallRespawn_PreservesPistonsGatesBridgeAndBrokenPanels_FullRestartClearsThem()
        {
            yield return LoadRoom(SecondScene);
            room.Begin();
            foreach (ShiftPiston piston in room.pistons) yield return ActivatePiston(piston);
            foreach (ShiftBreakable panel in room.panels) panel.Break();
            yield return null;
            AssertProgressComplete();

            room.player.Teleport(new Vector3(0, -100, 0));
            float deadline = Time.realtimeSinceStartup + 2f;
            while (room.player.Body.position.y < -50f && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(room.player.Body.position.y, Is.GreaterThan(-50f), "Düşüş güvenli doğma noktasına dönmeli.");
            AssertProgressComplete();
            foreach (ShiftBreakable panel in room.panels)
                Assert.That(panel.IsBroken, Is.True, "Düşüşten sonra kırık yolun ilerlemesi korunmalı.");

            room.RestartRoom();
            Assert.That(room.State, Is.EqualTo(ShiftRoom.RoomState.Playing));
            Assert.That(room.PoweredCount, Is.Zero);
            foreach (ShiftPiston piston in room.pistons) Assert.That(piston.IsLatched, Is.False);
            foreach (ShiftGate gate in room.gates)
            {
                Assert.That(gate.IsOpen, Is.False);
                Assert.That(gate.blocker.enabled, Is.True);
            }
            foreach (ShiftBridge bridge in room.bridges)
            {
                Assert.That(bridge.IsExtended, Is.False);
                Assert.That(bridge.surface.enabled, Is.False);
            }
            foreach (ShiftBreakable panel in room.panels)
            {
                Assert.That(panel.IsBroken, Is.False);
                Assert.That(panel.surface.enabled, Is.True);
            }
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator SecondRoom_RequiresBothPistonsAndPhysicalExitProximity()
        {
            yield return LoadRoom(SecondScene);
            room.Begin();
            Assert.That(room.pistons.Length, Is.EqualTo(2));
            room.player.Teleport(room.exitPoint.position);
            room.CompleteRoom();
            Assert.That(room.State, Is.EqualTo(ShiftRoom.RoomState.Playing));

            yield return ActivatePiston(room.pistons[0]);
            room.player.Teleport(room.exitPoint.position);
            room.CompleteRoom();
            Assert.That(room.PoweredCount, Is.EqualTo(1));
            Assert.That(room.State, Is.EqualTo(ShiftRoom.RoomState.Playing), "Tek piston yeterli olmamalı.");

            yield return ActivatePiston(room.pistons[1]);
            room.player.Teleport(room.stations[0].spawnPoint.position);
            room.CompleteRoom();
            Assert.That(room.State, Is.EqualTo(ShiftRoom.RoomState.Playing), "İki piston olsa bile uzaktan kazanılmamalı.");
            room.player.Teleport(room.exitPoint.position);
            room.CompleteRoom();
            Assert.That(room.State, Is.EqualTo(ShiftRoom.RoomState.Complete));
            Assert.That(room.player.ControlsEnabled, Is.False);
        }

        [UnityTest]
        public IEnumerator CompletedFirstRoom_ContinueStartsSecondRoom()
        {
            yield return LoadRoom(FirstScene);
            room.Begin();
            foreach (ShiftPiston piston in room.pistons) yield return ActivatePiston(piston);
            room.player.Teleport(room.exitPoint.position);
            room.CompleteRoom();
            Assert.That(room.State, Is.EqualTo(ShiftRoom.RoomState.Complete));
            room.Continue();
            yield return null;
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SecondScene));
            room = Object.FindFirstObjectByType<ShiftRoom>();
            Assert.That(room, Is.Not.Null);
            Assert.That(room.State, Is.EqualTo(ShiftRoom.RoomState.Playing));
            Assert.That(room.PoweredCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator EscapePausesRobotPhysics_AndResumesThroughSameKey()
        {
            yield return LoadRoom(FirstScene);
            room.Begin();
            PrepareKeyboard();
            yield return Settle(room.player);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            for (int frame = 0; frame < 3 && room.State != ShiftRoom.RoomState.Paused; frame++) yield return null;
            Assert.That(room.State, Is.EqualTo(ShiftRoom.RoomState.Paused));
            Assert.That(room.player.ControlsEnabled, Is.False);
            Assert.That(Time.timeScale, Is.Zero);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            yield return null;
            Vector3 position = room.player.Body.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.Space));
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(Vector3.Distance(room.player.Body.position, position), Is.LessThan(0.001f));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            for (int frame = 0; frame < 3 && room.State != ShiftRoom.RoomState.Playing; frame++) yield return null;
            Assert.That(room.State, Is.EqualTo(ShiftRoom.RoomState.Playing));
            Assert.That(room.player.ControlsEnabled, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest]
        [Category("PhysicalTraversal")]
        public IEnumerator FirstShift_PhysicalKeyboardRoute_CompletesWithoutTeleportOrRespawn()
        {
            yield return LoadRoom(FirstScene);
            room.Begin();
            PrepareKeyboard();
            int respawnsBefore = room.Respawns;
            yield return Settle(room.player);
            Assert.That(room.player.IsHeavy, Is.True);

            // Bu testte doğduktan sonra Teleport, SetHeavy veya doğrudan piston çağrısı yok.
            // Başlangıç istasyonu: E ile hafif, ardından gerçek platform zıplamaları.
            yield return PressE();
            Assert.That(room.player.IsHeavy, Is.False);
            yield return WalkToX(-7.8f);
            yield return JumpToX(-5f);
            yield return WalkToX(-3.9f);
            yield return JumpToX(-1f);
            yield return PressE();
            Assert.That(room.player.IsHeavy, Is.True);

            yield return WalkToX(2.5f);
            float deadline = Time.realtimeSinceStartup + 4f;
            while ((!room.panels[0].IsBroken || room.player.Body.position.y > -2f || !room.player.IsGrounded)
                && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(room.panels[0].IsBroken, Is.True);
            Assert.That(room.player.Body.position.y, Is.LessThan(-2f));
            Assert.That(room.player.IsGrounded, Is.True);
            yield return WalkToX(3f);
            yield return PressE();
            Assert.That(room.pistons[0].IsLatched, Is.True);

            yield return WalkToX(6.8f);
            yield return PressE();
            Assert.That(room.player.IsHeavy, Is.False);
            yield return WalkToX(8.3f);
            yield return JumpToX(10.7f);
            yield return WalkToX(11.8f);
            yield return JumpToX(14.5f);
            yield return WalkToX(15.7f);
            yield return JumpToX(18.5f);

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            deadline = Time.realtimeSinceStartup + 4f;
            while (room.State == ShiftRoom.RoomState.Playing && Time.realtimeSinceStartup < deadline)
                yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            Assert.That(room.State, Is.EqualTo(ShiftRoom.RoomState.Complete));
            Assert.That(room.Respawns, Is.EqualTo(respawnsBefore), "Gerçek rota düşüş/yeniden doğuş olmadan tamamlanmalı.");
        }

        [UnityTest]
        [Category("PhysicalTraversal")]
        public IEnumerator PrepareTheWay_PhysicalPreparedRoute_OpensBridgeBeforeDropping()
        {
            yield return LoadRoom(SecondScene);
            room.Begin();
            PrepareKeyboard();
            int respawnsBefore = room.Respawns;
            yield return Settle(room.player);
            yield return PressE(); // S0: hafif.
            Assert.That(room.player.IsHeavy, Is.False);
            yield return VisitUpperPistonAndReturnToS0();
            Assert.That(room.bridges[0].IsExtended, Is.True);
            Assert.That(room.pistons[1].IsLatched, Is.False);

            yield return PressE(); // S0: ağır, panelden alt odaya in.
            Assert.That(room.player.IsHeavy, Is.True);
            yield return DropToLowerRoom();
            yield return WalkToX(1f);
            yield return PressE();
            Assert.That(room.pistons[1].IsLatched, Is.True);
            yield return WalkToX(6f);
            yield return PressE(); // S2: hafif, çıkış basamaklarına hazırlan.
            Assert.That(room.player.IsHeavy, Is.False);
            yield return CrossBridgeAndFinish();
            Assert.That(room.Respawns, Is.EqualTo(respawnsBefore));
        }

        [UnityTest]
        [Category("PhysicalTraversal")]
        public IEnumerator PrepareTheWay_PhysicalEarlyDropRoute_RecoversWithoutResetAndPreservesP2()
        {
            yield return LoadRoom(SecondScene);
            room.Begin();
            PrepareKeyboard();
            int respawnsBefore = room.Respawns;
            int attemptsBefore = room.Attempts;
            yield return Settle(room.player);
            Assert.That(room.player.IsHeavy, Is.True);
            // Önce paneli kır: köprüyü hazırlamadan inmek bilinçli alternatif sıra.
            yield return DropToLowerRoom();
            yield return WalkToX(1f);
            yield return PressE();
            Assert.That(room.pistons[1].IsLatched, Is.True);
            Assert.That(room.gates[1].IsOpen, Is.True, "P2 kurtarma kapısını açmalı.");
            Assert.That(room.bridges[0].IsExtended, Is.False);
            yield return WalkToX(6f);
            yield return PressE(); // S2: kurtarma basamakları için hafif.
            Assert.That(room.player.IsHeavy, Is.False);

            yield return WalkToX(-5.8f);
            yield return JumpToX(-8.3f);
            yield return WalkToX(-9.3f);
            yield return JumpToX(-11.6f);
            yield return WalkToX(-12.2f);
            yield return JumpToX(-14.5f);
            yield return WalkToX(-14.3f);
            yield return JumpToX(-11f);
            yield return WalkToX(-10f);
            yield return Settle(room.player);
            Assert.That(room.pistons[1].IsLatched, Is.True, "Kurtarma turu P2 ilerlemesini korumalı.");

            yield return VisitUpperPistonAndReturnToS0();
            Assert.That(room.PoweredCount, Is.EqualTo(2));
            Assert.That(room.panels[0].IsBroken, Is.True);
            // F zaten açık. Hafif modda yeniden inilir; pistonun üzerinde durmak gerekmez.
            yield return DropToLowerRoom();
            yield return WalkToX(6f);
            yield return CrossBridgeAndFinish();
            Assert.That(room.Respawns, Is.EqualTo(respawnsBefore));
            Assert.That(room.Attempts, Is.EqualTo(attemptsBefore), "Erken iniş Reset gerektirmemeli.");
        }

        private IEnumerator VisitUpperPistonAndReturnToS0()
        {
            Assert.That(room.player.IsHeavy, Is.False);
            yield return JumpToX(-7.8f);
            yield return WalkToX(-7.1f);
            // İlk üst basamak, alttaki ağır yolun tavan açıklığı için yükseltilebilir.
            yield return JumpToX(-4.5f, 0.7f);
            yield return WalkToX(-3.8f);
            yield return JumpToX(-1f);
            yield return WalkToX(2f); // Alçak bakım cebine güvenle düş.
            yield return Settle(room.player);
            yield return PressE();
            Assert.That(room.player.IsHeavy, Is.True);
            yield return WalkToX(5f);
            yield return PressE();
            Assert.That(room.pistons[0].IsLatched, Is.True);
            yield return WalkToX(2f);
            yield return PressE(); // S1 hafif: bakım cebinden çıkabilmek için gerekli.
            Assert.That(room.player.IsHeavy, Is.False);
            yield return WalkToX(1.6f);
            yield return JumpToX(-1f);
            yield return WalkToX(-10f);
            yield return Settle(room.player);
        }

        private IEnumerator DropToLowerRoom()
        {
            yield return WalkToX(0.5f);
            float deadline = Time.realtimeSinceStartup + 4f;
            while ((!room.panels[0].IsBroken || room.player.Body.position.y > -2.5f || !room.player.IsGrounded)
                && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(room.panels[0].IsBroken, Is.True);
            Assert.That(room.player.Body.position.y, Is.LessThan(-2.5f));
            Assert.That(room.player.IsGrounded, Is.True);
        }

        private IEnumerator CrossBridgeAndFinish()
        {
            Assert.That(room.player.IsHeavy, Is.False);
            Assert.That(room.bridges[0].IsExtended, Is.True);
            yield return WalkToX(21f);
            yield return JumpToX(23.4f);
            yield return WalkToX(24.3f);
            yield return JumpToX(27f);
            yield return WalkToX(28.1f);
            yield return JumpToX(30.8f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            float deadline = Time.realtimeSinceStartup + 4f;
            while (room.State == ShiftRoom.RoomState.Playing && Time.realtimeSinceStartup < deadline)
                yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            Assert.That(room.State, Is.EqualTo(ShiftRoom.RoomState.Complete));
        }

        private IEnumerator PressE()
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            yield return null;
        }

        private IEnumerator WalkToX(float target)
        {
            float direction = Mathf.Sign(target - room.player.Body.position.x);
            InputSystem.QueueStateEvent(keyboard,
                new KeyboardState(direction > 0 ? Key.D : Key.A));
            float deadline = Time.realtimeSinceStartup + 5f;
            while (direction * (target - room.player.Body.position.x) > 0.08f
                && Time.realtimeSinceStartup < deadline && room.State == ShiftRoom.RoomState.Playing)
                yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            yield return null;
            Assert.That(Mathf.Abs(room.player.Body.position.x - target), Is.LessThan(0.45f),
                "Yürüme hedefi: " + target + "; konum: " + room.player.Body.position);
        }

        private IEnumerator JumpToX(float target, float minimumRise = 1.3f)
        {
            yield return Settle(room.player);
            float direction = Mathf.Sign(target - room.player.Body.position.x);
            Key movement = direction > 0 ? Key.D : Key.A;
            float startY = room.player.Body.position.y;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(movement, Key.Space));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(movement));
            bool rose = false;
            float deadline = Time.realtimeSinceStartup + 5f;
            while (direction * (target - room.player.Body.position.x) > 0.08f
                && Time.realtimeSinceStartup < deadline && room.State == ShiftRoom.RoomState.Playing)
            {
                rose |= room.player.Body.position.y > startY + 0.3f;
                yield return null;
            }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            yield return null;
            Assert.That(rose, Is.True, "Space karakteri gerçekten zıplatmalı.");
            Assert.That(Mathf.Abs(room.player.Body.position.x - target), Is.LessThan(0.45f));
            yield return Settle(room.player);
            Assert.That(room.player.Body.position.y, Is.GreaterThan(startY + minimumRise),
                "Hedefteki üst platforma inilmeli; yalnızca yatay konuma ulaşmak yeterli değil.");
        }

        private IEnumerator LoadRoom(string sceneName)
        {
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            yield return null;
            room = Object.FindFirstObjectByType<ShiftRoom>();
            Assert.That(room, Is.Not.Null, "Sahne ShiftRoom içermeli: " + sceneName);
            Assert.That(room.player, Is.Not.Null);
            Assert.That(room.stations, Is.Not.Empty);
            Assert.That(room.pistons, Is.Not.Empty);
            Assert.That(room.exitPoint, Is.Not.Null);
        }

        private IEnumerator Settle(ShiftRobot robot)
        {
            for (int step = 0; step < 100 && !robot.IsGrounded; step++) yield return new WaitForFixedUpdate();
            Assert.That(robot.IsGrounded, Is.True, "Robot iki fizik saniyesinde zemine oturmalı. Konum: " + robot.Body.position);
        }

        private IEnumerator ActivatePiston(ShiftPiston piston)
        {
            room.player.SetHeavy(true);
            // Sadece bileşen entegrasyonu için yerleştir. Bu, rota çözümü testi değildir.
            room.player.Teleport(piston.transform.position + Vector3.up * 1.1f);
            yield return Settle(room.player);
            piston.TryActivate(room.player);
            Assert.That(piston.IsLatched, Is.True, "Ağır robot pistonun üstünde etkinleştirebilmeli: " + piston.pistonId);
        }

        private void AssertProgressComplete()
        {
            Assert.That(room.PoweredCount, Is.EqualTo(room.pistons.Length));
            foreach (ShiftPiston piston in room.pistons) Assert.That(piston.IsLatched, Is.True);
            foreach (ShiftGate gate in room.gates) Assert.That(gate.IsOpen, Is.True);
            foreach (ShiftBridge bridge in room.bridges) Assert.That(bridge.IsExtended, Is.True);
        }

        private GameObject Track(GameObject obj) { fixtures.Add(obj); return obj; }

        private void CreateFloor(Vector3 position, Vector3 scale)
        {
            GameObject obj = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            obj.name = "Test fixed floor";
            obj.transform.SetPositionAndRotation(position, Quaternion.identity);
            obj.transform.localScale = scale;
        }

        private ShiftRobot CreateRobot(Vector3 position)
        {
            GameObject obj = Track(GameObject.CreatePrimitive(PrimitiveType.Capsule));
            obj.name = "Test robot";
            obj.transform.position = position;
            return obj.AddComponent<ShiftRobot>();
        }

        private ShiftStation CreateStation(Vector3 position)
        {
            GameObject obj = Track(new GameObject("Test station"));
            obj.SetActive(false);
            obj.transform.position = position;
            ShiftStation station = obj.AddComponent<ShiftStation>();
            station.stationId = "TEST";
            station.spawnPoint = obj.transform;
            obj.SetActive(true);
            return station;
        }

        private ShiftPiston CreatePiston(Vector3 position)
        {
            GameObject obj = Track(new GameObject("Test piston"));
            obj.SetActive(false);
            obj.transform.position = position;
            ShiftPiston piston = obj.AddComponent<ShiftPiston>();
            piston.pistonId = "TEST";
            piston.gates = new ShiftGate[0];
            piston.bridges = new ShiftBridge[0];
            GameObject plunger = new GameObject("Test plunger");
            plunger.transform.SetParent(obj.transform, false);
            piston.plunger = plunger.transform;
            obj.SetActive(true);
            return piston;
        }

        private ShiftBreakable CreatePanel(Vector3 position, Vector3 scale)
        {
            GameObject obj = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            obj.SetActive(false);
            obj.transform.position = position;
            obj.transform.localScale = scale;
            ShiftBreakable panel = obj.AddComponent<ShiftBreakable>();
            panel.surface = obj.GetComponent<Collider>();
            panel.visuals = new[] { obj.GetComponent<Renderer>() };
            obj.SetActive(true);
            return panel;
        }

        private void PrepareKeyboard()
        {
            previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            previousEditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            inputSettingsChanged = true;
            keyboard = InputSystem.AddDevice<Keyboard>();
            keyboard.MakeCurrent();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1f;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            keyboard = null;
            if (inputSettingsChanged)
            {
                InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
#if UNITY_EDITOR
                InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInputBehavior;
#endif
                inputSettingsChanged = false;
            }
            foreach (GameObject obj in fixtures) if (obj != null) Object.Destroy(obj);
            fixtures.Clear();
            yield return null;
            Time.timeScale = 1f;
        }
    }
}
