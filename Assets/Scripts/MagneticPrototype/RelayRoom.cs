using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PortfolioMagnetics
{
    // Odanın durumu: başla, oyna, yeniden dene, çıkışa ulaş.
    public class RelayRoom : MonoBehaviour
    {
        public PlayerMovement player;
        public MagnetEmitter[] magnets;
        public Transform[] consoles;
        public MagneticBall[] balls;
        public GoalSocket[] sockets;
        public RelayGate[] gates;
        public Transform exitPoint;
        public string roomTitle = "01 / FIRST CONTACT";
        [TextArea] public string objective = "Power the receiver. Reach the exit.";
        public string nextScene;
        public bool showIntroduction = true;
        public float interactionDistance = 1.7f;
        public enum RoomState { Introduction, Playing, Paused, Complete }
        public RoomState State { get; private set; }
        public int Attempts { get; private set; } = 1;
        public int ActiveConsoleIndex { get; private set; } = -1;
        public int PoweredCount { get { int count = 0; foreach (var socket in sockets) if (socket.IsSatisfied) count++; return count; } }
        private float elapsed;
        private GUIStyle titleStyle, headingStyle, textStyle, smallStyle, buttonStyle, accentStyle;
        private Texture2D panelTexture, buttonTexture;
        private AudioSource sound;
        private AudioClip switchClip, captureClip, finishClip;
        private bool initializedStyles;

        private void Start()
        {
            player.ManagedRestart = true;
            player.RestartRequested += RestartRoom;
            foreach (var socket in sockets) socket.Captured += OnCapture;
            sound = gameObject.AddComponent<AudioSource>();
            sound.volume = 0.17f;
            switchClip = Tone(340, 0.09f);
            captureClip = Tone(620, 0.18f);
            finishClip = Tone(880, 0.25f);
            SetState(showIntroduction ? RoomState.Introduction : RoomState.Playing);
        }

        private void Update()
        {
            Keyboard keys = Keyboard.current;
            if (keys != null && keys.escapeKey.wasPressedThisFrame)
            {
                if (State == RoomState.Playing) SetState(RoomState.Paused);
                else if (State == RoomState.Paused) SetState(RoomState.Playing);
            }
            if (keys != null && keys.enterKey.wasPressedThisFrame)
            {
                if (State == RoomState.Introduction) Begin();
                else if (State == RoomState.Complete) Continue();
            }
            if (State != RoomState.Playing) return;
            elapsed += Time.deltaTime;
            ActiveConsoleIndex = FindNearestConsole();
            if (keys != null && keys.eKey.wasPressedThisFrame && ActiveConsoleIndex >= 0)
            {
                magnets[ActiveConsoleIndex].Toggle();
                sound.PlayOneShot(switchClip);
            }
            // Topun kaybı da aynı hızlı ve tam oda sıfırlamasını kullanır.
            foreach (var ball in balls)
            {
                if (ball.transform.position.y < -5f) { RestartRoom(); return; }
            }
            if (PoweredCount == sockets.Length && Vector3.Distance(player.transform.position, exitPoint.position) < 1.15f)
                CompleteRoom();
        }

        public void Begin() => SetState(RoomState.Playing);

        public void RestartRoom()
        {
            foreach (var socket in sockets) socket.ResetSocket();
            foreach (var ball in balls) ball.ResetBall();
            foreach (var magnet in magnets) magnet.ResetMagnet();
            foreach (var gate in gates) gate.ResetGate();
            player.ResetToSpawn();
            Attempts++;
            elapsed = 0f;
            ActiveConsoleIndex = -1;
            SetState(RoomState.Playing);
        }

        public void CompleteRoom()
        {
            if (State != RoomState.Playing || PoweredCount != sockets.Length) return;
            sound.PlayOneShot(finishClip);
            SetState(RoomState.Complete);
        }

        public void Continue()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(string.IsNullOrEmpty(nextScene) ? "03_FirstContact" : nextScene);
        }

        private void SetState(RoomState state)
        {
            State = state;
            Time.timeScale = state == RoomState.Playing ? 1f : 0f;
            player.SetControlsEnabled(state == RoomState.Playing);
        }

        private int FindNearestConsole()
        {
            int index = -1;
            float distance = interactionDistance;
            for (int i = 0; i < consoles.Length; i++)
            {
                float candidate = Vector3.Distance(player.transform.position, consoles[i].position);
                if (candidate < distance) { index = i; distance = candidate; }
            }
            return index;
        }

        private void OnCapture() { if (sound != null) sound.PlayOneShot(captureClip); }

        private void OnGUI()
        {
            SetupStyles();
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            GUI.DrawTexture(new Rect(0, 0, width, 94), panelTexture);
            GUI.Label(new Rect(32, 15, 750, 30), "POLAR RELAY  /  " + roomTitle, headingStyle);
            GUI.Label(new Rect(32, 50, width - 300, 28), objective, textStyle);
            GUI.Label(new Rect(width - 225, 23, 205, 34), "CIRCUITS  " + PoweredCount + " / " + sockets.Length, accentStyle);
            GUI.DrawTexture(new Rect(0, height - 46, width, 46), panelTexture);
            GUI.Label(new Rect(30, height - 35, width - 60, 28), "A / D  MOVE     SPACE  JUMP     E  MAGNET     R  RESTART ROOM     ESC  PAUSE", smallStyle);
            if (State == RoomState.Playing)
            {
                string prompt = PoweredCount == sockets.Length ? "POWER RESTORED  /  REACH THE EXIT →" : "Find the numbered control terminal.";
                if (ActiveConsoleIndex >= 0)
                    prompt = "[ E ]  MAGNET " + (ActiveConsoleIndex + 1) + "  /  " + (magnets[ActiveConsoleIndex].IsActive ? "ON — switch off" : "OFF — switch on");
                GUI.Label(new Rect(30, height - 85, width - 60, 35), prompt, accentStyle);
                return;
            }
            float x = (width - 740) * 0.5f;
            float y = Mathf.Max(115, (height - 370) * 0.5f);
            GUI.DrawTexture(new Rect(x, y, 740, 360), panelTexture);
            string heading = State == RoomState.Introduction ? "POWER THE WAY FORWARD" : State == RoomState.Paused ? "TAKE A BREATH" : "CIRCUIT COMPLETE";
            GUI.Label(new Rect(x + 32, y + 30, 680, 58), heading, titleStyle);
            string body = State == RoomState.Introduction
                ? "The relay station is offline.\nReach each terminal and draw its metal core into the receiver.\nPowered receivers open the gates. Jump the gaps and find the exit."
                : State == RoomState.Paused ? "Your room is paused.\nResume where you left off, or reset the whole puzzle."
                : "The route is powered.\n" + (string.IsNullOrEmpty(nextScene) ? "Both rooms restored. Thanks for playing." : "Next: connect two separate circuits.") +
                  "\nTime: " + Mathf.FloorToInt(elapsed) + "s   /   Attempts: " + Attempts;
            GUI.Label(new Rect(x + 34, y + 104, 670, 126), body, textStyle);
            string primary = State == RoomState.Introduction ? "START  [ENTER]" : State == RoomState.Paused ? "RESUME" : string.IsNullOrEmpty(nextScene) ? "PLAY AGAIN  [ENTER]" : "NEXT ROOM  [ENTER]";
            if (GUI.Button(new Rect(x + 34, y + 252, 315, 52), primary, buttonStyle))
            {
                if (State == RoomState.Complete) Continue(); else Begin();
            }
            if (State != RoomState.Introduction && GUI.Button(new Rect(x + 373, y + 252, 330, 52), "RESTART THIS ROOM", buttonStyle)) RestartRoom();
        }

        private void SetupStyles()
        {
            if (initializedStyles) return;
            initializedStyles = true;
            panelTexture = Solid(new Color(0.025f, 0.055f, 0.09f, 0.95f));
            buttonTexture = Solid(new Color(0.08f, 0.40f, 0.43f));
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold };
            titleStyle.normal.textColor = Color.white;
            headingStyle = new GUIStyle(titleStyle) { fontSize = 21 };
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, wordWrap = true };
            textStyle.normal.textColor = new Color(0.83f, 0.9f, 0.94f);
            smallStyle = new GUIStyle(textStyle) { fontSize = 16 };
            accentStyle = new GUIStyle(textStyle) { fontSize = 20, fontStyle = FontStyle.Bold };
            accentStyle.normal.textColor = new Color(0.33f, 0.94f, 0.83f);
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold };
            buttonStyle.normal.background = buttonTexture;
            buttonStyle.normal.textColor = Color.white;
        }

        private static Texture2D Solid(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color); texture.Apply();
            return texture;
        }

        private static AudioClip Tone(float frequency, float duration)
        {
            int samples = Mathf.RoundToInt(22050 * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++) data[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / 22050f) * Mathf.Sin(Mathf.PI * i / samples) * 0.4f;
            var clip = AudioClip.Create("Relay tone", samples, 1, 22050, false); clip.SetData(data, 0); return clip;
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            if (player != null) player.RestartRequested -= RestartRoom;
            if (sockets != null) foreach (var socket in sockets) if (socket != null) socket.Captured -= OnCapture;
            if (panelTexture != null) Destroy(panelTexture);
            if (buttonTexture != null) Destroy(buttonTexture);
            if (switchClip != null) Destroy(switchClip);
            if (captureClip != null) Destroy(captureClip);
            if (finishClip != null) Destroy(finishClip);
        }
    }
}
