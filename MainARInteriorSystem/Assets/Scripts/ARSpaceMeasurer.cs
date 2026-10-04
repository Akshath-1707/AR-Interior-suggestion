using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ARInterior
{
    /// <summary>
    /// ARSpaceMeasurer: Apple-style AR Tape Measure.
    /// Compatible with Unity 6's New Input System.
    /// </summary>
    public class ARSpaceMeasurer : MonoBehaviour
    {
        [Header("AR Foundation Managers")]
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private GameObject pinPrefab;
        [SerializeField] private Camera arCamera;

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

        private void Awake()
        {
            FindCamera();
        }

        private void Start()
        {
            if (raycastManager == null)
            {
                raycastManager = GetComponent<ARRaycastManager>();
            }

            FindCamera();

            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
                if (lineRenderer == null)
                {
                    lineRenderer = gameObject.AddComponent<LineRenderer>();
                }
                lineRenderer.startWidth = 0.02f;
                lineRenderer.endWidth = 0.02f;
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                lineRenderer.startColor = Color.white;
                lineRenderer.endColor = Color.yellow;
                lineRenderer.positionCount = 0;
            }
        }

        private void FindCamera()
        {
            if (arCamera == null)
            {
                if (Camera.main != null) arCamera = Camera.main;
                else arCamera = FindObjectOfType<Camera>();
            }
        }

        private void Update()
        {
            if (arCamera == null)
            {
                FindCamera();
                if (arCamera == null) return;
            }

            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            // 1. Live stretching while measuring
            if (isMeasuring && !isMeasurementLocked)
            {
                Vector3 currentTargetPos;
                if (RaycastFloor(screenCenter, out currentTargetPos))
                {
                    UpdateLiveMeasurement(pointAPosition, currentTargetPos);
                }
            }

            // 2. Handle Touch / Click Input using Unity 6 New Input System
            bool tapDetected = false;
            Vector2 tapPosition = screenCenter;

            #if ENABLE_INPUT_SYSTEM
            // Unity 6 New Input System (Mouse for Editor, Touchscreen for Mobile)
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                tapDetected = true;
                tapPosition = Mouse.current.position.ReadValue();
            }
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                tapDetected = true;
                tapPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            }
            #else
            // Legacy Input System Fallback
            if (Input.GetMouseButtonDown(0))
            {
                tapDetected = true;
                tapPosition = Input.mousePosition;
            }
            else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
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

            // 1. ARCore Plane Raycast
            if (raycastManager != null && raycastManager.Raycast(screenPoint, hits, TrackableType.PlaneWithinPolygon))
            {
                hitPoint = hits[0].pose.position;
                return true;
            }

            // 2. Safe Physics Raycast for Editor XR Simulation
            if (arCamera != null)
            {
                Ray ray = arCamera.ScreenPointToRay(screenPoint);
                RaycastHit physicsHit;
                if (Physics.Raycast(ray, out physicsHit, 20f))
                {
                    hitPoint = physicsHit.point;
                    return true;
                }
            }

            return false;
        }

        public void HandleMeasurementTap(Vector3 position)
        {
            if (!isMeasuring)
            {
                ResetMeasurement();
                pointAPosition = position;
                isMeasuring = true;
                isMeasurementLocked = false;

                if (pinPrefab != null)
                {
                    pinAInstance = Instantiate(pinPrefab, pointAPosition, Quaternion.identity);
                }

                if (lineRenderer != null)
                {
                    lineRenderer.positionCount = 2;
                    lineRenderer.SetPosition(0, pointAPosition);
                    lineRenderer.SetPosition(1, pointAPosition);
                }
                Debug.Log("[AR MEASURE] Pin A set at: " + pointAPosition);
            }
            else if (isMeasuring && !isMeasurementLocked)
            {
                pointBPosition = position;
                isMeasurementLocked = true;

                if (pinPrefab != null)
                {
                    pinBInstance = Instantiate(pinPrefab, pointBPosition, Quaternion.identity);
                }

                lockedWidthCm = currentDistanceCm;
                lockedDepthCm = Mathf.Round(lockedWidthCm * 0.65f);

                if (lineRenderer != null)
                {
                    lineRenderer.SetPosition(0, pointAPosition);
                    lineRenderer.SetPosition(1, pointBPosition);
                }

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
