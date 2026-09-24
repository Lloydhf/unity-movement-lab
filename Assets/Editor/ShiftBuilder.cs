using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using ShiftGame;

// Üretilen yeni sahneler Hierarchy'de doğrudan düzenlenebilir. 01–04 korunur.
public static class ShiftBuilder
{
    private const string Root = "Assets/ShiftPrototype";
    private const string First = "Assets/Scenes/05_ShiftFirstShift.unity";
    private const string Second = "Assets/Scenes/06_ShiftPrepareTheWay.unity";
    private static Material floor, trim, wall, dark, robotShell, cyan, amber, steel, dim;
    private static Transform environment, gameplay, backdrop;
    private static readonly List<ShiftStation> stations = new List<ShiftStation>();
    private static readonly List<ShiftPiston> pistons = new List<ShiftPiston>();
    private static readonly List<ShiftBreakable> panels = new List<ShiftBreakable>();
    private static readonly List<ShiftGate> gates = new List<ShiftGate>();
    private static readonly List<ShiftBridge> bridges = new List<ShiftBridge>();

    [MenuItem("Portfolio/SHIFT/Generate two robot levels")]
    public static void Generate()
    {
        if (!Application.isBatchMode && File.Exists(First) && !EditorUtility.DisplayDialog("Rebuild SHIFT levels?", "This replaces only 05_ShiftFirstShift and 06_ShiftPrepareTheWay. Save your edited versions under different names first.", "Rebuild", "Cancel")) return;
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(Root+"/Materials");
        Directory.CreateDirectory(Root+"/Prefabs");
        AssetDatabase.Refresh();
        floor = Mat("Structural metal", new Color(.16f,.23f,.27f),.3f);
        trim = Mat("Edge steel",new Color(.34f,.47f,.51f),.6f);
        wall = Mat("Wall panels",new Color(.043f,.073f,.095f),.15f);
        dark = Mat("Rubber and visor",new Color(.022f,.037f,.048f),.4f);
        robotShell = Mat("Ivory robot shell",new Color(.82f,.86f,.81f),.45f);
        cyan = Mat("Light signal",new Color(.15f,.83f,.77f),.5f,true);
        amber = Mat("Weight signal",new Color(1f,.57f,.19f),.35f,true);
        steel = Mat("Piston steel",new Color(.51f,.60f,.63f),.7f);
        dim = Mat("Background ribs",new Color(.11f,.19f,.23f),.4f);
        BuildLevel(false);
        BuildLevel(true);
        // Eski prototip sahneleri testlerde yüklenebilir; oyunun ilk iki sahnesi SHIFT.
        var keep = EditorBuildSettings.scenes.Where(s => s.path!=First && s.path!=Second).ToList();
        keep.Insert(0,new EditorBuildSettingsScene(Second,true));
        keep.Insert(0,new EditorBuildSettingsScene(First,true));
        EditorBuildSettings.scenes=keep.ToArray();
        PlayerSettings.companyName="PortfolioLab";
        PlayerSettings.productName="SHIFT - Mass Protocol";
        PlayerSettings.bundleVersion="0.1.0";
        PlayerSettings.defaultScreenWidth=1280; PlayerSettings.defaultScreenHeight=720;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
        PlayerSettings.runInBackground=false;
        PlayerSettings.resizableWindow=true;
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(First);
        Debug.Log("SHIFT_GENERATED: Two editable robot levels ready.");
    }

