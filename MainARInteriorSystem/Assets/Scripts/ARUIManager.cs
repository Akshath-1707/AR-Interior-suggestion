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

        private void OnGUI()
        {
            if (measurer == null || spawner == null)
            {
                FindReferences();
                if (measurer == null) return; // Prevent any null-reference crash in OnGUI
            }

            GUI.skin.label.fontSize = 15;
            GUI.skin.button.fontSize = 14;

            // 1. Center Screen Crosshair
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            GUI.Box(new Rect(cx - 5, cy - 5, 10, 10), "");

            // 2. Top Info Box (Measurement Status)
            GUILayout.BeginArea(new Rect(20, 20, Screen.width - 40, 150));
            GUILayout.BeginVertical("box");

            if (!measurer.isMeasuring && !measurer.isMeasurementLocked)
            {
                GUILayout.Label("📏 <b>AR Tape Measure:</b> Aim at floor and click to set <b>Pin A</b>");
            }
            else if (measurer.isMeasuring && !measurer.isMeasurementLocked)
            {
                GUILayout.Label($"📏 <b>Measuring:</b> <color=yellow>{measurer.currentDistanceCm} cm</color>");
                GUILayout.Label("Move camera to target point, then click to set <b>Pin B</b>");
            }
            else if (measurer.isMeasurementLocked)
            {
                GUILayout.Label($"🔒 <b>Space Measured:</b> <color=green>{measurer.lockedWidthCm} cm (W) × {measurer.lockedDepthCm} cm (D)</color>");
                GUILayout.Label("Tap a button below to place furniture in this space:");
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();

            // 3. Bottom Controls Area
            float bottomY = Screen.height - 170;
            GUILayout.BeginArea(new Rect(20, bottomY, Screen.width - 40, 150));
            GUILayout.BeginVertical();

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

            // Placement Buttons
            if (measurer.isMeasurementLocked)
            {
                GUILayout.Space(8);
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("🛋️ Place Sofa", GUILayout.Height(45)))
                {
                    Vector3 center = measurer.GetMeasurementCenter();
                    spawner.SpawnFurniture(sofa3DModel, center, Quaternion.identity);
                }

                if (GUILayout.Button("🪑 Place Chair", GUILayout.Height(45)))
                {
                    Vector3 center = measurer.GetMeasurementCenter();
                    spawner.SpawnFurniture(chair3DModel, center, Quaternion.identity);
                }

                if (GUILayout.Button("🖥️ Place Desk", GUILayout.Height(45)))
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
