using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace ARInterior
{
    [Serializable]
    public class RecommendationRequest
    {
        public float available_width_cm;
        public float available_depth_cm;
        public string category;
        public string preferred_style;
        public string preferred_color;
        public int top_n = 3;
    }

    [Serializable]
    public class DimensionsData
    {
        public float width_cm;
        public float depth_cm;
        public float height_cm;
    }

    [Serializable]
    public class RecommendationItem
    {
        public int id;
        public string name;
        public string category;
        public string style;
        public string color;
        public DimensionsData dimensions;
        public int match_score;
        public string fit_status;
        public string explanation;
        public string model_filename;
    }

    [Serializable]
    public class RecommendationResponse
    {
        public string status;
        public int count;
        public List<RecommendationItem> recommendations;
    }

    public class RecommendationApiClient : MonoBehaviour
    {
        [Header("Server Configuration")]
        [SerializeField] private string serverUrl = "http://localhost:8000/api/v1/recommend";

        public static RecommendationApiClient Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void GetRecommendations(float widthCm, float depthCm, string category, string style, Action<List<RecommendationItem>> onSuccess, Action<string> onError)
        {
            StartCoroutine(SendRecommendationRequest(widthCm, depthCm, category, style, onSuccess, onError));
        }

        private IEnumerator SendRecommendationRequest(float widthCm, float depthCm, string category, string style, Action<List<RecommendationItem>> onSuccess, Action<string> onError)
        {
            RecommendationRequest reqData = new RecommendationRequest
            {
                available_width_cm = widthCm,
                available_depth_cm = depthCm,
                category = category,
                preferred_style = style,
                top_n = 3
            };

            string jsonPayload = JsonUtility.ToJson(reqData);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);

            using (UnityWebRequest www = new UnityWebRequest(serverUrl, "POST"))
            {
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");

                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = www.downloadHandler.text;
                    RecommendationResponse response = JsonUtility.FromJson<RecommendationResponse>(jsonResponse);
                    onSuccess?.Invoke(response.recommendations);
                }
                else
                {
                    onError?.Invoke(www.error + ": " + www.downloadHandler.text);
                }
            }
        }
    }
}