    [MenuItem("Portfolio/SHIFT/Build Windows game")]
    public static void BuildWindows()
    {
        string output=Path.GetFullPath("Builds/SHIFT-0.1/SHIFT.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{First,Second},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded) throw new Exception("SHIFT build failed: "+report.summary.result);
        Debug.Log("SHIFT_BUILD_OK: "+output);
    }

    // Otomatik ilk kurulumda tek Unity oturumunda üret ve paketle.
    public static void GenerateAndBuild()
    {
        Generate();
        BuildWindows();
    }

    private static void BuildLevel(bool second)
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        stations.Clear(); pistons.Clear(); panels.Clear(); gates.Clear(); bridges.Clear();
        environment=new GameObject("01 ENVIRONMENT - move platforms here").transform;
        gameplay=new GameObject("02 MECHANISMS - connections and settings").transform;
        backdrop=new GameObject("04 BACKDROP - visual only").transform;
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight=new Color(.55f,.63f,.69f); RenderSettings.fog=false;
        var key=new GameObject("Key light").AddComponent<Light>();
        key.type=LightType.Directional; key.intensity=1.6f; key.transform.rotation=Quaternion.Euler(30,-30,0);
        float left=second?-18:-15, right=second?36:23, low=second?-4f:-3.6f;
        Backdrop(left,right,second?11:8,low);
        Vector3 spawn=second?new Vector3(-10,3.23f,0):new Vector3(-12,.83f,0);
        var player=Robot(spawn);
        var cam=new GameObject("Main Camera").AddComponent<Camera>();
        cam.tag="MainCamera"; cam.orthographic=true; cam.backgroundColor=new Color(.023f,.042f,.060f);
        cam.clearFlags=CameraClearFlags.SolidColor; cam.nearClipPlane=.1f; cam.farClipPlane=150;
        cam.transform.position=new Vector3(spawn.x,2,-25); cam.gameObject.AddComponent<AudioListener>();
        var follow=cam.gameObject.AddComponent<ShiftCamera>(); follow.target=player.transform;
        follow.size=second?7.1f:6.6f;
        follow.minBounds=new Vector2(second?-8:-6,second?.1f:.3f);
        follow.maxBounds=new Vector2(second?27:14,second?4.5f:2.7f);
        var room=new GameObject("05 ROOM RULES - objectives and flow").AddComponent<ShiftRoom>();
        room.player=player; room.followCamera=follow; room.levelNumber=second?2:1;
        room.showIntroduction=!second; room.nextScene=second?"":"06_ShiftPrepareTheWay";
        room.fallLimit=-10;
        if(second) SecondRoom(room); else FirstRoom(room);
        room.stations=stations.ToArray(); room.pistons=pistons.ToArray(); room.panels=panels.ToArray();
        room.gates=gates.ToArray(); room.bridges=bridges.ToArray();
        Wall("Left boundary",left-.3f,-6,second?12:9,.6f);
        Wall("Right boundary",right+.3f,-6,second?12:9,.6f);
        Label(second?"02 / ROUTE PLANNING":"01 / FIRST SHIFT",new Vector3(second?-11:-11,second?7:4.5f,2.8f),.24f,new Color(.34f,.48f,.54f),backdrop);
        Label("MASS / SERVICE FACILITY",new Vector3(second?20:12,second?6.5f:5.9f,2.8f),.15f,new Color(.22f,.35f,.40f),backdrop);
        EditorSceneManager.SaveScene(scene,second?Second:First);
    }

    private static void FirstRoom(ShiftRoom room)
    {
        Platform("Start deck",-14,-7,0);
        Platform("Light jump 01",-6.5f,-3.5f,1.6f);
        Platform("Upper service deck",-2.7f,1,3.2f);
        Platform("Machine room roof",4,9.5f,3.2f);
        Wall("Machine room left wall",.3f,-3.6f,3.2f,.6f);
        Wall("Upper route end wall",4.25f,3.2f,8.3f,.5f);
        Platform("Machine room floor",.6f,9.5f,-3.6f);
        Platform("Exit step 01",9.5f,12.5f,-2);
        Platform("Exit step 02",13.2f,16.2f,-.4f);
        Platform("Exit landing",16.9f,22,1.2f);
        Station("S0",-12,0);
        Station("S1",-1,3.2f);
        Station("S2",6.8f,-3.6f);
        Panel("F1",1,4,3.2f);
        var exitGate=Gate("EXIT",19,1.2f,3.4f);
        var piston=Piston("P1 / EXIT",3,-3.6f,new[]{exitGate},Array.Empty<ShiftBridge>());
        Cable(new[]{new Vector3(3,-3.9f,1.5f),new Vector3(18.5f,-3.9f,1.5f),new Vector3(18.5f,2,1.5f)},amber,gameplay);
        Label("LIGHT ROUTE",new Vector3(-5.3f,3.2f,.3f),.12f,new Color(.34f,.91f,.83f));
        Label("HEAVY FLOOR",new Vector3(2.5f,4.2f,.3f),.12f,new Color(1,.70f,.37f));
        Label("MACHINE ROOM",new Vector3(5,-.8f,2.5f),.15f,new Color(.35f,.49f,.54f),backdrop);
        room.exitPoint=Exit(20.5f,1.2f);
    }

