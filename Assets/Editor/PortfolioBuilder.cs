using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using PortfolioMagnetics;

// Yalnızca yeni prototip sahnelerini üretir. Öğrencinin 01/02 sahnelerini değiştirmez.
public static class PortfolioBuilder
{
    private const string Root = "Assets/PortfolioPrototype";
    private static Material floor, wall, trim, teal, amber, metal, white, dark;

    [MenuItem("Portfolio/Generate magnetic prototype scenes")]
    public static void Generate()
    {
        if (!Application.isBatchMode && File.Exists("Assets/Scenes/03_FirstContact.unity") &&
            !EditorUtility.DisplayDialog("Regenerate prototype?", "This replaces 03_FirstContact and 04_DoubleRelay. Save your edited scenes under different names first.", "Regenerate", "Cancel")) return;
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(Root + "/Materials");
        AssetDatabase.Refresh();
        floor = Material("Floor", new Color(0.13f, 0.23f, 0.30f), 0.25f);
        wall = Material("Backdrop", new Color(0.025f, 0.065f, 0.11f), 0.1f);
        trim = Material("Trim", new Color(0.22f, 0.39f, 0.44f), 0.5f);
        teal = Material("Signal", new Color(0.12f, 0.86f, 0.77f), 0.4f, true);
        amber = Material("Magnet", new Color(1f, 0.43f, 0.13f), 0.4f, true);
        metal = Material("Core", new Color(0.61f, 0.79f, 0.85f), 0.75f);
        white = Material("Robot", new Color(0.82f, 0.89f, 0.92f), 0.4f);
        dark = Material("Visor", new Color(0.025f, 0.08f, 0.11f), 0.2f);
        BuildRoom(false);
        BuildRoom(true);
        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene("Assets/Scenes/03_FirstContact.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/04_DoubleRelay.unity", true)
        };
        PlayerSettings.companyName = "PortfolioLab";
        PlayerSettings.productName = "Polar Relay";
        PlayerSettings.bundleVersion = "0.1.0";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.runInBackground = false;
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/03_FirstContact.unity");
        Debug.Log("PORTFOLIO_GENERATED: Two magnetic rooms ready.");
    }

    public static void BuildWindows()
    {
        string output = Path.GetFullPath("Builds/PolarRelay/PolarRelay.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/03_FirstContact.unity", "Assets/Scenes/04_DoubleRelay.unity" },
            locationPathName = output,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Windows build failed: " + report.summary.result);
        Debug.Log("PORTFOLIO_BUILD_OK: " + output);
    }

    private static void BuildRoom(bool second)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.65f, 0.76f);
        RenderSettings.fog = false;
        var light = new GameObject("Station key light").AddComponent<Light>();
        light.type = LightType.Directional; light.intensity = 1.55f;
        light.transform.rotation = Quaternion.Euler(35, -30, 0);
        var camera = new GameObject("Main Camera").AddComponent<Camera>();
        camera.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = 5.3f;
        camera.transform.position = new Vector3(-2f, 2.65f, -18f);
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.025f, 0.055f, 0.09f);
        camera.gameObject.AddComponent<AudioListener>();

        var environment = new GameObject("Environment — edit platform positions here").transform;
        Cube("Station wall", new Vector3(2, 3.2f, 3.4f), new Vector3(30, 11, 0.25f), wall, false, environment);
        for (int x = -12; x <= 16; x += 3)
        {
            Cube("Wall rib", new Vector3(x, 3, 3.15f), new Vector3(0.075f, 9, 0.12f), trim, false, environment);
            Cube("Ceiling light", new Vector3(x, 5.4f, 2.9f), new Vector3(1.2f, 0.045f, 0.08f), teal, false, environment);
        }
        Cube("Bottom safety line", new Vector3(2, -1.5f, 3), new Vector3(28, 0.035f, .1f), amber, false, environment);
        if (!second)
        {
            Platform("Start floor", -5.5f, 0, 9, environment);
            Platform("Exit floor", 7.5f, 0, 13, environment);
            Platform("Control ledge", -3.6f, 0.72f, 2.2f, environment);
            WorldLabel("JUMP THE GAP", new Vector3(-0.1f, -.85f, -.9f), 0.07f, Color.white);
        }
        else
        {
            Platform("Start floor", -6, 0, 8, environment);
            Platform("Middle island", 2.5f, 0, 5, environment);
            Platform("Exit floor", 10.5f, 0, 7, environment);
            Platform("First ledge", -4.5f, 0.72f, 2.0f, environment);
            Platform("Second ledge", 3.1f, 0.8f, 1.8f, environment);
        }
        Cube("Left boundary", new Vector3(-10.25f, 1.6f, 0), new Vector3(.4f, 4.2f, 3), floor, true, environment);
        Cube("Right boundary", new Vector3(14.25f, 1.6f, 0), new Vector3(.4f, 4.2f, 3), floor, true, environment);

        var playerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        playerObject.name = "Player — movement settings";
        playerObject.transform.position = new Vector3(-8.4f, 1.05f, 0);
        playerObject.GetComponent<Renderer>().sharedMaterial = white;
        var player = playerObject.AddComponent<PlayerMovement>(); player.Configure(4f, 5.5f, -5f);
        Cube("Visor", new Vector3(-8.4f, 1.48f, -.47f), new Vector3(.66f, .24f, .12f), dark, false, playerObject.transform);
        Cube("Eye strip", new Vector3(-8.4f, 1.48f, -.54f), new Vector3(.46f, .055f, .05f), teal, false, playerObject.transform);
        Cube("Belt", new Vector3(-8.4f, .74f, -.02f), new Vector3(.99f, .13f, .76f), trim, false, playerObject.transform);
        var sideCamera = camera.gameObject.AddComponent<SideCamera>(); sideCamera.target = playerObject.transform;

        var controller = new GameObject("Room rules — objective and flow").AddComponent<RelayRoom>();
        controller.player = player;
        controller.roomTitle = second ? "02 / TWO CONNECTIONS" : "01 / FIRST CONTACT";
        controller.objective = second ? "Power both receivers. Cross the gaps and reach the exit." : "Find terminal 1. Pull the core into its receiver, then reach the exit.";
        controller.nextScene = second ? "" : "04_DoubleRelay";
        controller.showIntroduction = !second;
        var magnets = new List<MagnetEmitter>(); var consoles = new List<Transform>();
        var balls = new List<MagneticBall>(); var sockets = new List<GoalSocket>(); var gates = new List<RelayGate>();
        if (second)
        {
            Circuit(1, new Vector3(-4.5f, 1.38f, 0), new Vector3(-4.8f, 3.65f, 0), new Vector3(-1.6f, 4.0f, 0), 0.6f, environment, magnets, consoles, balls, sockets, gates);
            Circuit(2, new Vector3(3.1f, 1.46f, 0), new Vector3(2.7f, 3.65f, 0), new Vector3(6.1f, 4.0f, 0), 8.5f, environment, magnets, consoles, balls, sockets, gates);
        }
        else Circuit(1, new Vector3(-3.6f, 1.38f, 0), new Vector3(-3.0f, 3.65f, 0), new Vector3(.3f, 4f, 0), 5.8f, environment, magnets, consoles, balls, sockets, gates);
        controller.magnets = magnets.ToArray(); controller.consoles = consoles.ToArray(); controller.balls = balls.ToArray();
        controller.sockets = sockets.ToArray(); controller.gates = gates.ToArray();
        var exit = new GameObject("Exit target"); exit.transform.position = new Vector3(12.3f, 1f, 0); controller.exitPoint = exit.transform;
        Cube("Exit pad", new Vector3(12.3f, .035f, 0), new Vector3(1.6f, .07f, 2), teal, false, environment);
        Cube("Exit arch left", new Vector3(11.6f, 1.3f, .45f), new Vector3(.13f, 2.6f, .15f), teal, false, environment);
        Cube("Exit arch right", new Vector3(13f, 1.3f, .45f), new Vector3(.13f, 2.6f, .15f), teal, false, environment);
        Cube("Exit arch top", new Vector3(12.3f, 2.65f, .45f), new Vector3(1.55f, .13f, .15f), teal, false, environment);
        WorldLabel("EXIT", new Vector3(12.3f, 3f, -.1f), .13f, new Color(.4f, 1f, .8f));
        WorldLabel(second ? "RELAY / 02" : "RELAY / 01", new Vector3(-8.1f, 3.9f, 2.9f), .15f, new Color(.38f, .58f, .68f));
        EditorSceneManager.SaveScene(scene, second ? "Assets/Scenes/04_DoubleRelay.unity" : "Assets/Scenes/03_FirstContact.unity");
    }

    private static void Circuit(int number, Vector3 controlPosition, Vector3 ballPosition, Vector3 receiverPosition, float gateX, Transform environment,
        List<MagnetEmitter> magnets, List<Transform> consoles, List<MagneticBall> balls, List<GoalSocket> sockets, List<RelayGate> gates)
    {
        var circuit = new GameObject("Circuit " + number + " — magnet, core, receiver").transform;
        var terminal = Cube("Terminal " + number, controlPosition, new Vector3(.65f, 1.3f, .65f), dark, true, circuit);
        var screen = Cube("Terminal indicator", controlPosition + new Vector3(0, .06f, -.36f), new Vector3(.46f, .34f, .06f), amber, false, terminal.transform);
        WorldLabel(number.ToString(), controlPosition + new Vector3(0, .07f, -.405f), .15f, Color.white, terminal.transform);
        WorldLabel("[ E ]", controlPosition + new Vector3(0, .78f, -.15f), .11f, Color.white, terminal.transform);
        consoles.Add(terminal.transform);
        var core = GameObject.CreatePrimitive(PrimitiveType.Sphere); core.name = "Core " + number;
        core.transform.SetParent(circuit); core.transform.position = ballPosition; core.transform.localScale = Vector3.one * .6f;
        core.GetComponent<Renderer>().sharedMaterial = metal;
        var ball = core.AddComponent<MagneticBall>(); balls.Add(ball);
        float railLeft = ballPosition.x - .7f, railRight = receiverPosition.x + .5f;
        Cube("Core rail", new Vector3((railLeft + railRight) / 2f, 3.17f, 0), new Vector3(railRight - railLeft, .25f, 1.0f), trim, true, circuit);
        Cube("Rail left stop", new Vector3(railLeft, 3.55f, 0), new Vector3(.15f, .6f, 1), trim, true, circuit);
        var receiver = new GameObject("Receiver " + number); receiver.transform.SetParent(circuit); receiver.transform.position = receiverPosition;
        Cube("Receiver backplate", receiverPosition + new Vector3(0, 0, .48f), new Vector3(.86f, .86f, .15f), amber, false, receiver.transform);
        WorldLabel(number.ToString(), receiverPosition + new Vector3(0, .7f, -.1f), .13f, Color.white, receiver.transform);
        var socket = receiver.AddComponent<GoalSocket>(); socket.targetBall = ball; socket.capturePoint = receiver.transform; socket.captureRadius = .46f;
        sockets.Add(socket);
        var magnet = receiver.AddComponent<MagnetEmitter>(); magnet.targetBall = ball; magnet.fieldRadius = 5f; magnet.acceleration = 26f;
        magnet.maxBallSpeed = 5f; magnet.indicatorRenderer = screen.GetComponent<Renderer>(); magnets.Add(magnet);
        // İnce çizgiler menzili ve devre bağlantısını gösterir; fizik nesnesi değildir.
        var field = new GameObject("Magnetic reach").AddComponent<LineRenderer>(); field.transform.SetParent(circuit);
        field.sharedMaterial = trim; field.widthMultiplier = .018f; field.loop = true; field.positionCount = 64;
        for (int i = 0; i < 64; i++) { float a = i * Mathf.PI * 2 / 64; field.SetPosition(i, receiverPosition + new Vector3(Mathf.Cos(a) * 5, Mathf.Sin(a) * 5, 1)); }
        var gate = new GameObject("Gate " + number).AddComponent<RelayGate>(); gate.transform.SetParent(environment); gate.socket = socket;
        gate.panel = Cube("Gate panel", new Vector3(gateX, 1.55f, 0), new Vector3(.30f, 3.1f, 2.2f), trim, true, gate.transform).transform;
        Cube("Gate stripe", new Vector3(gateX, 1.55f, -1.13f), new Vector3(.32f, 2.8f, .04f), amber, false, gate.panel);
        gate.statusLight = Cube("Gate status", new Vector3(gateX, 3.32f, -.5f), new Vector3(.65f, .16f, .18f), amber, false, gate.transform).GetComponent<Renderer>();
        gates.Add(gate);
        var cable = new GameObject("Receiver to gate cable").AddComponent<LineRenderer>(); cable.transform.SetParent(circuit);
        cable.sharedMaterial = amber; cable.widthMultiplier = .035f; cable.positionCount = 4;
        cable.SetPositions(new[] { receiverPosition + Vector3.forward, new Vector3(receiverPosition.x, 4.9f, 1), new Vector3(gateX, 4.9f, 1), new Vector3(gateX, 3.32f, 1) });
    }

    private static void Platform(string name, float x, float top, float width, Transform parent)
    {
        Cube(name, new Vector3(x, top - .45f, 0), new Vector3(width, .9f, 2.5f), floor, true, parent);
        Cube(name + " edge", new Vector3(x, top - .035f, -1.28f), new Vector3(width, .07f, .06f), teal, false, parent);
    }

    private static GameObject Cube(string name, Vector3 position, Vector3 scale, Material material, bool collider, Transform parent)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name;
        if (parent != null) obj.transform.SetParent(parent);
        obj.transform.position = position;
        Vector3 parentScale = parent == null ? Vector3.one : parent.lossyScale;
        obj.transform.localScale = new Vector3(scale.x / parentScale.x, scale.y / parentScale.y, scale.z / parentScale.z);
        obj.GetComponent<Renderer>().sharedMaterial = material;
        if (!collider) UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
        return obj;
    }

    private static void WorldLabel(string value, Vector3 position, float size, Color color, Transform parent = null)
    {
        var label = new GameObject(value).AddComponent<TextMesh>();
        if (parent != null) label.transform.SetParent(parent);
        label.transform.position = position; label.text = value; label.fontSize = 64;
        label.characterSize = size; label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center; label.color = color;
    }

    private static Material Material(string name, Color color, float smoothness, bool emissive = false)
    {
        string path = Root + "/Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
        material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", smoothness);
        if (emissive) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * 1.2f); }
        return material;
    }
}
