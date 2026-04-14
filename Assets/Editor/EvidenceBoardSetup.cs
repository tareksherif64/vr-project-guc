using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Editor utility — run once via  Tools > VR Café Detective > Build Evidence Board
/// to generate the complete evidence board hierarchy in the open scene.
/// Safe to re-run: deletes any existing EvidenceBoard and EvidenceBoardManager
/// root objects before rebuilding.
/// </summary>
public static class EvidenceBoardSetup
{
    [MenuItem("Tools/VR Café Detective/Setup XR Rig + Floor")]
    public static void SetupXRRig()
    {
        // ── XRInteractionManager (singleton required by all XRIT interactables) ──
        if (Object.FindFirstObjectByType<XRInteractionManager>() == null)
        {
            var mgr = new GameObject("XRInteractionManager");
            mgr.AddComponent<XRInteractionManager>();
            Debug.Log("[EvidenceBoardSetup] XRInteractionManager created.");
        }

        // ── Locate XR Origin — supports both the legacy "XRRig" name and the
        //    current XR Interaction Toolkit 3.x "XR Origin (XR Rig)" name ──────
        var rig = GameObject.Find("XR Origin (XR Rig)")
               ?? GameObject.Find("XRRig")
               ?? GameObject.Find("XR Origin");

        if (rig != null)
        {
            rig.transform.SetPositionAndRotation(new Vector3(0f, 0f, 1f), Quaternion.identity);
            Debug.Log($"[EvidenceBoardSetup] XR Rig '{rig.name}' positioned at (0,0,1).");
        }
        else
        {
            Debug.LogWarning("[EvidenceBoardSetup] No XR Rig found — skipping rig setup.");
        }

        // ── Floor plane ──────────────────────────────────────────────────────────
        if (GameObject.Find("Floor") == null)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            floor.transform.localScale = new Vector3(3f, 1f, 3f); // 30×30 m

            var teleArea = floor.AddComponent<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationArea>();
            // Layer 31 = "Teleport" interaction layer — must match the Teleport Interactor on the XR rig
            teleArea.interactionLayers = UnityEngine.XR.Interaction.Toolkit.InteractionLayerMask.GetMask("Teleport");

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", new Color(0.25f, 0.25f, 0.25f));
            floor.GetComponent<Renderer>().sharedMaterial = mat;
            Debug.Log("[EvidenceBoardSetup] Floor created.");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[EvidenceBoardSetup] XR Rig setup complete.");
    }

