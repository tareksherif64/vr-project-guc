using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;

public class BuildDetectiveRoom
{
    [MenuItem("Tools/Build Detective Room")]
    public static void Build()
    {
        // --- Materials ---
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material wallMat = new Material(shader);
        SetColor(wallMat, new Color(0.82f, 0.76f, 0.68f));   // warm off-white
        AssetDatabase.CreateAsset(wallMat, "Assets/Materials/Room_Wall.mat");

        Material floorMat = new Material(shader);
        SetColor(floorMat, new Color(0.20f, 0.16f, 0.13f));  // dark wood
        AssetDatabase.CreateAsset(floorMat, "Assets/Materials/Room_Floor.mat");

        Material ceilingMat = new Material(shader);
        SetColor(ceilingMat, new Color(0.90f, 0.88f, 0.85f)); // near-white
        AssetDatabase.CreateAsset(ceilingMat, "Assets/Materials/Room_Ceiling.mat");

        AssetDatabase.SaveAssets();

        // --- Room parent ---
        GameObject room = new GameObject("DetectiveRoom");

        // Room bounds:  X: -3.5 to 3.5  |  Z: -3 to 4  |  Y: 0 to 3
        // Floor / Ceiling
        MakeBox("Floor",   room, new Vector3(0f,    -0.05f, 0.5f), new Vector3(7f, 0.1f, 7f),   floorMat);
        MakeBox("Ceiling", room, new Vector3(0f,     3.05f, 0.5f), new Vector3(7f, 0.1f, 7f),   ceilingMat);

        // Back wall (evidence board will sit here)
        MakeBox("Wall_Back",  room, new Vector3(0f,    1.5f,  4.05f), new Vector3(7f, 3f, 0.1f),   wallMat);

        // Side walls
        MakeBox("Wall_Left",  room, new Vector3(-3.55f, 1.5f, 0.5f),  new Vector3(0.1f, 3f, 7f),   wallMat);
        MakeBox("Wall_Right", room, new Vector3( 3.55f, 1.5f, 0.5f),  new Vector3(0.1f, 3f, 7f),   wallMat);

        // Front wall — door opening 1 m wide (x: -0.5 to 0.5), 2.2 m tall
        // Left section  x: -3.5 to -0.5  (3 m wide, center x=-2)
        MakeBox("Wall_Front_Left",    room, new Vector3(-2f,   1.5f,  -3.05f), new Vector3(3f, 3f, 0.1f),    wallMat);
        // Right section x:  0.5 to  3.5  (3 m wide, center x=+2)
        MakeBox("Wall_Front_Right",   room, new Vector3( 2f,   1.5f,  -3.05f), new Vector3(3f, 3f, 0.1f),    wallMat);
        // Lintel above door  (y: 2.2 to 3.0 = 0.8 m tall)
        MakeBox("Wall_Front_DoorTop", room, new Vector3( 0f,   2.6f,  -3.05f), new Vector3(1f, 0.8f, 0.1f),  wallMat);

        // --- Placeholders ---
        // Door placeholder — centered in door gap
        GameObject doorPlaceholder = new GameObject("DoorPlaceholder");
        doorPlaceholder.transform.SetParent(room.transform);
        doorPlaceholder.transform.position = new Vector3(0f, 1.1f, -3.0f);

        // Desk placeholder — left side of room, facing the board
        GameObject deskPlaceholder = new GameObject("DeskPlaceholder");
        deskPlaceholder.transform.SetParent(room.transform);
        deskPlaceholder.transform.position = new Vector3(-2.0f, 0f, 0.5f);
        deskPlaceholder.transform.eulerAngles = new Vector3(0f, 90f, 0f); // faces board wall

        // --- Lighting ---
        // Main overhead point light (warm, slightly off-center)
        GameObject mainLightGO = new GameObject("Light_RoomMain");
        mainLightGO.transform.SetParent(room.transform);
        mainLightGO.transform.position = new Vector3(0f, 2.75f, 0.5f);
        Light mainLight = mainLightGO.AddComponent<Light>();
        mainLight.type      = LightType.Point;
        mainLight.intensity = 2.0f;
        mainLight.range     = 12f;
        mainLight.color     = new Color(1.0f, 0.91f, 0.76f); // warm incandescent
        mainLight.shadows   = LightShadows.Soft;

        // Spot light aimed at the evidence board
        GameObject boardLightGO = new GameObject("Light_BoardSpot");
        boardLightGO.transform.SetParent(room.transform);
        boardLightGO.transform.position    = new Vector3(0f, 2.8f, 2.2f);
        boardLightGO.transform.eulerAngles = new Vector3(55f, 0f, 0f);
        Light boardLight = boardLightGO.AddComponent<Light>();
        boardLight.type      = LightType.Spot;
        boardLight.intensity = 1.5f;
        boardLight.range     = 5f;
        boardLight.spotAngle = 50f;
        boardLight.color     = new Color(1.0f, 0.95f, 0.85f);
        boardLight.shadows   = LightShadows.Soft;

        // Dim the default directional light so it feels like an interior
        GameObject dirLightGO = GameObject.Find("Directional Light");
        if (dirLightGO != null)
        {
            Light dirLight = dirLightGO.GetComponent<Light>();
            if (dirLight != null)
            {
                dirLight.intensity = 0.08f;
                dirLight.color     = new Color(0.4f, 0.45f, 0.6f); // cool ambient fill
            }
        }

        // --- Move evidence board to back wall ---
        GameObject evidenceBoard = GameObject.Find("evidence_board");
        if (evidenceBoard != null)
        {
            evidenceBoard.transform.position = new Vector3(0f, 1.5f, 3.9f);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[DetectiveRoom] Room built successfully!");
    }

    static void MakeBox(string name, GameObject parent, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform);
        go.transform.position   = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    [MenuItem("Tools/Apply Board Texture")]
    public static void ApplyBoardTexture()
    {
        // Try jpg then png
        string texPath = null;
        foreach (string candidate in new[]{"Assets/boardtexture.jpg","Assets/boardtexture.jpeg","Assets/boardtexture.png"})
        {
            if (System.IO.File.Exists(candidate))  { texPath = candidate; break; }
        }
        if (texPath == null)
        {
            Debug.LogError("[DetectiveRoom] No boardtexture file found in Assets/");
            return;
        }

        AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (tex == null)
        {
            Debug.LogError($"[DetectiveRoom] Could not load {texPath} as Texture2D.");
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);

        // Assign texture to albedo/base map
        if (mat.HasProperty("_BaseMap"))       mat.SetTexture("_BaseMap", tex);
        else if (mat.HasProperty("_MainTex"))  mat.SetTexture("_MainTex", tex);

        // Keep colour white so texture isn't tinted
        SetColor(mat, Color.white);

        string matPath = "Assets/Materials/EvidenceBoard_Mat.mat";
        AssetDatabase.DeleteAsset(matPath); // replace if exists
        AssetDatabase.CreateAsset(mat, matPath);
        AssetDatabase.SaveAssets();

        // Apply to the EvidenceBoard mesh
        GameObject board = GameObject.Find("EvidenceBoard");
        if (board == null)
        {
            Debug.LogError("[DetectiveRoom] EvidenceBoard GameObject not found in scene.");
            return;
        }

        Renderer rend = board.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.sharedMaterial = mat;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[DetectiveRoom] Board texture applied successfully.");
        }
    }