    private static void SecondRoom(ShiftRoom room)
    {
        Platform("Central service deck",-12,-1,2.4f);
        // Ana ağır güzergâhın üstünde en az robot boyu kadar baş boşluğu kalır.
        Platform("Upper step 01",-9,-6.5f,4.65f);
        Platform("Upper step 02",-5.7f,-3.2f,5.6f);
        Platform("Maintenance entrance lip",-2.4f,.4f,7.2f);
        Platform("Recessed maintenance bay",.4f,6.8f,5.6f);
        Wall("Bay left lip",.4f,5.6f,7.2f,.25f);
        Wall("Bay right wall",7.05f,5.6f,12,.5f);
        Platform("Lower room ceiling",2,10,2.4f);
        Wall("Panel right bulkhead",2.25f,2.4f,5.6f,.5f);
        Platform("Lower room floor",-1.1f,10,-4);
        // Geri dönüş tüneli P2 kapısı ile alt odadan ayrılır.
        Platform("Recovery tunnel floor",-17,-1.1f,-4);
        Wall("Recovery separation wall",-1.35f,-1,2.4f,.5f);
        Platform("Recovery step 01",-10,-7,-2.4f);
        Platform("Recovery step 02",-13,-10.5f,-.8f);
        Platform("Recovery step 03",-16,-13.6f,.8f);
        Station("S0",-10,2.4f);
        Station("S1",2,5.6f);
        Station("S2",6,-4);
        Panel("F2",-1,2,2.4f);
        var bridge=Bridge(10,18,-4);
        Platform("Far bridge landing",18,22,-4);
        Platform("Exit climb 01",22,25,-2.4f);
        Platform("Exit climb 02",25.8f,28.8f,-.8f);
        Platform("Exit landing",29.5f,35,.8f);
        var gate=Gate("EXIT",32,.8f,3.4f);
        var recovery=Gate("K / RETURN",-1.35f,-4,3);
        Piston("P1 / BRIDGE",5,5.6f,Array.Empty<ShiftGate>(),new[]{bridge});
        Piston("P2 / EXIT + RETURN",1,-4,new[]{gate,recovery},Array.Empty<ShiftBridge>());
        Cable(new[]{new Vector3(5,5.3f,1.6f),new Vector3(8,5.3f,1.6f),new Vector3(8,-4.3f,1.6f),new Vector3(14,-4.3f,1.6f)},cyan,gameplay);
        Cable(new[]{new Vector3(1,-4.5f,1.6f),new Vector3(31.5f,-4.5f,1.6f),new Vector3(31.5f,1.6f,1.6f)},amber,gameplay);
        Cable(new[]{new Vector3(1,-4.5f,1.6f),new Vector3(-2,-4.5f,1.6f),new Vector3(-2,-2.5f,1.6f)},amber,gameplay);
        Label("P1 / BRIDGE CONTROL",new Vector3(3.8f,9.3f,2.4f),.14f,new Color(.34f,.91f,.83f),backdrop);
        Label("SERVICE RETURN",new Vector3(-7,-.8f,2.4f),.15f,new Color(.34f,.48f,.54f),backdrop);
        Label("BRIDGE / P1",new Vector3(14,-2.1f,2.4f),.14f,new Color(.34f,.91f,.83f),backdrop);
        Label("P2 / EXIT + RETURN",new Vector3(2.4f,-1.0f,2.4f),.13f,new Color(1,.70f,.37f),backdrop);
        room.exitPoint=Exit(33.5f,.8f);
    }

