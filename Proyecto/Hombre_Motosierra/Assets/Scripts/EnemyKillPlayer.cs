using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections; // 🔥 ESTA ES LA LÍNEA QUE FALTABA

public class EnemyKillPlayer : MonoBehaviour
{
    private bool gameOver = false;

private void OnTriggerEnter(Collider other)
{
    if (gameOver) return;

    // Solo matar si ESTE OBJETO es un enemigo
    if (!CompareTag("Enemy")) return;

    if (other.CompareTag("Player"))
    {
        gameOver = true;

        Debug.Log("HAS PERDIDO");

        Time.timeScale = 0f;

        StartCoroutine(ShowLoseMessage());
    }
}

    private IEnumerator ShowLoseMessage()
    {
        GameObject canvasObj = new GameObject("LoseCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        GameObject textObj = new GameObject("LoseText");
        textObj.transform.parent = canvasObj.transform;

        var text = textObj.AddComponent<Text>();
        text.text = "HAS PERDIDO";
        text.fontSize = 75;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.red;

        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        var rect = text.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(800, 200);
        rect.anchoredPosition = Vector2.zero;

        yield return null;
    }
}