    [MenuItem("Tools/List Hand Bones")]
    public static void ListHandBones()
    {
        GameObject leftHandAsset = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Samples/XR Hands/1.7.3/HandVisualizer/Models/LeftHand.fbx");
        if (leftHandAsset == null) { Debug.LogError("LeftHand.fbx not found"); return; }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== LeftHand bones ===");
        foreach (Transform t in leftHandAsset.GetComponentsInChildren<Transform>(true))
            sb.AppendLine($"  {GetPath(t, leftHandAsset.transform)}");
        Debug.Log(sb.ToString());
    }

    static string GetPath(Transform t, Transform root)
    {
        if (t == root) return t.name;
        return GetPath(t.parent, root) + "/" + t.name;
    }

    [MenuItem("Tools/Setup Controller Hands")]
    public static void SetupControllerHands()
    {
        // Load hand FBX models
        GameObject leftHandAsset  = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Samples/XR Hands/1.7.3/HandVisualizer/Models/LeftHand.fbx");
        GameObject rightHandAsset = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Samples/XR Hands/1.7.3/HandVisualizer/Models/RightHand.fbx");

        if (leftHandAsset == null || rightHandAsset == null)
        {
            Debug.LogError("[Hands] Hand FBX assets not found in XR Hands sample folder.");
            return;
        }

        // Load the hand material
        Material handMat = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Samples/XR Hands/1.7.3/HandVisualizer/Materials/HighFidelity.png");
        // Fallback: find any material in the HandVisualizer folder
        if (handMat == null)
        {
            string[] matGuids = AssetDatabase.FindAssets("t:Material", new[]{ "Assets/Samples/XR Hands/1.7.3/HandVisualizer" });
            if (matGuids.Length > 0)
                handMat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(matGuids[0]));
        }