    private static ShiftRobot Robot(Vector3 position)
    {
        var root=new GameObject("03 ROBOT - movement and weight settings"); root.transform.position=position;
        var robot=root.AddComponent<ShiftRobot>(); robot.moveSpeed=4.4f; robot.lightJump=7.2f; robot.heavyJump=3.8f; robot.initialHeavy=true;
        var shape=root.GetComponent<CapsuleCollider>(); shape.height=1.6f; shape.radius=.32f; shape.center=Vector3.zero;
        var visual=new GameObject("Visual - no colliders").transform; visual.SetParent(root.transform,false);
        Transform Part(string name,Vector3 local,Vector3 size,Material material)
        {
            var part=Cube(name,position+local,size,material,false,visual); return part.transform;
        }
        var torso=Part("Armored torso",new Vector3(0,.05f,0),new Vector3(.65f,.65f,.48f),robotShell);
        var head=Part("Head",new Vector3(0,.56f,0),new Vector3(.7f,.40f,.50f),robotShell);
        Cube("Dark visor",position+new Vector3(0,.58f,-.28f),new Vector3(.54f,.19f,.09f),dark,false,head);
        var eyes=Cube("Mode eyes",position+new Vector3(0,.58f,-.335f),new Vector3(.38f,.055f,.025f),cyan,false,head);
        Cube("Chest plate",position+new Vector3(0,.07f,-.265f),new Vector3(.42f,.3f,.07f),dark,false,torso);
        var core=Cube("Mode core",position+new Vector3(0,.08f,-.31f),new Vector3(.1f,.19f,.04f),cyan,false,torso);
        var legL=Part("Left actuator",new Vector3(-.22f,-.48f,0),new Vector3(.24f,.5f,.36f),dark);
        var legR=Part("Right actuator",new Vector3(.22f,-.48f,0),new Vector3(.24f,.5f,.36f),dark);
        Cube("Left boot",position+new Vector3(-.22f,-.69f,-.04f),new Vector3(.32f,.18f,.45f),steel,false,legL);
        Cube("Right boot",position+new Vector3(.22f,-.69f,-.04f),new Vector3(.32f,.18f,.45f),steel,false,legR);
        var armL=Part("Left arm",new Vector3(-.45f,.06f,0),new Vector3(.18f,.56f,.28f),steel);
        var armR=Part("Right arm",new Vector3(.45f,.06f,0),new Vector3(.18f,.56f,.28f),steel);
        var animation=root.AddComponent<ShiftRobotVisual>(); animation.robot=robot;
        animation.torso=torso; animation.head=head; animation.leftLeg=legL; animation.rightLeg=legR; animation.leftArm=armL; animation.rightArm=armR;
        animation.modeLights=new[]{eyes.GetComponent<Renderer>(),core.GetComponent<Renderer>()};
        PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/ShiftRobot.prefab");
        return robot;
    }

    private static ShiftStation Station(string id,float x,float top)
    {
        var root=new GameObject(id+" - change weight / checkpoint"); root.transform.SetParent(gameplay); root.transform.position=new Vector3(x,top+.8f,0);
        var station=root.AddComponent<ShiftStation>(); station.stationId=id;
        var spawn=new GameObject("Safe respawn").transform; spawn.SetParent(root.transform,false); spawn.localPosition=new Vector3(0,.035f,0); station.spawnPoint=spawn;
        Cube("Service base",new Vector3(x,top+.04f,0),new Vector3(1.55f,.08f,2),trim,false,root.transform);
        Cube("Terminal back",new Vector3(x,top+.95f,.9f),new Vector3(.68f,1.9f,.28f),dark,false,root.transform);
        Cube("Terminal screen",new Vector3(x,top+1.34f,.72f),new Vector3(.45f,.42f,.05f),cyan,false,root.transform);
        for(int i=0;i<3;i++) Cube("Service light",new Vector3(x-.25f+i*.25f,top+.17f,-1.1f),new Vector3(.13f,.045f,.05f),cyan,false,root.transform);
        Label(id,new Vector3(x,top+2.34f,.6f),.14f,new Color(.38f,.95f,.86f),root.transform);
        Label("[ E ]",new Vector3(x,top+1.77f,.63f),.09f,Color.white,root.transform);
        stations.Add(station); return station;
    }

    private static ShiftPiston Piston(string id,float x,float top,ShiftGate[] targets,ShiftBridge[] decks)
    {
        var root=new GameObject(id+" - HEAVY + E latches targets"); root.transform.SetParent(gameplay); root.transform.position=new Vector3(x,top,0);
        var piston=root.AddComponent<ShiftPiston>(); piston.pistonId=id; piston.gates=targets; piston.bridges=decks;
        Cube("Piston housing",new Vector3(x,top-.12f,0),new Vector3(1.3f,.25f,1.8f),dark,false,root.transform);
        var plunger=Cube("Pressure pad",new Vector3(x,top+.065f,0),new Vector3(1.15f,.13f,1.6f),steel,false,root.transform);
        piston.plunger=plunger.transform;
        var indicator=Cube("Latch indicator",new Vector3(x,top+.09f,-.86f),new Vector3(.82f,.09f,.06f),amber,false,root.transform);
        piston.indicator=indicator.GetComponent<Renderer>();
        Label(id,new Vector3(x,top+1.8f,.8f),.11f,new Color(1,.75f,.43f),root.transform);
        Label("HEAVY + E",new Vector3(x,top+.62f,1.05f),.09f,new Color(.85f,.88f,.86f),root.transform);
        pistons.Add(piston); return piston;
    }

