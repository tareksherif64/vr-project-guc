using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System;

/// <summary>
/// Run  Cafe ▶ Setup Physical Payments  to:
///   1. Make Eftpos machine permanent (always active) + add CardReader trigger
///   2. Add CashItem + XRGrabInteractable + Rigidbody to the cash prefab
///   3. Create a CashRegister trigger zone on the counter
///   4. Create a PaymentCard quad near the cashier
///   5. Wire Player fields for all new objects
/// </summary>
public static class PaymentSetup
{
    // Positions relative to the counter layout used by CafeVRSetup
    // Counter player side Z ≈ -9,  cashier side Z ≈ -8.2,  counter top Y ≈ 1.05

    [MenuItem("Cafe/Setup Physical Payments")]
    public static void SetupPayments()
    {
        int fixes = 0;

        // ── 1. Make Eftpos machine permanent ────────────────────────────────
        // The prefab instance name override is "Eftpos machine"
        // We use Resources.FindObjectsOfTypeAll to find inactive objects
        var allGOs = Resources.FindObjectsOfTypeAll<GameObject>();

        GameObject eftpos = null;
        GameObject cashGO = null;

        foreach (var go in allGOs)
        {
            if (go.scene.name == null) continue; // prefab asset, skip
            if (go.name == "Eftpos machine") eftpos = go;
            if (go.name == "cash")           cashGO = go;
        }

        // ── Eftpos machine → permanent, add CardReader ───────────────────────
        if (eftpos == null)
        {
            Debug.LogWarning("[PaymentSetup] 'Eftpos machine' not found. Add CardReader manually.");
        }
        else
        {
            Undo.RecordObject(eftpos, "Make Eftpos Permanent");
            eftpos.SetActive(true);
            EditorUtility.SetDirty(eftpos);
            fixes++;

            // XRSocketInteractor is required by CardReader — add via reflection
            Type socketType = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                socketType = asm.GetType("UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor");
                if (socketType != null) break;
            }

            if (socketType == null)
            {
                Debug.LogError("[PaymentSetup] XRSocketInteractor type not found — is XRIT installed?");
            }
            else
            {
                if (eftpos.GetComponent(socketType) == null)
                {
                    Undo.AddComponent(eftpos, socketType);
                    fixes++;
                }

                if (eftpos.GetComponent<CardReader>() == null)
                {
                    Undo.AddComponent<CardReader>(eftpos);
                    fixes++;
                }

                Debug.Log("[PaymentSetup] XRSocketInteractor + CardReader ensured on Eftpos machine.");
            }
        }

