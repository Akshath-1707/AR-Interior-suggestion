using System.Collections.Generic;
using UnityEngine;

namespace ARInterior
{
    /// <summary>
    /// ARUIManager: Minimalist, clean on-screen UI overlay for testing in Unity and on mobile.
    /// Provides the Apple-style HUD, Pin trigger, Lock Position button, and Furniture Spawner buttons.
    /// </summary>
    public class ARUIManager : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private ARSpaceMeasurer measurer;
        [SerializeField] private FurnitureSpawner spawner;
        [SerializeField] private RecommendationApiClient apiClient;

        [Header("Furniture Models (Assign your 3D models here if you have them)")]
        [SerializeField] private GameObject sofa3DModel;
        [SerializeField] private GameObject chair3DModel;
        [SerializeField] private GameObject desk3DModel;

        private List<SimpleRecommendation> currentSuggestions = new List<SimpleRecommendation>();

        private void Start()
        {
            if (measurer == null) measurer = FindObjectOfType<ARSpaceMeasurer>();
            if (spawner == null) spawner = FindObjectOfType<FurnitureSpawner>();
            if (apiClient == null) apiClient = FindObjectOfType<RecommendationApiClient>();

            if (measurer != null)
            {
                measurer.OnMeasurementLocked += HandleMeasurementComplete;
            }
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
            GUI.skin.label.fontSize = 16;
            GUI.skin.button.fontSize = 15;

            // 1. Center Screen Crosshair / Reticle
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            GUI.Box(new Rect(cx - 5, cy - 5, 10, 10), "");

            // 2. Top Info Box (Measurement Status)
            GUILayout.BeginArea(new Rect(20, 20, Screen.width - 40, 160));
            GUILayout.BeginVertical("box");

            if (!measurer.isMeasuring && !measurer.isMeasurementLocked)
            {
                GUILayout.Label("📏 <b>AR Tape Measure:</b> Aim camera at floor and click to set <b>Pin A</b>");
            }
            else if (measurer.isMeasuring && !measurer.isMeasurementLocked)
            {
                GUILayout.Label($"📏 <b>Measuring Distance:</b> <color=yellow>{measurer.currentDistanceCm} cm</color>");
                GUILayout.Label("Walk or move phone to target location, then click to set <b>Pin B</b>");
            }
            else if (measurer.isMeasurementLocked)
            {
                GUILayout.Label($"🔒 <b>Space Measured:</b> <color=green>{measurer.lockedWidthCm} cm (W) × {measurer.lockedDepthCm} cm (D)</color>");
                GUILayout.Label("Select recommended furniture below to place in your space:");
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();

            // 3. Bottom Controls Area
            float bottomY = Screen.height - 180;
            GUILayout.BeginArea(new Rect(20, bottomY, Screen.width - 40, 160));
            GUILayout.BeginVertical();

            // Button Row: Reset Measurement & Lock Position
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

            // Furniture Placement Buttons (When Space is Measured)
            if (measurer.isMeasurementLocked)
            {
                GUILayout.Space(10);
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
