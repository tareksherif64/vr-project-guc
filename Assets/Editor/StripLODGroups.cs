using UnityEngine;
using UnityEditor;

public static class StripLODGroups
{
    [MenuItem("Tools/Strip LOD Groups (Keep Lowest Poly)")]
    public static void Execute()
    {
        var lodGroups = Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None);

        if (lodGroups.Length == 0)
        {
            Debug.Log("StripLODGroups: No LODGroup components found in the scene.");
            return;
        }

        int processed = 0;

        foreach (var lodGroup in lodGroups)
        {
            LOD[] lods = lodGroup.GetLODs();

            if (lods.Length == 0)
            {
                Object.DestroyImmediate(lodGroup);
                continue;
            }

            // Last LOD = lowest polygon count
            LOD lowestLOD = lods[lods.Length - 1];

            // Collect all renderers across all LODs
            for (int i = 0; i < lods.Length; i++)
            {
                foreach (Renderer r in lods[i].renderers)
                {
                    if (r == null) continue;

                    if (i == lods.Length - 1)
                    {
                        // Keep lowest-poly renderers enabled
                        r.enabled = true;
                    }
                    else
                    {
                        // Disable (or destroy) higher-poly renderers
                        // We destroy the GameObject only if it's a dedicated LOD child;
                        // otherwise just disable the renderer to stay safe.
                        r.enabled = false;
                    }
                }
            }

            // Remove the LODGroup so Unity stops switching
            Undo.DestroyObjectImmediate(lodGroup);
            processed++;
        }

        EditorUtility.SetDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()[0]);
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log($"StripLODGroups: Processed {processed} LODGroup(s). Lowest-poly LOD kept, higher LODs disabled, LODGroup components removed.");
    }
}
