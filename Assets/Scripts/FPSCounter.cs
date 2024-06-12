using UnityEngine;
using System.Collections;
using TMPro;

public class FPSCounter : MonoBehaviour {
    public TextMeshProUGUI fpsText; // Assign a UI Text element in the inspector
    public float updateInterval = 2.0f; // Time interval in seconds to update the FPS

    private int frames = 0;
    private float timePassed = 0.0f;

    void Start() {
        StartCoroutine(UpdateFPS());
    }

    void Update() {
        frames++;
        timePassed += Time.deltaTime;
    }

    private IEnumerator UpdateFPS() {
        while (true) {
            yield return new WaitForSeconds(updateInterval);
            float fps = frames / timePassed;
            fpsText.text = $"FPS: {fps:F2}";

            // Reset the counters
            frames = 0;
            timePassed = 0.0f;
        }
    }
}
