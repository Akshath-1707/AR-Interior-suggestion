using System.Collections.Generic;
using UnityEngine;

namespace ARInterior
{
    /// <summary>
    /// ARUIManager: Crash-safe, bulletproof on-screen UI overlay for testing in Unity and on mobile.
    /// Provides Apple-style HUD, Pin trigger, Lock Position button, and Furniture Spawner buttons.
    /// </summary>
    public class ARUIManager : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private ARSpaceMeasurer measurer;
        [SerializeField] private FurnitureSpawner spawner;
        [SerializeField] private RecommendationApiClient apiClient;

        [Header("Furniture Models (Optional Prefabs)")]
        [SerializeField] private GameObject sofa3DModel;
        [SerializeField] private GameObject chair3DModel;
        [SerializeField] private GameObject desk3DModel;

        private List<SimpleRecommendation> currentSuggestions = new List<SimpleRecommendation>();

        private static Rect topUIRect;
        private static Rect bottomUIRect;

        private void Start()
        {
            FindReferences();

            if (measurer != null)
            {
                measurer.OnMeasurementLocked += HandleMeasurementComplete;
            }
        }

        private void FindReferences()
        {
            if (measurer == null) measurer = FindObjectOfType<ARSpaceMeasurer>();
            if (spawner == null) spawner = FindObjectOfType<FurnitureSpawner>();
            if (apiClient == null) apiClient = FindObjectOfType<RecommendationApiClient>();
        }

        private void HandleMeasurementComplete(float widthCm, float depthCm)
        {
            if (apiClient != null)
            {
                apiClient.RequestRecommendations(widthCm, depthCm, "", (results) =>
                {
                    currentSuggestions = results;
                });
            }
        }

        /// <summary>
        /// Check if a screen coordinate falls inside any active UI element to prevent accidental floor raycasts.
        /// </summary>
        public static bool IsPositionOverUI(Vector2 screenPos)
        {
            float guiY = Screen.height - screenPos.y;
            Vector2 guiPoint = new Vector2(screenPos.x, guiY);

            if (topUIRect.Contains(guiPoint)) return true;
            if (bottomUIRect.Contains(guiPoint)) return true;

            return false;
        }

        private void OnGUI()
        {
            if (measurer == null || spawner == null)
            {
                FindReferences();
                if (measurer == null) return;
            }

            GUI.skin.label.fontSize = 15;
            GUI.skin.button.fontSize = 14;

            // 1. Center Screen Crosshair (Reticle)
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            GUI.Box(new Rect(cx - 6, cy - 6, 12, 12), "");

            // 2. Top Info Box (Measurement Status & Controls)
            topUIRect = new Rect(20, 20, Screen.width - 40, 160);
            GUILayout.BeginArea(topUIRect);
            GUILayout.BeginVertical("box");

            if (!measurer.isMeasuring && !measurer.isMeasurementLocked)
            {
                GUILayout.Label("📏 <b>AR Tape Measure:</b> Aim crosshair at floor, or click anywhere on floor.");
                GUILayout.Space(4);
                if (GUILayout.Button("📍 Drop Pin A (at Crosshair)", GUILayout.Height(40)))
                {
                    measurer.DropPinAtReticle();
                }
            }
            else if (measurer.isMeasuring && !measurer.isMeasurementLocked)
            {
                GUILayout.Label($"📏 <b>Measuring:</b> <color=yellow>{measurer.currentDistanceCm} cm</color>");
                GUILayout.Label("Move phone / mouse to target point, then drop Pin B:");
                GUILayout.Space(4);
                if (GUILayout.Button($"📍 Drop Pin B & Lock ({measurer.currentDistanceCm} cm)", GUILayout.Height(40)))
                {
                    measurer.DropPinAtReticle();
                }
            }
            else if (measurer.isMeasurementLocked)
            {
                GUILayout.Label($"🔒 <b>Space Measured:</b> <color=#90EE90>{measurer.lockedWidthCm} cm (W) × {measurer.lockedDepthCm} cm (D)</color>");
                if (currentSuggestions != null && currentSuggestions.Count > 0)
                {
                    GUILayout.Label($"🤖 <b>AI Fit:</b> {currentSuggestions[0].name} ({currentSuggestions[0].match_score}% Match)");
                }
                else
                {
                    GUILayout.Label("Select a furniture model below to stage into room:");
                }
            }

            GUILayout.Space(2);
            GUILayout.Label("<size=11><color=#CCCCCC>Editor Hint: Hold Right-Click + W/A/S/D to fly camera | Or click on floor</color></size>");

            GUILayout.EndVertical();
            GUILayout.EndArea();

            // 3. Bottom Controls Area (Reset, Lock, and Furniture Buttons)
            float bottomHeight = measurer.isMeasurementLocked ? 170 : 80;
            float bottomY = Screen.height - bottomHeight - 20;
            bottomUIRect = new Rect(20, bottomY, Screen.width - 40, bottomHeight);

            GUILayout.BeginArea(bottomUIRect);
            GUILayout.BeginVertical("box");

            // Reset Tape & Lock Position Row
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🔄 Reset Tape", GUILayout.Height(45)))
            {
                measurer.ResetMeasurement();
                currentSuggestions.Clear();
            }

            if (GUILayout.Button("🔒 LOCK POSITION", GUILayout.Height(45)))
            {
                if (spawner != null)
                {
                    spawner.LockCurrentObject();
                }
            }
            GUILayout.EndHorizontal();

            // Placement Buttons (shown once space is measured)
            if (measurer.isMeasurementLocked)
            {
                GUILayout.Space(6);
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("🛋️ Sofa", GUILayout.Height(45)))
                {
                    Vector3 center = measurer.GetMeasurementCenter();
                    spawner.SpawnFurniture(sofa3DModel, center, Quaternion.identity);
                }

                if (GUILayout.Button("🪑 Chair", GUILayout.Height(45)))
                {
                    Vector3 center = measurer.GetMeasurementCenter();
                    spawner.SpawnFurniture(chair3DModel, center, Quaternion.identity);
                }

                if (GUILayout.Button("🖥️ Desk", GUILayout.Height(45)))
                {
                    Vector3 center = measurer.GetMeasurementCenter();
                    spawner.SpawnFurniture(desk3DModel, center, Quaternion.identity);
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