    private static ShiftBreakable Panel(string id,float from,float to,float top)
    {
        var root=new GameObject(id+" - heavy breakable panel"); root.transform.SetParent(gameplay); root.transform.position=new Vector3((from+to)/2,top,0);
        var panel=root.AddComponent<ShiftBreakable>();
        var surface=Cube("Fracture surface",new Vector3((from+to)/2,top-.13f,0),new Vector3(to-from,.26f,2.5f),amber,true,root.transform);
        panel.surface=surface.GetComponent<Collider>();
        for(float x=from+.25f;x<to;x+=.5f) Cube("Crack warning stripe",new Vector3(x,top-.12f,-1.285f),new Vector3(.19f,.25f,.04f),dark,false,root.transform).transform.rotation=Quaternion.Euler(0,0,-22);
        Label(id,new Vector3((from+to)/2,top-.58f,-1.32f),.11f,new Color(1,.73f,.39f),root.transform);
        panel.visuals=root.GetComponentsInChildren<Renderer>(); panels.Add(panel); return panel;
    }

    private static ShiftGate Gate(string id,float x,float top,float height)
    {
        var root=new GameObject(id+" - latched gate"); root.transform.SetParent(gameplay); root.transform.position=new Vector3(x,top,0);
        var gate=root.AddComponent<ShiftGate>();
        var panel=Cube("Sliding gate",new Vector3(x,top+height/2,0),new Vector3(.36f,height,2.5f),steel,true,root.transform);
        gate.panel=panel.transform; gate.blocker=panel.GetComponent<Collider>(); gate.openOffset=new Vector3(0,height+.2f,0);
        for(float y=top+.3f;y<top+height;y+=.5f) Cube("Gate stripe",new Vector3(x,y,-1.28f),new Vector3(.38f,.12f,.04f),amber,false,panel.transform);
        Cube("Gate frame",new Vector3(x,top+height+.1f,.2f),new Vector3(.9f,.20f,2.8f),dark,false,root.transform);
        gates.Add(gate); return gate;
    }

    private static ShiftBridge Bridge(float from,float to,float top)
    {
        var root=new GameObject("P1 - retractable bridge"); root.transform.SetParent(gameplay); root.transform.position=new Vector3((from+to)/2,top,0);
        var bridge=root.AddComponent<ShiftBridge>();
        var deck=Cube("Bridge deck",new Vector3((from+to)/2,top-.17f,0),new Vector3(to-from,.34f,2.5f),steel,true,root.transform);
        bridge.deck=deck.transform; bridge.surface=deck.GetComponent<Collider>();
        Cube("Bridge signal",new Vector3((from+to)/2,top-.02f,-1.28f),new Vector3(to-from,.07f,.06f),cyan,false,deck.transform);
        for(float x=from+.4f;x<to;x+=.8f) Cube("Bridge tread",new Vector3(x,top-.14f,-1.31f),new Vector3(.04f,.27f,.03f),dark,false,deck.transform);
        bridge.visuals=deck.GetComponentsInChildren<Renderer>();
        Cube("Left bridge socket",new Vector3(from-.15f,top-.3f,0),new Vector3(.3f,.8f,2.8f),trim,false,root.transform);
        Cube("Right bridge socket",new Vector3(to+.15f,top-.3f,0),new Vector3(.3f,.8f,2.8f),trim,false,root.transform);
        bridges.Add(bridge); return bridge;
    }