    [MenuItem("Tools/VR Café Detective/Setup Pin Interactables")]
    public static void SetupPinInteractables()
    {
        var manager = Object.FindFirstObjectByType<EvidenceBoardManager>();
        int count = 0;

        foreach (var slot in Object.FindObjectsByType<EvidenceSlot>(FindObjectsSortMode.None))
        {
            if (slot.pinTransform == null) continue;
            var pin = slot.pinTransform.gameObject;
            WirePinComponents(pin, manager);
            count++;
        }
        foreach (var slot in Object.FindObjectsByType<PortraitSlot>(FindObjectsSortMode.None))
        {
            if (slot.pinTransform == null) continue;
            var pin = slot.pinTransform.gameObject;
            WirePinComponents(pin, manager);
            count++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log($"[EvidenceBoardSetup] Wired PinConnector on {count} pins.");
    }

    static void WirePinComponents(GameObject pin, EvidenceBoardManager manager)
    {
        // Collider
        if (pin.GetComponent<SphereCollider>() == null)
        {
            var col = pin.AddComponent<SphereCollider>();
            col.radius = 0.5f; // local space — scales with the pin's small localScale
        }

        // XR interactable
        if (pin.GetComponent<XRSimpleInteractable>() == null)
            pin.AddComponent<XRSimpleInteractable>();

        // PinConnector
        var pc = pin.GetComponent<PinConnector>();
        if (pc == null) pc = pin.AddComponent<PinConnector>();
        pc.boardManager = manager;

        EditorUtility.SetDirty(pin);
    }

    [MenuItem("Tools/VR Café Detective/Add Pins to Existing Slots")]
    public static void AddPinsToExistingSlots()
    {
        int count = 0;
        foreach (var slot in Object.FindObjectsByType<EvidenceSlot>(FindObjectsSortMode.None))
        {
            if (slot.pinTransform == null)
            {
                slot.pinTransform = CreatePin(slot.gameObject);
                EditorUtility.SetDirty(slot);
                count++;
            }
        }
        foreach (var slot in Object.FindObjectsByType<PortraitSlot>(FindObjectsSortMode.None))
        {
            if (slot.pinTransform == null)
            {
                slot.pinTransform = CreatePin(slot.gameObject);
                EditorUtility.SetDirty(slot);
                count++;
            }
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log($"[EvidenceBoardSetup] Added pins to {count} slots.");
    }

    [MenuItem("Tools/VR Café Detective/Build Evidence Board")]
    public static void BuildEvidenceBoard()
    {
        // ── clean up previous run ──────────────────────────────────────
        DestroyExisting("EvidenceBoard");
        DestroyExisting("EvidenceBoardManager");

        // ── root ──────────────────────────────────────────────────────
        var boardRoot = new GameObject("EvidenceBoard");
        boardRoot.transform.SetPositionAndRotation(
            new Vector3(0f, 1.5f, 2.5f),
            Quaternion.identity);

        // ── cork surface ──────────────────────────────────────────────
        var surface = CreateQuad("BoardSurface", boardRoot.transform,
            Vector3.zero, new Vector3(2.8f, 2.0f, 0.05f));
        SetColor(surface, new Color(0.45f, 0.30f, 0.18f)); // cork brown

        // ── screenshot slots (left column) ────────────────────────────
        float[] slotY = { 0.6f, 0f, -0.6f };
        var screenshotSlots = new EvidenceSlot[3];

        for (int i = 0; i < 3; i++)
        {
            var go = CreateQuad($"Slot_{i}", boardRoot.transform,
                new Vector3(-0.85f, slotY[i], -0.02f),
                new Vector3(0.55f, 0.45f, 0.01f));

            SetColor(go, new Color(0.85f, 0.85f, 0.85f));

            var col = go.AddComponent<BoxCollider>();
            col.size = Vector3.one;

            var xri = go.AddComponent<XRSimpleInteractable>();
            xri.interactionLayers = InteractionLayerMask.GetMask("Default");

            var slot = go.AddComponent<EvidenceSlot>();
            slot.slotIndex    = i;
            slot.pinTransform = CreatePin(go);
            screenshotSlots[i] = slot;
        }

        // ── portrait slots (right column) ─────────────────────────────
        var portraitSlots = new PortraitSlot[3];

        for (int i = 0; i < 3; i++)
        {
            var go = CreateQuad($"Portrait_{i}", boardRoot.transform,
                new Vector3(0.85f, slotY[i], -0.02f),
                new Vector3(0.55f, 0.45f, 0.01f));

            SetColor(go, new Color(0.85f, 0.85f, 0.85f));

            var col = go.AddComponent<BoxCollider>();
            col.size = Vector3.one;

            var xri = go.AddComponent<XRSimpleInteractable>();
            xri.interactionLayers = InteractionLayerMask.GetMask("Default");

            var slot = go.AddComponent<PortraitSlot>();
            slot.portraitIndex = i;
            slot.pinTransform  = CreatePin(go);
            portraitSlots[i]   = slot;
        }

        // Column labels (world-space text)
        CreateLabel("Screenshots", boardRoot.transform, new Vector3(-0.85f,  0.95f, -0.03f), "CAMERA STILLS");
        CreateLabel("Portraits",   boardRoot.transform, new Vector3( 0.85f,  0.95f, -0.03f), "SUSPECTS");
        CreateLabel("Title",       boardRoot.transform, new Vector3( 0f,     0.95f, -0.03f), "EVIDENCE BOARD");

        // ── EvidenceBoardManager ──────────────────────────────────────
        var managerGO = new GameObject("EvidenceBoardManager");

        // ── Wire up manager ───────────────────────────────────────────
        var manager = managerGO.AddComponent<EvidenceBoardManager>();

        // Back-link all slots to manager
        foreach (var s in screenshotSlots) s.boardManager = manager;
        foreach (var p in portraitSlots)   p.boardManager = manager;

        // ── Mark scene dirty ──────────────────────────────────────────
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[EvidenceBoardSetup] Evidence board built. " +
                  "Assign screenshot/portrait textures to slot MeshRenderers, " +
                  "then adjust indices on EvidenceBoardManager.");

        Selection.activeGameObject = boardRoot;
        EditorUtility.SetDirty(boardRoot);
    }

    // ── helpers ───────────────────────────────────────────────────────

    static GameObject CreateQuad(string name, Transform parent, Vector3 localPos, Vector3 localScale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = localScale;
        // Remove the default MeshCollider — slots add BoxCollider instead
        var mc = go.GetComponent<MeshCollider>();
        if (mc) Object.DestroyImmediate(mc);
        return go;
    }

    static void SetColor(GameObject go, Color color)
    {
        var renderer = go.GetComponent<Renderer>();
        if (renderer == null) return;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", color);
        renderer.sharedMaterial = mat;
    }

    static void CreateLabel(string name, Transform parent, Vector3 localPos, string text)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200f, 40f);

