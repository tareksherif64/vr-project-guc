using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System;
using TMPro;

/// <summary>
/// Run  Cafe ▶ Setup VR Drink Items  to place placeholder drink objects
/// and a DeliveryZone on the counter in Menna'sCafe.
///
/// Positions assume:
///   Counter / Cashier trigger ≈ Z = -7.84
///   Player stands behind counter ≈ Z = -9 to -10
///   Drinks placed on player-accessible counter top at Z = -9
///   DeliveryZone on counter surface facing customers at Z = -8.2
/// </summary>
public static class CafeVRSetup
{
    // (ItemType, display name, price, placeholder color)
    static readonly (Customer.ItemType type, string displayName, float price, Color color)[] Drinks =
    {
        (Customer.ItemType.Cupcake, "Cupcake", 6.50f, new Color(0.65f, 0.40f, 0.18f)),
        (Customer.ItemType.Croissant , "Croissant",        5.50f, new Color(0.42f, 0.60f, 0.33f)),
        (Customer.ItemType.Espresso,     "Espresso",      4.00f, new Color(0.24f, 0.13f, 0.07f)),
    };

    [MenuItem("Cafe/Setup VR Drink Items")]
    public static void SetupDrinkItems()
    {
        // ── Drink items on player side of counter ───────────────────────────
        float counterY   = 1.05f;
        float counterZ   = -9.0f;    // player-accessible side
        float spacing    = 0.35f;
        float startX     = -spacing * (Drinks.Length - 1) / 2f;

        var counter = new GameObject("DrinkCounter");
        Undo.RegisterCreatedObjectUndo(counter, "Setup VR Drink Items");

        for (int i = 0; i < Drinks.Length; i++)
        {
            var (type, displayName, price, color) = Drinks[i];
            var item = CreateDrinkItem(type, displayName, price, color);
            item.transform.SetParent(counter.transform, false);
            item.transform.localPosition = new Vector3(startX + i * spacing, counterY, counterZ);
        }

        // ── DeliveryZone on counter facing customers ─────────────────────────
        var zone = CreateDeliveryZone();
        zone.transform.SetParent(counter.transform, false);
        zone.transform.localPosition = new Vector3(0f, counterY, -8.2f);

        // ── Auto-wire every Customer in the scene to this DeliveryZone ───────
        var dz        = zone.GetComponent<DeliveryZone>();
        var customers = UnityEngine.Object.FindObjectsOfType<Customer>();
        foreach (var c in customers)
        {
            c.deliveryZone = dz;
            EditorUtility.SetDirty(c);
        }

        Selection.activeGameObject = counter;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[CafeVRSetup] Done. Created {Drinks.Length} drink items + DeliveryZone. " +
                  $"Wired {customers.Length} Customer(s).");
    }

    // ────────────────────────────────────────────────────────────────────────
    static GameObject CreateDrinkItem(Customer.ItemType type, string displayName, float price, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = type.ToString() + "_Item";
        go.transform.localScale = new Vector3(0.07f, 0.10f, 0.07f);

        // Colored material
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        go.GetComponent<Renderer>().sharedMaterial = mat;

        // World-space price label (faces +Z so player can read it)
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        labelGO.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        labelGO.transform.localScale    = Vector3.one * 0.1f;
        var tmp       = labelGO.AddComponent<TextMeshPro>();
        tmp.text      = $"{displayName}\n${price:F2}";
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize  = 4f;
        tmp.color     = Color.white;

        // Physics
        var rb  = go.AddComponent<Rigidbody>();
        rb.mass = 0.3f;

        // VR grab
        go.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        // Drink marker
        var drink      = go.AddComponent<DrinkItem>();
        drink.itemType = type;
        drink.price    = price;

        return go;
    }

    // ════════════════════════════════════════════════════════════════════════
    // Camera VR Controls setup
    // ════════════════════════════════════════════════════════════════════════

    static readonly string[] CustomerNames = { "customer01", "customer02", "customer03" };
    static readonly string[] CustomerIDs   = { "Customer1",  "Customer2",  "Customer3"  };

    [MenuItem("Cafe/Setup Camera VR Controls")]
    public static void SetupCameraVR()
    {
        int fixes = 0;

        // ── 1. Fix ScreenControls canvas ────────────────────────────────────
        var screenControls = GameObject.Find("ScreenControls");
        if (screenControls != null)
        {
            var legacy = screenControls.GetComponent<GraphicRaycaster>();
            if (legacy != null)
            {
                Undo.DestroyObjectImmediate(legacy);
                fixes++;
                Debug.Log("[CameraVRSetup] Removed GraphicRaycaster from ScreenControls.");
            }

            Type trackedType = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                trackedType = asm.GetType(
                    "UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster");
                if (trackedType != null) break;
            }

            if (trackedType == null)
                Debug.LogError("[CameraVRSetup] TrackedDeviceGraphicRaycaster not found — " +
                               "is XR Interaction Toolkit installed?");
            else if (screenControls.GetComponent(trackedType) == null)
            {
                Undo.AddComponent(screenControls, trackedType);
                fixes++;
                Debug.Log("[CameraVRSetup] Added TrackedDeviceGraphicRaycaster to ScreenControls.");
            }
            else
            {
                Debug.Log("[CameraVRSetup] ScreenControls already has TrackedDeviceGraphicRaycaster.");
            }
        }
        else
        {
            Debug.LogWarning("[CameraVRSetup] 'ScreenControls' not found in scene.");
        }

        // ── 2. Add ReplayRecorder to each customer ───────────────────────────
        for (int i = 0; i < CustomerNames.Length; i++)
        {
            string goName = CustomerNames[i];
            string id     = CustomerIDs[i];

            var go = GameObject.Find(goName);
            if (go == null)
            {
                Debug.LogWarning("[CameraVRSetup] '" + goName + "' not found — skipping.");
                continue;
            }

            var recorder = go.GetComponent<ReplayRecorder>();
            if (recorder == null)
            {
                recorder = Undo.AddComponent<ReplayRecorder>(go);
                fixes++;
            }

            if (recorder.actorID != id)
            {
                Undo.RecordObject(recorder, "Set customerID");
                recorder.actorID = id;
                EditorUtility.SetDirty(recorder);
                fixes++;
            }

            Debug.Log("[CameraVRSetup] " + goName + " -> ReplayRecorder.customerID = " + id);
        }

        if (fixes > 0)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[CameraVRSetup] Done (" + fixes + " change(s)).");
    }

    // ════════════════════════════════════════════════════════════════════════

    static GameObject CreateDeliveryZone()
    {
        var zone      = new GameObject("DeliveryZone");
        var col       = zone.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size      = new Vector3(1.0f, 0.4f, 0.5f);
        col.center    = new Vector3(0f, 0.2f, 0f);
        zone.AddComponent<DeliveryZone>();

        // Green visual hint so you can see it in the editor (disable in production)
        var hint = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hint.name = "ZoneVisual";
        hint.transform.SetParent(zone.transform, false);
        hint.transform.localPosition = new Vector3(0f, 0.01f, 0f);
        hint.transform.localScale    = new Vector3(1.0f, 0.02f, 0.5f);
        UnityEngine.Object.DestroyImmediate(hint.GetComponent<BoxCollider>());
        var hintMat   = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        hintMat.color = new Color(0.2f, 0.8f, 0.2f, 0.5f);
        hint.GetComponent<Renderer>().sharedMaterial = hintMat;

        return zone;
    }
}
