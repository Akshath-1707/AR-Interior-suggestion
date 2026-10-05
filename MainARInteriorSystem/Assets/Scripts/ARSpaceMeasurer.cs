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
    /// Supports both Center Reticle Pinning (iPhone style) and direct Floor Clicking.
    /// Features foolproof ground plane raycasting and zero-click protection.
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
            SetupLineRenderer();
        }

        private void FindCamera()
        {
            if (arCamera == null)
            {
                if (Camera.main != null) arCamera = Camera.main;
                else arCamera = FindObjectOfType<Camera>();
            }
        }

        private void SetupLineRenderer()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
                if (lineRenderer == null)
                {
                    lineRenderer = gameObject.AddComponent<LineRenderer>();
                }
            }

            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = 0.015f; // 1.5 cm thick tape
            lineRenderer.endWidth = 0.015f;
            lineRenderer.alignment = LineAlignment.View;
            lineRenderer.numCapVertices = 4;
            lineRenderer.numCornerVertices = 4;

            // Safe unlit shader
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            lineRenderer.material = new Material(shader);
            lineRenderer.startColor = Color.white;
            lineRenderer.endColor = new Color(1f, 0.85f, 0.2f); // Warm yellow
            lineRenderer.positionCount = 0;
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
                // In Editor, track mouse cursor; On mobile or when mouse idle, track screenCenter
                Vector2 aimScreenPos = screenCenter;
                #if ENABLE_INPUT_SYSTEM
                if (Mouse.current != null)
                {
                    Vector2 mousePos = Mouse.current.position.ReadValue();
                    // If mouse is within the Game view window
                    if (mousePos.x >= 0 && mousePos.x <= Screen.width && mousePos.y >= 0 && mousePos.y <= Screen.height)
                    {
                        aimScreenPos = mousePos;
                    }
                }
                #endif

                Vector3 currentTargetPos;
                if (RaycastFloor(aimScreenPos, out currentTargetPos))
                {
                    UpdateLiveMeasurement(pointAPosition, currentTargetPos);
                }
            }

            // 2. Handle Touch / Click Input using Unity 6 New Input System
            bool tapDetected = false;
            Vector2 tapPosition = screenCenter;

            #if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                tapPosition = Mouse.current.position.ReadValue();
                tapDetected = true;
            }
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                tapPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                tapDetected = true;
            }
            #else
            if (Input.GetMouseButtonDown(0))
            {
                tapPosition = Input.mousePosition;
                tapDetected = true;
            }
            else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                tapPosition = Input.GetTouch(0).position;
                tapDetected = true;
            }
            #endif

            if (tapDetected)
            {
                // Ignore taps on UI buttons
                if (ARUIManager.IsPositionOverUI(tapPosition))
                {
                    return;
                }

                Vector3 hitPosition;
                if (RaycastFloor(tapPosition, out hitPosition))
                {
                    HandleMeasurementTap(hitPosition);
                }
            }
        }

        /// <summary>
        /// Triple-layer Floor Raycast:
        /// 1. AR Foundation detected planes (ARCore / XR Simulation)
        /// 2. 3D Physics Mesh colliders
        /// 3. Infinite Ground Plane at Y = 0 (mathematical fallback so raycast NEVER fails)
        /// </summary>
        public bool RaycastFloor(Vector2 screenPoint, out Vector3 hitPoint)
        {
            hitPoint = Vector3.zero;

            // 1. AR Plane Raycast (ARCore / Simulation Planes)
            if (raycastManager != null && raycastManager.Raycast(screenPoint, hits, TrackableType.PlaneWithinPolygon | TrackableType.PlaneWithinBounds))
            {
                hitPoint = hits[0].pose.position;
                return true;
            }

            if (arCamera != null)
            {
                Ray ray = arCamera.ScreenPointToRay(screenPoint);

                // 2. Physics Raycast (XR Simulation Room Meshes)
                RaycastHit physicsHit;
                if (Physics.Raycast(ray, out physicsHit, 50f))
                {
                    hitPoint = physicsHit.point;
                    return true;
                }

                // 3. Fallback Ground Plane at Y = 0 (Guarantees raycast works in editor even with no colliders)
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                float enter;
                if (groundPlane.Raycast(ray, out enter) && enter > 0.01f && enter < 50f)
                {
                    hitPoint = ray.GetPoint(enter);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Drop a Pin at the current center screen reticle (iPhone Measure app style).
        /// </summary>
        public void DropPinAtReticle()
        {
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector3 hitPos;
            if (RaycastFloor(center, out hitPos))
            {
                HandleMeasurementTap(hitPos);
            }
        }

        public void HandleMeasurementTap(Vector3 position)
        {
            if (!isMeasuring && !isMeasurementLocked)
            {
                // Drop Pin A
                ResetMeasurement();
                pointAPosition = position;
                isMeasuring = true;
                isMeasurementLocked = false;

                pinAInstance = CreatePinInstance(pointAPosition, Color.white);

                if (lineRenderer != null)
                {
                    lineRenderer.positionCount = 5;
                    UpdateLiveRectangle(pointAPosition, pointAPosition);
                }
                Debug.Log($"[AR MEASURE] Corner 1 set at: {pointAPosition}");
            }
            else if (isMeasuring && !isMeasurementLocked)
            {
                // Drop Corner 2 and Lock 4-sided Rectangle
                float distM = Vector3.Distance(pointAPosition, position);
                if (distM < 0.10f)
                {
                    Debug.Log("[AR MEASURE] Ignored tap too close to Corner 1. Move diagonally further away!");
                    return;
                }

                pointBPosition = position;
                isMeasuring = false;
                isMeasurementLocked = true;

                pinBInstance = CreatePinInstance(pointBPosition, new Color(0.06f, 0.72f, 0.5f)); // Emerald Green
                UpdateLiveRectangle(pointAPosition, pointBPosition);

                if (lineRenderer != null)
                {
                    lineRenderer.startColor = new Color(0.06f, 0.72f, 0.5f);
                    lineRenderer.endColor = new Color(0.06f, 0.72f, 0.5f);
                }

                Debug.Log($"[AR MEASURE] 4-Line Space LOCKED: Width = {lockedWidthCm} cm, Depth = {lockedDepthCm} cm");
                OnMeasurementLocked?.Invoke(lockedWidthCm, lockedDepthCm);
            }
        }

        private void UpdateLiveMeasurement(Vector3 start, Vector3 current)
        {
            UpdateLiveRectangle(start, current);
        }

        private void UpdateLiveRectangle(Vector3 pA, Vector3 pB)
        {
            float y = Mathf.Min(pA.y, pB.y) + 0.01f;
            float minX = Mathf.Min(pA.x, pB.x);
            float maxX = Mathf.Max(pA.x, pB.x);
            float minZ = Mathf.Min(pA.z, pB.z);
            float maxZ = Mathf.Max(pA.z, pB.z);

            float wMeters = Mathf.Max(0.2f, maxX - minX);
            float dMeters = Mathf.Max(0.2f, maxZ - minZ);

            lockedWidthCm = Mathf.Round(wMeters * 100f);
            lockedDepthCm = Mathf.Round(dMeters * 100f);
            currentDistanceCm = lockedWidthCm;

            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 5;
                lineRenderer.SetPosition(0, new Vector3(minX, y, minZ));
                lineRenderer.SetPosition(1, new Vector3(maxX, y, minZ));
                lineRenderer.SetPosition(2, new Vector3(maxX, y, maxZ));
                lineRenderer.SetPosition(3, new Vector3(minX, y, maxZ));
                lineRenderer.SetPosition(4, new Vector3(minX, y, minZ)); // Closes 4-sided rectangle loop
            }
        }

        private GameObject CreatePinInstance(Vector3 pos, Color pinColor)
        {
            if (pinPrefab != null)
            {
                return Instantiate(pinPrefab, pos, Quaternion.identity);
            }

            // Clean fallback visual pin: glowing small sphere elevated 2cm
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "AR_MeasurePin";
            sphere.transform.position = pos + Vector3.up * 0.02f;
            sphere.transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);

            // Destroy collider so it doesn't block future raycasts
            Collider col = sphere.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer ren = sphere.GetComponent<Renderer>();
            if (ren != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
                Material mat = new Material(shader);
                mat.color = pinColor;
                ren.material = mat;
            }
            return sphere;
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

            Debug.Log("[AR MEASURE] Measurement reset. Ready for Pin A.");
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
