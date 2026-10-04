using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARInterior
{
    /// <summary>
    /// ARSpaceMeasurer: Apple-style AR Tape Measure.
    /// Drops Pin A, stretches a line while moving the camera, and drops Pin B to measure space.
    /// Works in both Mobile AR and Unity XR Simulation (Editor Play Mode).
    /// </summary>
    public class ARSpaceMeasurer : MonoBehaviour
    {
        [Header("AR Foundation Managers")]
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private GameObject pinPrefab;

        [Header("Live Measurement Stats (cm)")]
        public float currentDistanceCm = 0f;
        public float lockedWidthCm = 0f;
        public float lockedDepthCm = 0f;
        public bool isMeasuring = false;
        public bool isMeasurementLocked = false;

        private Vector3 pointAPosition;
        private Vector3 pointBPosition;
        private GameObject pinAInstance;
        private GameObject pinBInstance;
        private static List<ARRaycastHit> hits = new List<ARRaycastHit>();

        public event Action<float, float> OnMeasurementLocked;

        private void Start()
        {
            if (raycastManager == null)
            {
                raycastManager = GetComponent<ARRaycastManager>();
            }

            // Setup LineRenderer for the tape measure line
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
                lineRenderer.startWidth = 0.02f;
                lineRenderer.endWidth = 0.02f;
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                lineRenderer.startColor = Color.white;
                lineRenderer.endColor = Color.yellow;
                lineRenderer.positionCount = 0;
            }
        }

        private void Update()
        {
            // Center of screen raycast for real-time tape stretching
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            // 1. If currently measuring between Pin A and camera position
            if (isMeasuring && !isMeasurementLocked)
            {
                Vector3 currentTargetPos;
                if (RaycastFloor(screenCenter, out currentTargetPos))
                {
                    UpdateLiveMeasurement(pointAPosition, currentTargetPos);
                }
            }

            // 2. Handle Tap Input (Touch on Mobile OR Mouse Click in Unity XR Simulation)
            bool tapDetected = false;
            Vector2 tapPosition = screenCenter;

            #if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0))
            {
                tapDetected = true;
                tapPosition = Input.mousePosition;
            }
            #else
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                tapDetected = true;
                tapPosition = Input.GetTouch(0).position;
            }
            #endif

            if (tapDetected)
            {
                Vector3 hitPosition;
                if (RaycastFloor(tapPosition, out hitPosition))
                {
                    HandleMeasurementTap(hitPosition);
                }
            }
        }

        private bool RaycastFloor(Vector2 screenPoint, out Vector3 hitPoint)
        {
            hitPoint = Vector3.zero;
            if (raycastManager != null && raycastManager.Raycast(screenPoint, hits, TrackableType.PlaneWithinPolygon))
            {
                hitPoint = hits[0].pose.position;
                return true;
            }

            // Fallback for Editor simulation: raycast against physics colliders (floor)
            Ray ray = Camera.main.ScreenPointToRay(screenPoint);
            RaycastHit physicsHit;
            if (Physics.Raycast(ray, out physicsHit, 15f))
            {
                hitPoint = physicsHit.point;
                return true;
            }

            return false;
        }

        public void HandleMeasurementTap(Vector3 position)
        {
            if (!isMeasuring)
            {
                // Step 1: Place Pin A
                ResetMeasurement();
                pointAPosition = position;
                isMeasuring = true;
                isMeasurementLocked = false;

                if (pinPrefab != null)
                {
                    pinAInstance = Instantiate(pinPrefab, pointAPosition, Quaternion.identity);
                }

                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, pointAPosition);
                lineRenderer.SetPosition(1, pointAPosition);
                Debug.Log("[AR MEASURE] Pin A set at: " + pointAPosition);
            }
            else if (isMeasuring && !isMeasurementLocked)
            {
                // Step 2: Place Pin B and Lock Measurement
                pointBPosition = position;
                isMeasurementLocked = true;

                if (pinPrefab != null)
                {
                    pinBInstance = Instantiate(pinPrefab, pointBPosition, Quaternion.identity);
                }

                lockedWidthCm = currentDistanceCm;
                lockedDepthCm = Mathf.Round(lockedWidthCm * 0.65f); // Estimate ergonomic depth ratio

                lineRenderer.SetPosition(0, pointAPosition);
                lineRenderer.SetPosition(1, pointBPosition);

                Debug.Log($"[AR MEASURE] Locked! Width: {lockedWidthCm} cm, Depth: {lockedDepthCm} cm");
                OnMeasurementLocked?.Invoke(lockedWidthCm, lockedDepthCm);
            }
        }

        private void UpdateLiveMeasurement(Vector3 start, Vector3 current)
        {
            float distanceMeters = Vector3.Distance(start, current);
            currentDistanceCm = Mathf.Round(distanceMeters * 100f);

            if (lineRenderer != null && lineRenderer.positionCount == 2)
            {
                lineRenderer.SetPosition(0, start);
                lineRenderer.SetPosition(1, current);
            }
        }

        public void ResetMeasurement()
        {
            isMeasuring = false;
            isMeasurementLocked = false;
            currentDistanceCm = 0f;
            lockedWidthCm = 0f;
            lockedDepthCm = 0f;

            if (pinAInstance != null) Destroy(pinAInstance);
            if (pinBInstance != null) Destroy(pinBInstance);
            if (lineRenderer != null) lineRenderer.positionCount = 0;
        }

        public Vector3 GetMeasurementCenter()
        {
            if (isMeasurementLocked)
            {
                return (pointAPosition + pointBPosition) * 0.5f;
            }
            return pointAPosition;
        }
    }
}
