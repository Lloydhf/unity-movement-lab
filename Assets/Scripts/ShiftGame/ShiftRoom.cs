using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ShiftGame
{
    // Bölüm akışı burada; ağırlık, istasyon ve mekanizmalar kendi bileşenlerinde.
    public class ShiftRoom : MonoBehaviour
    {
        public ShiftRobot player;
        public ShiftStation[] stations = Array.Empty<ShiftStation>();
        public ShiftPiston[] pistons = Array.Empty<ShiftPiston>();
        public ShiftBreakable[] panels = Array.Empty<ShiftBreakable>();
        public ShiftGate[] gates = Array.Empty<ShiftGate>();
        public ShiftBridge[] bridges = Array.Empty<ShiftBridge>();
        public Transform exitPoint;
        public ShiftCamera followCamera;
        public int levelNumber = 1;
        public string nextScene = "06_ShiftPrepareTheWay";
        public bool showIntroduction = true;
        public float fallLimit = -9f;
        public enum RoomState { Introduction, Playing, Paused, Complete }
        public RoomState State { get; private set; }
        public int PoweredCount { get { int n = 0; foreach (var p in pistons) if (p != null && p.IsLatched) n++; return n; } }
        public int Attempts { get; private set; } = 1;
        public int Respawns { get; private set; }
        public int ModeChanges { get; private set; }
        public float Elapsed { get; private set; }
        public static bool English;
        public string Feedback { get; private set; }
        private Vector3 initialSpawn, checkpoint;
        private bool checkpointHeavy = true;
        private float feedbackUntil;
        private ShiftStation nearbyStation;
        private ShiftPiston nearbyPiston;
        private AudioSource speaker;
        private AudioClip modeSound, latchSound, breakSound, finishSound, errorSound;
        private Texture2D white;
        private GUIStyle title, heading, body, small, button, status, numeral;
        private bool stylesReady;
        private readonly Color ink = new Color(.035f,.065f,.095f,.97f);
        private readonly Color pale = new Color(.86f,.92f,.94f);
        private readonly Color cyan = new Color(.28f,.91f,.84f);
        private readonly Color amber = new Color(1f,.69f,.30f);

        private void Start()
        {
            initialSpawn = player.transform.position;
            checkpoint = initialSpawn;
            checkpointHeavy = player.IsHeavy;
            speaker = gameObject.AddComponent<AudioSource>();
            speaker.volume = .21f;
            modeSound = Tone("Mode", 430, 660, .18f);
            latchSound = Tone("Latch", 160, 380, .34f);
            breakSound = Tone("Break", 130, 45, .24f);
            finishSound = Tone("Complete", 520, 1040, .65f);
            errorSound = Tone("Too light", 180, 120, .12f);
            foreach (var piston in pistons) piston.Activated += OnPiston;
            foreach (var panel in panels) panel.Broken += OnBreak;
            SetState(showIntroduction ? RoomState.Introduction : RoomState.Playing);
            if (followCamera != null) followCamera.SnapToTarget();
        }

        private void Update()
        {
            var keys = Keyboard.current;
            bool input = keys != null && (Application.isFocused || Application.isBatchMode);
            if (input && keys.escapeKey.wasPressedThisFrame)
            {
                if (State == RoomState.Playing) SetState(RoomState.Paused);
                else if (State == RoomState.Paused) Begin();
            }
            if (input && keys.enterKey.wasPressedThisFrame)
            {
                if (State == RoomState.Introduction || State == RoomState.Paused) Begin();
                else if (State == RoomState.Complete) Continue();
            }
            if (State != RoomState.Playing) return;
            Elapsed += Time.deltaTime;
            FindInteraction();
            if (input && keys.rKey.wasPressedThisFrame) { RestartRoom(); return; }
            if (player.transform.position.y < fallLimit) { Respawn(); return; }
            if (input && keys.eKey.wasPressedThisFrame) TryInteract();
            if (Vector3.Distance(player.transform.position, exitPoint.position) < 1.1f) CompleteRoom();
        }

        public void Begin() => SetState(RoomState.Playing);

        private void SetState(RoomState state)
        {
            State = state;
            Time.timeScale = state == RoomState.Playing ? 1f : 0f;
            player.SetControlsEnabled(state == RoomState.Playing);
        }

        public void RestartRoom()
        {
            Time.timeScale = 1f;
            foreach (var piston in pistons) piston.ResetPiston();
            foreach (var gate in gates) gate.ResetGate();
            foreach (var bridge in bridges) bridge.ResetBridge();
            foreach (var panel in panels) panel.ResetPanel();
            player.SetHeavy(true);
            player.Teleport(initialSpawn);
            checkpoint = initialSpawn;
            checkpointHeavy = true;
            Attempts++;
            Respawns = 0;
            ModeChanges = 0;
            Elapsed = 0;
            nearbyStation = null; nearbyPiston = null;
            Feedback = null;
            SetState(RoomState.Playing);
            if (followCamera != null) followCamera.SnapToTarget();
        }

        public void Respawn()
        {
            if (State != RoomState.Playing) return;
            Respawns++;
            player.SetHeavy(checkpointHeavy);
            player.Teleport(checkpoint);
            Notify(T("Servis noktasına döndün. Açtığın yollar korunuyor.", "Back at the service station. Your progress is kept."));
            if (followCamera != null) followCamera.SnapToTarget();
        }

        public bool TryInteract()
        {
            if (State != RoomState.Playing) return false;
            FindInteraction();
            if (nearbyStation != null && nearbyStation.TryUse(player))
            {
                checkpoint = nearbyStation.spawnPoint != null ? nearbyStation.spawnPoint.position : player.transform.position;
                checkpointHeavy = player.IsHeavy;
                ModeChanges++;
                Play(modeSound);
                Notify(player.IsHeavy ? T("AĞIR · Zeminleri kır, pistonları kilitle.", "HEAVY · Break panels. Lock pistons.") : T("HAFİF · Yüksek basamaklara ulaş.", "LIGHT · Reach the high ledges."));
                return true;
            }
            if (nearbyPiston != null)
            {
                if (nearbyPiston.IsLatched) { Notify(T("Bu mekanizma kilitlendi; açık kalacak.", "This mechanism is latched. It stays open.")); return false; }
                if (nearbyPiston.TryActivate(player))
                {
                    if (nearbyPiston.bridges != null && nearbyPiston.bridges.Length > 0 && followCamera != null)
                        followCamera.Focus(nearbyPiston.bridges[0].transform.position + Vector3.up * 2f, 1.7f);
                    return true;
                }
                Play(errorSound);
                Notify(T("Pistonun üstünde AĞIR mod gerekiyor.", "Stand on the piston in HEAVY mode."));
            }
            return false;
        }

        private void FindInteraction()
        {
            nearbyStation = null; nearbyPiston = null;
            foreach (var station in stations)
                if (station != null && station.CanUse(player)) { nearbyStation = station; break; }
            float nearest = 1.6f;
            foreach (var piston in pistons)
            {
                if (piston == null) continue;
                Vector3 anchor = piston.transform.position + Vector3.up * .8f;
                float distance = Vector3.Distance(player.transform.position, anchor);
                if (distance < nearest) { nearest = distance; nearbyPiston = piston; }
            }
        }

        public void CompleteRoom()
        {
            if (State != RoomState.Playing || PoweredCount != pistons.Length ||
                Vector3.Distance(player.transform.position, exitPoint.position) >= 1.1f) return;
            Play(finishSound);
            SetState(RoomState.Complete);
        }

        public void Continue()
        {
            if (State != RoomState.Complete) return;
            Time.timeScale = 1f;
            SceneManager.LoadScene(string.IsNullOrEmpty(nextScene) ? "05_ShiftFirstShift" : nextScene);
        }

        private void OnPiston()
        {
            Play(latchSound);
            Notify(PoweredCount == pistons.Length ? T("Yol hazır. Çıkışa ulaş!", "The route is ready. Reach the exit!") : T("Mekanizma kilitlendi. Bağlı yol açık kalacak.", "Mechanism latched. Its route stays open."));
        }
        private void OnBreak() { Play(breakSound); Notify(T("Panel kırıldı. Aşağıdaki yolu keşfet.", "Panel broken. Explore the route below.")); }
        private void Play(AudioClip clip) { if (speaker != null && clip != null) speaker.PlayOneShot(clip); }
        private void Notify(string message) { Feedback = message; feedbackUntil = Time.time + 3.2f; }
        private static string T(string tr, string en) => English ? en : tr;

        private void OnGUI()
        {
            SetupStyles();
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            if (scale <= 0) return;
            Matrix4x4 original = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            float w = Screen.width / scale, h = Screen.height / scale;
            Color mode = player.IsHeavy ? amber : cyan;
            Fill(new Rect(0,0,w,92), ink);
            Fill(new Rect(0,0,5,92), mode);
            GUI.Label(new Rect(28,13,830,29), "SHIFT   /   " + (levelNumber == 1 ? T("01  İLK VARDİYA", "01  FIRST SHIFT") : T("02  ÖNCE YOLU HAZIRLA", "02  PREPARE THE WAY")), heading);
            GUI.Label(new Rect(28,50,865,30), Objective(), body);
            GUI.Label(new Rect(w-250,16,228,27), player.IsHeavy ? T("AĞIR MOD", "HEAVY MODE") : T("HAFİF MOD", "LIGHT MODE"), ModeStyle(mode));
            GUI.Label(new Rect(w-250,50,230,28), T("MEKANİZMALAR  ", "MECHANISMS  ") + PoweredCount + " / " + pistons.Length, small);
            Fill(new Rect(0,h-48,w,48), ink);
            GUI.Label(new Rect(26,h-36,w-52,28), T("A / D  HAREKET     SPACE  ZIPLA     E  ETKİLEŞİM     R  BÖLÜMÜ SIFIRLA     ESC  MENÜ", "A / D  MOVE     SPACE  JUMP     E  INTERACT     R  RESTART LEVEL     ESC  MENU"), small);

            if (State == RoomState.Playing)
            {
                string prompt = NearbyPrompt();
                if (!string.IsNullOrEmpty(prompt))
                {
                    Fill(new Rect(24,h-110,w-48,48), new Color(.025f,.055f,.08f,.93f));
                    GUI.Label(new Rect(40,h-100,w-80,33), prompt, ModeStyle(cyan));
                }
            }
            else DrawMenu(w,h);
            GUI.matrix = original;
        }

        private string Objective()
        {
            if (PoweredCount == pistons.Length) return T("Çıkış açık. Yüksek basamaklar için hafifle.", "Exit unlocked. Switch to LIGHT for the high ledges.");
            return levelNumber == 1 ? T("Makine odasına ulaş, çıkış pistonunu kilitle.", "Reach the machine room and latch the exit piston.") : T("Üst kontrol köprüyü, alt kontrol çıkışı açar.", "Upper control opens the bridge. Lower control unlocks the exit.");
        }

        private string NearbyPrompt()
        {
            if (nearbyStation != null) return "[ E ]  " + nearbyStation.stationId + "  /  " + (player.IsHeavy ? T("HAFİF moda geç", "Switch to LIGHT") : T("AĞIR moda geç", "Switch to HEAVY"));
            if (nearbyPiston != null) return nearbyPiston.IsLatched ? nearbyPiston.pistonId + T("  /  KİLİTLENDİ · Yol açık kalır", "  /  LATCHED · Route stays open") : "[ E ]  " + nearbyPiston.pistonId + T("  /  AĞIR modda pistonu kilitle", "  /  Latch the piston in HEAVY mode");
            if (Time.time < feedbackUntil) return Feedback;
            return player.IsHeavy ? T("AĞIR: çatlak paneller kırılır · yüksek basamaklar için istasyonda hafifle", "HEAVY: cracked panels break · use a station to reach high ledges") : T("HAFİF: yüksek sıçrama · pistonlar için istasyonda ağırlaş", "LIGHT: high jumps · use a station to press heavy pistons");
        }

        private void DrawMenu(float w, float h)
        {
            Fill(new Rect(0,92,w,h-140),new Color(.015f,.028f,.045f,.50f));
            float mw = 830, mh = 458, x=(w-mw)*.5f,y=Mathf.Max(103,(h-mh)*.5f);
            Fill(new Rect(x,y,mw,mh),ink);
            Fill(new Rect(x,y,4,mh),cyan);
            string kicker=State==RoomState.Introduction ? T("KÜTLE PROTOKOLÜ / OYNANABİLİR PROTOTİP 0.1", "MASS PROTOCOL / PLAYABLE PROTOTYPE 0.1") : State==RoomState.Paused ? T("SERVİS MOLASI", "SERVICE BREAK") : T("VARDİYA TAMAMLANDI", "SHIFT COMPLETE");
            GUI.Label(new Rect(x+36,y+24,mw-72,25),kicker,small);
            GUI.Label(new Rect(x+33,y+63,mw-66,69),State==RoomState.Introduction ? "SHIFT" : State==RoomState.Paused ? T("DURAKLATILDI", "PAUSED") : string.IsNullOrEmpty(nextScene) ? T("TESİS YENİDEN AKTİF", "FACILITY RESTORED") : T("YOLU AÇTIN", "ROUTE RESTORED"),title);
            string description = State==RoomState.Introduction ? T("Bir robot. İki ağırlık.\nServis istasyonlarında mod değiştir, tesisin çıkış yolunu aç.", "One robot. Two weights.\nChange modes at service stations and restore the way out.") : State==RoomState.Paused ? T("Açtığın yollar korunuyor.\nDevam edebilir veya bu bölümün tamamını sıfırlayabilirsin.", "Your restored routes are kept.\nResume, or restart this entire level.") : string.IsNullOrEmpty(nextScene) ? T("İki bölümü de tamamladın.\nİkinci bölümü farklı bir sırayla deneyebilirsin.", "Both levels complete.\nTry a different route order in the second level.") : T("Sıradaki bölümde aşağı inmeden önce yolu planla.", "Next: plan your route before descending.");
            GUI.Label(new Rect(x+36,y+140,mw-72,80),description,body);
            if (State==RoomState.Complete)
            {
                GUI.Label(new Rect(x+36,y+240,230,26),T("SÜRE", "TIME"),small);
                GUI.Label(new Rect(x+290,y+240,220,26),T("MOD DEĞİŞİMİ", "MODE CHANGES"),small);
                GUI.Label(new Rect(x+565,y+240,225,26),T("GERİ DÖNÜŞ", "RECOVERIES"),small);
                GUI.Label(new Rect(x+36,y+268,230,50),TimeSpan.FromSeconds(Elapsed).ToString(@"mm\:ss"),numeral);
                GUI.Label(new Rect(x+290,y+268,230,50),ModeChanges.ToString(),numeral);
                GUI.Label(new Rect(x+565,y+268,230,50),Respawns.ToString(),numeral);
            }
            else
            {
                Fill(new Rect(x+36,y+239,365,73),new Color(.07f,.17f,.21f));
                Fill(new Rect(x+421,y+239,373,73),new Color(.19f,.13f,.10f));
                GUI.Label(new Rect(x+51,y+249,337,24),T("HAFİF / YÜKSEĞE ULAŞ", "LIGHT / REACH HIGHER"),ModeStyle(cyan));
                GUI.Label(new Rect(x+436,y+249,340,24),T("AĞIR / ÇEVREYİ DEĞİŞTİR", "HEAVY / CHANGE THE ROUTE"),ModeStyle(amber));
                GUI.Label(new Rect(x+51,y+278,337,25),T("Yüksek sıçrama, güvenli panel geçişi", "High jumps, safe cracked panels"),small);
                GUI.Label(new Rect(x+436,y+278,340,25),T("Zemin kırma, pistonları kilitleme", "Break floors, latch mechanisms"),small);
            }
            string primary=State==RoomState.Introduction ? T("BAŞLA  [ENTER]", "START  [ENTER]") : State==RoomState.Paused ? T("DEVAM ET  [ENTER]", "RESUME  [ENTER]") : string.IsNullOrEmpty(nextScene) ? T("BAŞTAN OYNA  [ENTER]", "PLAY AGAIN  [ENTER]") : T("İKİNCİ BÖLÜM  [ENTER]", "NEXT LEVEL  [ENTER]");
            if (GUI.Button(new Rect(x+36,y+339,365,52),primary,button)) { if(State==RoomState.Complete) Continue(); else Begin(); }
            if (State != RoomState.Introduction && GUI.Button(new Rect(x+421,y+339,373,52),T("BU BÖLÜMÜ SIFIRLA", "RESTART THIS LEVEL"),button)) RestartRoom();
            if (State==RoomState.Introduction && GUI.Button(new Rect(x+421,y+339,373,52),English ? "TÜRKÇE / ENGLISH" : "TÜRKÇE / ENGLISH",button)) English=!English;
            if (GUI.Button(new Rect(x+36,y+410,165,28),T("OYUNDAN ÇIK", "QUIT GAME"),small)) Application.Quit();
            if (State!=RoomState.Introduction && GUI.Button(new Rect(x+615,y+410,180,28),"TR / EN",small)) English=!English;
        }

        private GUIStyle ModeStyle(Color color) { status.normal.textColor=color; return status; }
        private void Fill(Rect rect,Color color) { Color old=GUI.color; GUI.color=color; GUI.DrawTexture(rect,white); GUI.color=old; }
        private void SetupStyles()
        {
            if(stylesReady) return; stylesReady=true;
            white=new Texture2D(1,1); white.SetPixel(0,0,Color.white); white.Apply();
            title=new GUIStyle(GUI.skin.label){fontSize=46,fontStyle=FontStyle.Bold,wordWrap=false}; title.normal.textColor=pale;
            heading=new GUIStyle(title){fontSize=22};
            body=new GUIStyle(GUI.skin.label){fontSize=21,wordWrap=true}; body.normal.textColor=pale;
            small=new GUIStyle(body){fontSize=16};
            status=new GUIStyle(body){fontSize=19,fontStyle=FontStyle.Bold,wordWrap=false};
            numeral=new GUIStyle(title){fontSize=32};
            button=new GUIStyle(GUI.skin.button){fontSize=20,fontStyle=FontStyle.Bold};
            button.normal.textColor=pale; button.hover.textColor=cyan;
        }
        private static AudioClip Tone(string name,float from,float to,float duration)
        {
            const int rate=22050; int count=Mathf.CeilToInt(duration*rate); var data=new float[count]; float phase=0;
            for(int i=0;i<count;i++){float t=(float)i/count; phase+=2*Mathf.PI*Mathf.Lerp(from,to,t)/rate; data[i]=(Mathf.Sin(phase)+.18f*Mathf.Sin(phase*2))*Mathf.Sin(t*Mathf.PI)*.32f;}
            var clip=AudioClip.Create(name,count,1,rate,false); clip.SetData(data,0); return clip;
        }
        private void OnDestroy()
        {
            Time.timeScale=1;
            foreach(var p in pistons) if(p!=null) p.Activated-=OnPiston;
            foreach(var p in panels) if(p!=null) p.Broken-=OnBreak;
            if(white!=null) Destroy(white);
            foreach(var clip in new[]{modeSound,latchSound,breakSound,finishSound,errorSound}) if(clip!=null) Destroy(clip);
        }
    }
}