        // ── Cash object → grab-able, add CashItem ───────────────────────────
        if (cashGO == null)
        {
            Debug.LogWarning("[PaymentSetup] 'cash' not found. Add CashItem manually.");
        }
        else
        {
            // Do NOT activate yet — activated by Player.PayWithCash()

            if (cashGO.GetComponent<Rigidbody>() == null)
            {
                var rb = Undo.AddComponent<Rigidbody>(cashGO);
                rb.mass = 0.1f;
                fixes++;
            }

            if (cashGO.GetComponent<CashItem>() == null)
            {
                Undo.AddComponent<CashItem>(cashGO);
                fixes++;
            }

            // Add XRGrabInteractable via reflection (avoids hard assembly ref in Editor)
            Type grabType = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                grabType = asm.GetType("UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable");
                if (grabType != null) break;
            }
            if (grabType != null && cashGO.GetComponent(grabType) == null)
            {
                Undo.AddComponent(cashGO, grabType);
                fixes++;
                Debug.Log("[PaymentSetup] XRGrabInteractable added to cash.");
            }
        }

        // ── CashRegister trigger zone ─────────────────────────────────────────
        var existingRegister = GameObject.Find("CashRegister");
        if (existingRegister == null)
        {
            existingRegister = new GameObject("CashRegister");
            Undo.RegisterCreatedObjectUndo(existingRegister, "Create CashRegister");

            var col = existingRegister.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size   = new Vector3(0.5f, 0.3f, 0.4f);
            col.center = new Vector3(0f, 0.15f, 0f);

            existingRegister.AddComponent<CashRegister>();

            // Place on right side of counter (player side), adjust in editor as needed
            existingRegister.transform.position = new Vector3(0.6f, 1.05f, -9.0f);

            // Green visual hint
            var hint = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hint.name = "RegisterVisual";
            hint.transform.SetParent(existingRegister.transform, false);
            hint.transform.localPosition = Vector3.zero;
            hint.transform.localScale    = new Vector3(0.5f, 0.05f, 0.4f);
            UnityEngine.Object.DestroyImmediate(hint.GetComponent<BoxCollider>());
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.3f, 0.6f, 1f, 0.6f);
            hint.GetComponent<Renderer>().sharedMaterial = mat;

            fixes++;
            Debug.Log("[PaymentSetup] CashRegister created at " + existingRegister.transform.position);
        }

        // ── PaymentCard quad ──────────────────────────────────────────────────
        var existingCard = GameObject.Find("PaymentCard");
        foreach (var go in allGOs)
            if (go.scene.name != null && go.name == "PaymentCard") { existingCard = go; break; }

        if (existingCard == null)
        {
            // Use a Cube primitive so we get a BoxCollider (Quad gives non-convex MeshCollider)
            existingCard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            existingCard.name = "PaymentCard";
            Undo.RegisterCreatedObjectUndo(existingCard, "Create PaymentCard");

            // Credit-card proportions (meters): 85.6 × 54 × 3 mm
            existingCard.transform.localScale = new Vector3(0.086f, 0.054f, 0.003f);
            existingCard.transform.position   = Vector3.zero; // placed at runtime
            existingCard.SetActive(false);

            // Card appearance
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.18f, 0.22f, 0.65f);
            existingCard.GetComponent<Renderer>().sharedMaterial = mat;

            // Physics + grab
            var rb  = existingCard.AddComponent<Rigidbody>();
            rb.mass = 0.05f;

            Type grabType = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                grabType = asm.GetType("UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable");
                if (grabType != null) break;
            }
            if (grabType != null) existingCard.AddComponent(grabType);

            existingCard.AddComponent<PaymentCard>();

            fixes++;
            Debug.Log("[PaymentSetup] PaymentCard created (Cube with BoxCollider).");
        }

        // ── Card spawn point ──────────────────────────────────────────────────
        var existingSpawnPoint = GameObject.Find("CardSpawnPoint");
        if (existingSpawnPoint == null)
        {
            existingSpawnPoint = new GameObject("CardSpawnPoint");
            Undo.RegisterCreatedObjectUndo(existingSpawnPoint, "Create CardSpawnPoint");
            // Place flat on the counter surface, customer side, centre X
            // Counter top Y ≈ 1.05, customer-facing side Z ≈ -7.9
            existingSpawnPoint.transform.position = new Vector3(0f, 1.08f, -7.9f);
            existingSpawnPoint.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // flat on counter
            fixes++;
            Debug.Log("[PaymentSetup] CardSpawnPoint created — move it in the Inspector to the exact counter spot.");
        }

        // ── Wire Player fields ────────────────────────────────────────────────
        var player = UnityEngine.Object.FindFirstObjectByType<Player>();
        if (player != null)
        {
            Undo.RecordObject(player, "Wire Player payment fields");

            if (cashGO != null && player.cashObject == null)
                player.cashObject = cashGO.GetComponent<CashItem>();
            if (player.cashRegister == null)
                player.cashRegister = existingRegister?.GetComponent<CashRegister>();
            if (player.paymentCard == null)
                player.paymentCard = existingCard?.GetComponent<PaymentCard>();
            if (eftpos != null && player.cardReader == null)
                player.cardReader = eftpos.GetComponent<CardReader>();
            if (player.cardSpawnPoint == null)
                player.cardSpawnPoint = existingSpawnPoint?.transform;

            EditorUtility.SetDirty(player);
            fixes++;
            Debug.Log("[PaymentSetup] Player fields wired.");
        }
        else
        {
            Debug.LogWarning("[PaymentSetup] Player script not found — wire fields manually.");
        }

        if (fixes > 0)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[PaymentSetup] Done (" + fixes + " change(s)).");
    }
}
