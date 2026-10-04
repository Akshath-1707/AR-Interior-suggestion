using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARInterior
{
    public class ARSpaceMeasurer : MonoBehaviour
    {
        [Header("AR Foundation References")]
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private GameObject pinMarkerPrefab;
        [SerializeField] private LineRenderer lineRenderer;

        [Header("Measurement Output")]
        public float MeasuredWidthCm { get; private set; }
        public float MeasuredDepthCm { get; private set; }

        private List<GameObject> activePins = new List<GameObject>();
        private List<Vector3> pinPositions = new List<Vector3>();
        private static List<ARRaycastHit> hits = new List<ARRaycastHit>();

        public event Action<float, float> OnMeasurementCompleted;

        private void Update()
        {
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                Touch touch = Input.GetTouch(0);
                if (raycastManager != null && raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
                {
                    Pose hitPose = hits[0].pose;
                    AddPinMarker(hitPose.position);
                }
            }
        }

        public void AddPinMarker(Vector3 position)
        {
            if (activePins.Count >= 2)
            {
                ClearMeasurement();
            }

            if (pinMarkerPrefab != null)
            {
                GameObject pin = Instantiate(pinMarkerPrefab, position, Quaternion.identity);
                activePins.Add(pin);
            }
            pinPositions.Add(position);

            if (pinPositions.Count == 2)
            {
                CalculateSpaceDimensions();
            }
        }

        private void CalculateSpaceDimensions()
        {
            float distanceMeters = Vector3.Distance(pinPositions[0], pinPositions[1]);
            MeasuredWidthCm = distanceMeters * 100f;
            MeasuredDepthCm = MeasuredWidthCm * 0.6f;

            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, pinPositions[0]);
                lineRenderer.SetPosition(1, pinPositions[1]);
            }

            Debug.Log($"[AR MEASUREMENT] Measured Width: {MeasuredWidthCm:F1} cm, Depth: {MeasuredDepthCm:F1} cm");
            OnMeasurementCompleted?.Invoke(MeasuredWidthCm, MeasuredDepthCm);
        }

        public void ClearMeasurement()
        {
            foreach (var pin in activePins)
            {
                if (pin != null) Destroy(pin);
            }
            activePins.Clear();
            pinPositions.Clear();
            if (lineRenderer != null) lineRenderer.positionCount = 0;
        }
    }
}