    private static Transform Exit(float x,float top)
    {
        var root=new GameObject("Exit destination").transform; root.SetParent(gameplay); root.position=new Vector3(x,top+.8f,0);
        Cube("Exit light base",new Vector3(x,top+.03f,0),new Vector3(1.8f,.05f,2.4f),cyan,false,root);
        Cube("Exit left post",new Vector3(x-.95f,top+1.25f,.85f),new Vector3(.10f,2.5f,.10f),cyan,false,root);
        Cube("Exit right post",new Vector3(x+.95f,top+1.25f,.85f),new Vector3(.10f,2.5f,.10f),cyan,false,root);
        Cube("Exit header",new Vector3(x,top+2.5f,.85f),new Vector3(2,.10f,.10f),cyan,false,root);
        Label("EXIT",new Vector3(x,top+3.1f,.6f),.19f,new Color(.38f,.95f,.86f),root);
        return root;
    }

    private static void Platform(string name,float from,float to,float top)
    {
        var block=Cube(name,new Vector3((from+to)/2,top-.27f,0),new Vector3(to-from,.54f,2.5f),floor,true,environment);
        Cube("Walkable edge",new Vector3((from+to)/2,top-.045f,-1.285f),new Vector3(to-from,.085f,.04f),trim,false,block.transform);
        if(to-from>2.5f) for(float x=from+.6f;x<to-.2f;x+=2) Cube("Panel bolt",new Vector3(x,top-.29f,-1.29f),new Vector3(.055f,.055f,.04f),steel,false,block.transform);
    }
    private static void Wall(string name,float x,float bottom,float top,float width)
        => Cube(name,new Vector3(x,(bottom+top)/2,0),new Vector3(width,top-bottom,2.5f),floor,true,environment);

    private static void Backdrop(float left,float right,float top,float bottom)
    {
        float h=top-bottom+4;
        Cube("Facility wall",new Vector3((left+right)/2,(top+bottom)/2,3.8f),new Vector3(right-left+4,h,.35f),wall,false,backdrop);
        for(float x=left;x<=right;x+=3.5f)
        {
            Cube("Wall rib",new Vector3(x,(top+bottom)/2,3.5f),new Vector3(.09f,h,.16f),dim,false,backdrop);
            Cube("Upper lamp",new Vector3(x,top-.8f,3.30f),new Vector3(1.6f,.07f,.15f),cyan,false,backdrop);
            Cube("Wall foot vent",new Vector3(x,bottom-1.1f,3.3f),new Vector3(1.4f,.38f,.12f),dark,false,backdrop);
            for(int i=0;i<4;i++) Cube("Vent slot",new Vector3(x-.5f+i*.34f,bottom-1.1f,3.21f),new Vector3(.07f,.3f,.02f),dim,false,backdrop);
        }
        Cube("Lower utility pipe",new Vector3((left+right)/2,bottom-1.8f,3.2f),new Vector3(right-left,.17f,.15f),dim,false,backdrop);
    }
    private static void Cable(Vector3[] points,Material material,Transform parent)
    {
        for(int i=1;i<points.Length;i++)
        {
            var delta=points[i]-points[i-1];
            var line=Cube("Mechanism signal cable",(points[i]+points[i-1])/2,new Vector3(delta.magnitude,.055f,.07f),material,false,parent);
            line.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
        }
    }
    private static GameObject Cube(string name,Vector3 position,Vector3 size,Material mat,bool collision,Transform parent)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name;
        if(parent!=null) go.transform.SetParent(parent);
        go.transform.position=position;
        var s=parent==null?Vector3.one:parent.lossyScale;
        go.transform.localScale=new Vector3(size.x/s.x,size.y/s.y,size.z/s.z);
        go.GetComponent<Renderer>().sharedMaterial=mat;
        if(!collision) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }
    private static void Label(string text,Vector3 position,float size,Color color,Transform parent=null)
    {
        var label=new GameObject(text).AddComponent<TextMesh>(); label.transform.SetParent(parent==null?gameplay:parent);
        label.transform.position=position; label.fontSize=64; label.characterSize=size*.48f;
        label.anchor=TextAnchor.MiddleCenter; label.alignment=TextAlignment.Center; label.text=text; label.color=color;
    }
    private static Material Mat(string name,Color color,float smooth,bool emission=false)
    {
        string path=Root+"/Materials/"+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material,path);}
        material.SetColor("_BaseColor",color); material.SetFloat("_Smoothness",smooth);
        if(emission){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",color*.8f);}
        return material;
    }
}