        var tmpGO = new GameObject("Text");
        tmpGO.transform.SetParent(go.transform, false);
        var tmp = tmpGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = 28;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        var trt = tmpGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
    }

    static GameObject BuildQuizCanvas(GameObject parent)
    {
        var canvasGO = new GameObject("QuizPanel");
        canvasGO.transform.SetParent(parent.transform, false);
        canvasGO.transform.SetPositionAndRotation(
            new Vector3(0f, 1.5f, 2.0f), Quaternion.identity);
        canvasGO.transform.localScale = new Vector3(0.002f, 0.002f, 0.002f);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(700f, 400f);

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var bg = new GameObject("Background");
        bg.transform.SetParent(canvasGO.transform, false);
        var bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.92f);
        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        var questionGO = new GameObject("Question");
        questionGO.transform.SetParent(canvasGO.transform, false);
        var question = questionGO.AddComponent<TextMeshProUGUI>();
        question.text      = "Who paid with cash?";
        question.fontSize  = 48;
        question.fontStyle = FontStyles.Bold;
        question.color     = Color.white;
        question.alignment = TextAlignmentOptions.Center;
        var qRT = questionGO.GetComponent<RectTransform>();
        qRT.anchorMin = new Vector2(0.05f, 0.6f);
        qRT.anchorMax = new Vector2(0.95f, 0.95f);
        qRT.offsetMin = qRT.offsetMax = Vector2.zero;

        string[] npcNames = { "Customer A", "Customer B", "Customer C" };
        float[] btnY      = { 0.42f, 0.24f, 0.06f };

        for (int i = 0; i < 3; i++)
        {
            var btnGO = new GameObject($"CashBtn_{i}");
            btnGO.transform.SetParent(canvasGO.transform, false);
            var img = btnGO.AddComponent<Image>();
            img.color = new Color(0.2f, 0.5f, 0.9f);
            btnGO.AddComponent<Button>();

            var bRT = btnGO.GetComponent<RectTransform>();
            bRT.anchorMin = new Vector2(0.15f, btnY[i]);
            bRT.anchorMax = new Vector2(0.85f, btnY[i] + 0.14f);
            bRT.offsetMin = bRT.offsetMax = Vector2.zero;

            var lblGO = new GameObject("Label");
            lblGO.transform.SetParent(btnGO.transform, false);
            var lbl = lblGO.AddComponent<TextMeshProUGUI>();
            lbl.text      = npcNames[i];
            lbl.fontSize  = 36;
            lbl.color     = Color.white;
            lbl.alignment = TextAlignmentOptions.Center;
            var lRT = lblGO.GetComponent<RectTransform>();
            lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one;
            lRT.offsetMin = lRT.offsetMax = Vector2.zero;
        }

        return canvasGO;
    }

    static GameObject BuildAccusationCanvas(GameObject parent)
    {
        var canvasGO = new GameObject("AccusationPanel");
        canvasGO.transform.SetParent(parent.transform, false);
        canvasGO.transform.SetPositionAndRotation(
            new Vector3(0f, 1.5f, 2.0f), Quaternion.identity);
        canvasGO.transform.localScale = new Vector3(0.002f, 0.002f, 0.002f);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(700f, 400f);

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var bg = new GameObject("Background");
        bg.transform.SetParent(canvasGO.transform, false);
        var bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.05f, 0.25f, 0.05f, 0.92f);
        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        var resultGO = new GameObject("ResultText");
        resultGO.transform.SetParent(canvasGO.transform, false);
        var result = resultGO.AddComponent<TextMeshProUGUI>();
        result.text      = "Case Closed!\n\nYou identified the thief.\nThe cash payer was the culprit.";
        result.fontSize  = 44;
        result.fontStyle = FontStyles.Bold;
        result.color     = Color.white;
        result.alignment = TextAlignmentOptions.Center;
        var rRT = resultGO.GetComponent<RectTransform>();
        rRT.anchorMin = new Vector2(0.05f, 0.3f);
        rRT.anchorMax = new Vector2(0.95f, 0.92f);
        rRT.offsetMin = rRT.offsetMax = Vector2.zero;

        var btnGO = new GameObject("ContinueBtn");
        btnGO.transform.SetParent(canvasGO.transform, false);
        var img = btnGO.AddComponent<Image>();
        img.color = new Color(0.2f, 0.7f, 0.3f);
        btnGO.AddComponent<Button>();
        var bRT = btnGO.GetComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0.25f, 0.06f);
        bRT.anchorMax = new Vector2(0.75f, 0.22f);
        bRT.offsetMin = bRT.offsetMax = Vector2.zero;

        var lblGO = new GameObject("Label");
        lblGO.transform.SetParent(btnGO.transform, false);
        var lbl = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text      = "Continue";
        lbl.fontSize  = 36;
        lbl.color     = Color.white;
        lbl.alignment = TextAlignmentOptions.Center;
        var lRT = lblGO.GetComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one;
        lRT.offsetMin = lRT.offsetMax = Vector2.zero;

        return canvasGO;
    }

    /// <summary>
    /// Creates a small red pushpin sphere as a child of the slot.
    /// The pin sits at the center of the slot face, slightly in front.
    /// </summary>
    static Transform CreatePin(GameObject slotGO)
    {
        var pin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pin.name = "Pin";
        pin.transform.SetParent(slotGO.transform, false);
        // Sit slightly in front of the quad face (local Z = –1 in Quad space = forward in world)
        pin.transform.localPosition = new Vector3(0f, 0f, -1.5f);
        // Scale relative to the slot — small pinhead
        pin.transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);

        // Remove default sphere collider — we add our own below with correct radius
        var defaultCol = pin.GetComponent<Collider>();
        if (defaultCol) Object.DestroyImmediate(defaultCol);

        // Interaction collider (radius in local space — 0.5 fills the unit sphere)
        var col = pin.AddComponent<SphereCollider>();
        col.radius = 0.5f;

        // XR interactable + connector (boardManager assigned later in SetupPinInteractables)
        pin.AddComponent<XRSimpleInteractable>();
        pin.AddComponent<PinConnector>();

        // Red metallic pin material
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", new Color(0.85f, 0.1f, 0.1f));
        mat.SetFloat("_Metallic", 0.6f);
        mat.SetFloat("_Smoothness", 0.7f);
        pin.GetComponent<Renderer>().sharedMaterial = mat;

        return pin.transform;
    }

    static void DestroyExisting(string name)
    {
        var existing = GameObject.Find(name);
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
            Debug.Log($"[EvidenceBoardSetup] Removed existing '{name}'");
        }
    }
}

// Fluent Destroy helper
static class ComponentExt
{
    public static void Destroy(this Component c)
    {
        if (c != null) Object.DestroyImmediate(c);
    }
}