        SetupHand("Left Controller",  leftHandAsset,  handMat, isLeft: true);
        SetupHand("Right Controller", rightHandAsset, handMat, isLeft: false);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[Hands] Controller hands set up — controller models hidden, hand meshes added.");
    }

    static void SetupHand(string controllerName, GameObject handAsset, Material mat, bool isLeft)
    {
        GameObject controller = GameObject.Find(controllerName);
        if (controller == null)
        {
            Debug.LogWarning($"[Hands] '{controllerName}' not found in scene.");
            return;
        }

        // ── Hide all existing controller mesh renderers ──
        foreach (MeshRenderer mr in controller.GetComponentsInChildren<MeshRenderer>(true))
            mr.enabled = false;

        // ── Remove any previously added hand model ──
        Transform existing = controller.transform.Find("HandModel");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        // ── Instantiate hand mesh as child ──
        GameObject hand = (GameObject)PrefabUtility.InstantiatePrefab(handAsset);
        hand.name = "HandModel";
        hand.transform.SetParent(controller.transform, false);

        // Align hand to controller grip:
        // OpenXR grip pose: Z forward (into screen), Y up, X right
        // Hand FBX: fingers point along +Z, palm faces -Y (back of hand up)
        // Rotate so the palm wraps naturally around the controller grip
        hand.transform.localPosition = new Vector3(isLeft ? 0.005f : -0.005f, -0.03f, -0.05f);
        hand.transform.localEulerAngles = new Vector3(40f, isLeft ? 0f : 180f, isLeft ? 90f : -90f);
        hand.transform.localScale = Vector3.one;

        // Apply material to all SkinnedMeshRenderers
        if (mat != null)
        {
            foreach (var smr in hand.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                smr.sharedMaterial = mat;
            foreach (var mr in hand.GetComponentsInChildren<MeshRenderer>(true))
                mr.sharedMaterial = mat;
        }

        Debug.Log($"[Hands] Set up hand on '{controllerName}'.");
    }

    [MenuItem("Tools/Setup Scissors LOD")]
    public static void SetupScissorsLOD()
    {
        GameObject scissors = GameObject.Find("Scissors");
        if (scissors == null) { Debug.LogError("[LOD] 'Scissors' GameObject not found."); return; }

        // Gather all MeshRenderers in the scissors hierarchy
        MeshRenderer[] renderers = scissors.GetComponentsInChildren<MeshRenderer>();
        if (renderers.Length == 0) { Debug.LogError("[LOD] No MeshRenderers found under Scissors."); return; }

        // ── Build LOD1 siblings with simplified meshes ──
        // Create a container so LOD1 meshes don't clutter the hierarchy
        GameObject lod1Root = new GameObject("Scissors_LOD1");
        lod1Root.transform.SetParent(scissors.transform);
        lod1Root.transform.localPosition = Vector3.zero;
        lod1Root.transform.localRotation = Quaternion.identity;
        lod1Root.transform.localScale    = Vector3.one;

        if (!AssetDatabase.IsValidFolder("Assets/Meshes"))
            AssetDatabase.CreateFolder("Assets", "Meshes");

        var lod1Renderers = new System.Collections.Generic.List<Renderer>();
        int totalOriginal = 0, totalSimplified = 0;

        foreach (MeshRenderer mr in renderers)
        {
            MeshFilter mf = mr.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            Mesh original  = mf.sharedMesh;
            Mesh simplified = SimplifyMesh(original, 0.5f); // keep ~50% triangles

            string savePath = $"Assets/Meshes/{original.name}_LOD1.asset";
            AssetDatabase.DeleteAsset(savePath);
            AssetDatabase.CreateAsset(simplified, savePath);

            // Create a child GO mirroring the original transform
            GameObject lod1GO = new GameObject(mr.gameObject.name + "_LOD1");
            lod1GO.transform.SetParent(lod1Root.transform);
            lod1GO.transform.position    = mr.transform.position;
            lod1GO.transform.rotation    = mr.transform.rotation;
            lod1GO.transform.localScale  = mr.transform.localScale;

            lod1GO.AddComponent<MeshFilter>().sharedMesh        = simplified;
            lod1GO.AddComponent<MeshRenderer>().sharedMaterials = mr.sharedMaterials;
            lod1Renderers.Add(lod1GO.GetComponent<Renderer>());

            totalOriginal   += original.triangles.Length / 3;
            totalSimplified += simplified.triangles.Length / 3;
        }

        AssetDatabase.SaveAssets();

        // ── Add LODGroup to Scissors root ──
        LODGroup lodGroup = scissors.GetComponent<LODGroup>();
        if (lodGroup == null) lodGroup = scissors.AddComponent<LODGroup>();

        LOD[] lods = new LOD[3];
        lods[0] = new LOD(0.15f, renderers);                              // LOD0 full at >15% screen
        lods[1] = new LOD(0.05f, lod1Renderers.ToArray());                // LOD1 simplified at >5%
        lods[2] = new LOD(0f,    new Renderer[0]);                        // culled below 5%
        lodGroup.SetLODs(lods);
        lodGroup.RecalculateBounds();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[LOD] Scissors LOD set up — LOD0: {totalOriginal:N0} tris | LOD1: {totalSimplified:N0} tris (~{100f*totalSimplified/totalOriginal:F0}%) | culled below 5% screen height");
    }

    // Simple mesh decimation: removes every Nth triangle to reduce count by ~ratio
    static Mesh SimplifyMesh(Mesh source, float keepRatio)
    {
        int[] srcTris  = source.triangles;
        int srcCount   = srcTris.Length / 3;
        int keepCount  = Mathf.Max(1, Mathf.RoundToInt(srcCount * keepRatio));

        // Step through triangles evenly
        float step = (float)srcCount / keepCount;
        var newTris = new System.Collections.Generic.List<int>(keepCount * 3);
        for (int i = 0; i < keepCount; i++)
        {
            int idx = Mathf.FloorToInt(i * step) * 3;
            newTris.Add(srcTris[idx]);
            newTris.Add(srcTris[idx + 1]);
            newTris.Add(srcTris[idx + 2]);
        }

        Mesh simplified = new Mesh();
        simplified.name     = source.name + "_LOD1";
        simplified.vertices = source.vertices;
        simplified.normals  = source.normals;
        simplified.uv       = source.uv;
        simplified.triangles = newTris.ToArray();
        simplified.RecalculateBounds();
        return simplified;
    }

    [MenuItem("Tools/Report Polygon Count")]
    public static void ReportPolygonCount()
    {
        var meshFilters = Object.FindObjectsOfType<MeshFilter>();
        var results = new System.Collections.Generic.List<(string name, int tris)>();
        int totalTris = 0;

        foreach (var mf in meshFilters)
        {
            if (mf.sharedMesh == null) continue;
            int tris = mf.sharedMesh.triangles.Length / 3;
            totalTris += tris;
            results.Add((mf.gameObject.name, tris));
        }

        results.Sort((a, b) => b.tris.CompareTo(a.tris));

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== POLYGON REPORT — {results.Count} meshes, {totalTris:N0} total triangles ===");
        foreach (var r in results)
            sb.AppendLine($"  {r.tris,8:N0}  {r.name}");

        Debug.Log(sb.ToString());
    }

    [MenuItem("Tools/Upgrade Package Materials to URP")]
    public static void UpgradeMaterials()
    {
        Shader urpLit      = Shader.Find("Universal Render Pipeline/Lit");
        Shader urpUnlit    = Shader.Find("Universal Render Pipeline/Unlit");
        Shader stdShader   = Shader.Find("Standard");

        if (urpLit == null) { Debug.LogError("[DetectiveRoom] URP/Lit shader not found."); return; }

        string[] matGuids = AssetDatabase.FindAssets("t:Material",
            new[] { "Assets/nappin", "Assets/Gogo Casual Pack" });

        int upgraded = 0;
        foreach (string guid in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            string shaderName = mat.shader != null ? mat.shader.name : "";
            // Skip already-URP materials
            if (shaderName.StartsWith("Universal Render Pipeline")) continue;

            // Grab existing properties before swapping shader
            Color   baseColor   = mat.HasProperty("_Color")    ? mat.GetColor("_Color")       : Color.white;
            Texture mainTex     = mat.HasProperty("_MainTex")  ? mat.GetTexture("_MainTex")   : null;
            float   metallic    = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic")    : 0f;
            float   smoothness  = mat.HasProperty("_Glossiness")? mat.GetFloat("_Glossiness") : 0.5f;
            Texture normalMap   = mat.HasProperty("_BumpMap")  ? mat.GetTexture("_BumpMap")   : null;
            float   emission    = mat.IsKeywordEnabled("_EMISSION") ? 1f : 0f;
            Color   emitColor   = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;

            // Unlit if old shader was Unlit
            bool useUnlit = shaderName.Contains("Unlit") || shaderName.Contains("Transparent/Diffuse");
            mat.shader = useUnlit ? urpUnlit : urpLit;

            // Remap properties Standard → URP
            if (mat.HasProperty("_BaseColor"))  mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_BaseMap") && mainTex != null)  mat.SetTexture("_BaseMap", mainTex);
            if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic",   metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_BumpMap") && normalMap != null) mat.SetTexture("_BumpMap", normalMap);
            if (emission > 0 && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emitColor);
            }

            EditorUtility.SetDirty(mat);
            upgraded++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[DetectiveRoom] Upgraded {upgraded} materials to URP.");
    }

    [MenuItem("Tools/Dress Detective Room")]
    public static void DressRoom()
    {
        GameObject room = GameObject.Find("DetectiveRoom");
        Transform roomT = room != null ? room.transform : null;

        // ── Clean up any previous dress pass ──
        foreach (string n in new[]{ "CeilingLamp","Desk","OfficeChair","DeskLamp","Mug",
                                     "BookPile","FilingCabinet","Shelves","TrashCan",
                                     "DocumentHolder","Light_RoomMain" })
        {
            GameObject old = GameObject.Find(n);
            if (old != null) Object.DestroyImmediate(old);
        }

        // ── Ceiling lamp mesh (Gogo) — decorative fixture at ceiling ──
        // Pivot is at top, hangs downward; place at y=3 (ceiling)
        SpawnPrefab(
            "Assets/Gogo Casual Pack/Gogo Casual Free Light Pack/Prefabs/Decoration_Light_CeilingLamp_01_01.prefab",
            new Vector3(0f, 3.0f, 0.5f), new Vector3(0f, 0f, 0f), roomT, "CeilingLamp");

        // ── Point light inside the lamp bulb area — warm overhead fill ──
        // (The Gogo mesh is decorative only; light comes from here)
        {
            GameObject go = new GameObject("Light_RoomMain");
            if (roomT != null) go.transform.SetParent(roomT);
            go.transform.position = new Vector3(0f, 2.5f, 0.5f); // just below fixture
            Light l = go.AddComponent<Light>();
            l.type      = LightType.Point;
            l.intensity = 2.0f;
            l.range     = 12f;
            l.color     = new Color(1.0f, 0.91f, 0.76f);
            l.shadows   = LightShadows.Soft;
        }

        // Desk measured: 1.025 wide × 0.946 tall × 1.736 deep
        // Place left-centre, facing board (+Z). Pivot at mesh centre base.
        float deskTopY = 0.946f;
        SpawnPrefab(
            "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Desk1.prefab",
            new Vector3(-1.5f, 0f, 0.3f), new Vector3(0f, 0f, 0f), roomT, "Desk");

        // Chair behind desk (detective faces board through the desk)
        // Chair 0.826 wide × 1.228 tall × 0.818 deep
        SpawnPrefab(
            "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)OfficeChair.prefab",
            new Vector3(-1.5f, 0f, -1.05f), new Vector3(0f, 0f, 0f), roomT, "OfficeChair");

        // ── Desk-top items — y = deskTopY ──
        SpawnPrefab(
            "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)DeskLight.prefab",
            new Vector3(-1.05f, deskTopY, 0.55f), new Vector3(0f, 160f, 0f), roomT, "DeskLamp");

        SpawnPrefab(
            "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Mug.prefab",
            new Vector3(-1.9f, deskTopY, 0.1f), new Vector3(0f, 35f, 0f), roomT, "Mug");

        SpawnPrefab(
            "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)BookPile2.prefab",
            new Vector3(-1.1f, deskTopY, -0.1f), new Vector3(0f, 15f, 0f), roomT, "BookPile");

        SpawnPrefab(
            "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)DocumentHolder.prefab",
            new Vector3(-1.7f, deskTopY, 0.5f), new Vector3(0f, 170f, 0f), roomT, "DocumentHolder");

        // ── Filing cabinet — right wall, flush (cabinet 0.626 wide × 1.206 deep) ──
        // Y=0: extends from x=pivot to x=pivot+0.626; place so right edge at x=3.45
        SpawnPrefab(
            "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)BigDrawer.prefab",
            new Vector3(2.82f, 0f, 1.2f), new Vector3(0f, 0f, 0f), roomT, "FilingCabinet");

        // ── Shelves — right wall, further back (shelves 0.39 wide × 1.88 tall × 1.455 deep) ──
        // Rotate Y=90: depth (1.455) runs along X; width (0.39) runs along Z
        // Place so back face sits against x=3.5 wall
        SpawnPrefab(
            "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Shelves1.prefab",
            new Vector3(2.77f, 0f, 2.8f), new Vector3(0f, 90f, 0f), roomT, "Shelves");

        // ── Trash can — corner near door ──
        SpawnPrefab(
            "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)TrashCan.prefab",
            new Vector3(-3.1f, 0f, -2.3f), new Vector3(0f, 0f, 0f), roomT, "TrashCan");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[DetectiveRoom] Room dressed with props.");
    }

    static void SpawnPrefab(string assetPath, Vector3 pos, Vector3 rot, Transform parent, string name)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[DetectiveRoom] Prefab not found: {assetPath}");
            return;
        }
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.position = pos;
        go.transform.eulerAngles = rot;
        if (parent != null) go.transform.SetParent(parent, true);
    }

    [MenuItem("Tools/Stylize Board Text")]
    public static void StylizeBoardText()
    {
        // Collect all TMP components in the scene (including inactive)
        var allTMP = Resources.FindObjectsOfTypeAll<TMPro.TextMeshPro>();
        // Also check UI variant just in case
        var allTMPUI = Resources.FindObjectsOfTypeAll<TMPro.TextMeshProUGUI>();

        int changed = 0;

        foreach (var tmp in allTMP)
        {
            changed += ApplyBoardStyle(tmp.gameObject.name, tmp.text,
                c => tmp.color = c,
                s => tmp.fontStyle = s,
                sz => tmp.fontSize = sz);
        }

        foreach (var tmp in allTMPUI)
        {
            changed += ApplyBoardStyle(tmp.gameObject.name, tmp.text,
                c => tmp.color = c,
                s => tmp.fontStyle = s,
                sz => tmp.fontSize = sz);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[DetectiveRoom] Board text styled — {changed} components updated.");
    }

    static int ApplyBoardStyle(string goName, string text,
        System.Action<Color> setColor,
        System.Action<TMPro.FontStyles> setStyle,
        System.Action<float> setSize)
    {
        // Section headers — bold red, all-caps already
        if (text == "CAMERA STILLS" || text == "SUSPECTS" || text == "EVIDENCE")
        {
            setColor(new Color(0.72f, 0.08f, 0.08f));          // deep crimson
            setStyle(TMPro.FontStyles.Bold | TMPro.FontStyles.UpperCase);
            setSize(26f);
            return 1;
        }

        // Suspect name labels
        if (text.StartsWith("Customer") || text.StartsWith("Suspect"))
        {
            setColor(new Color(0.10f, 0.12f, 0.22f));           // dark navy ink
            setStyle(TMPro.FontStyles.Bold);
            setSize(30f);
            return 1;
        }

        // The question — looks like a typed note
        if (goName == "Question")
        {
            setColor(new Color(0.13f, 0.13f, 0.13f));           // near-black typewriter
            setStyle(TMPro.FontStyles.Italic);
            setSize(34f);
            return 1;
        }

        // "Case Closed!" — bold red stamp
        if (text.Contains("Case Closed"))
        {
            setColor(new Color(0.72f, 0.08f, 0.08f));
            setStyle(TMPro.FontStyles.Bold | TMPro.FontStyles.UpperCase);
            setSize(44f);
            return 1;
        }

        // Correct feedback — muted olive green
        if (text.Contains("Match found") || text.Contains("\u2713"))
        {
            setColor(new Color(0.18f, 0.45f, 0.18f));
            setStyle(TMPro.FontStyles.Bold);
            return 1;
        }

        // Wrong feedback — dark red
        if (text.Contains("No match") || text.Contains("\u2717"))
        {
            setColor(new Color(0.60f, 0.06f, 0.06f));
            setStyle(TMPro.FontStyles.Bold);
            return 1;
        }

        return 0;
    }

    [MenuItem("Tools/Fix Room Lighting")]
    public static void FixLighting()
    {
        // Switch ambient from skybox (causes the cotton gradient) to flat solid color
        RenderSettings.ambientMode  = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.08f, 0.07f, 0.07f); // very dark neutral fill

        // Kill the skybox so nothing bleeds in from outside
        RenderSettings.skybox = null;

        // Remove any stale fog
        RenderSettings.fog = false;

        // Add lights under DetectiveRoom if they don't already exist
        GameObject room = GameObject.Find("DetectiveRoom");
        Transform roomT = room != null ? room.transform : null;

        if (GameObject.Find("Light_RoomMain") == null)
        {
            GameObject go = new GameObject("Light_RoomMain");
            if (roomT != null) go.transform.SetParent(roomT);
            go.transform.position = new Vector3(0f, 2.75f, 0.5f);
            Light l = go.AddComponent<Light>();
            l.type      = LightType.Point;
            l.intensity = 2.2f;
            l.range     = 12f;
            l.color     = new Color(1.0f, 0.91f, 0.76f); // warm incandescent
            l.shadows   = LightShadows.Soft;
        }

        if (GameObject.Find("Light_BoardSpot") == null)
        {
            GameObject go = new GameObject("Light_BoardSpot");
            if (roomT != null) go.transform.SetParent(roomT);
            go.transform.position    = new Vector3(0f, 2.8f, 2.2f);
            go.transform.eulerAngles = new Vector3(55f, 0f, 0f);
            Light l = go.AddComponent<Light>();
            l.type      = LightType.Spot;
            l.intensity = 1.5f;
            l.range     = 5f;
            l.spotAngle = 50f;
            l.color     = new Color(1.0f, 0.95f, 0.85f);
            l.shadows   = LightShadows.Soft;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[DetectiveRoom] Lighting fixed — ambient set to flat, skybox cleared, room lights added.");
    }

    static void SetColor(Material mat, Color color)
    {
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
    }
}
