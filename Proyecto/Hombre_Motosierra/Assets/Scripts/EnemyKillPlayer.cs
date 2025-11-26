using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class EnemyKillPlayer : MonoBehaviour
{
    private bool processingCollision = false;

    private void OnTriggerEnter(Collider other)
    {
        if (processingCollision) return;

        // Solo actuar si ESTE OBJETO es un enemigo
        if (!CompareTag("Enemy")) return;

        if (other.CompareTag("Player"))
        {
            processingCollision = true;

            Debug.Log("Jugador colisionado por enemigo.");

            bool gameOver = false;

            if (GameManager.instancia != null)
            {
                // Dejar que GameManager gestione vidas y final de partida.
                gameOver = GameManager.instancia.LoseLife();
            }
            else
            {
                Debug.LogWarning("[EnemyKillPlayer] No hay instancia de GameManager.");
                // fallback: finalizar partida
                gameOver = true;
            }

            if (gameOver)
            {
                // Si se acabaron las vidas, mostrar mensaje de pérdida
                StartCoroutine(ShowLoseMessage());
            }
            else
            {
                // Si todavía hay vidas, evitar colisiones repetidas temporariamente
                StartCoroutine(ResetCollisionAfterDelay(1.0f)); // 1 segundo de "inmunidad"
            }
        }
    }

    private IEnumerator ResetCollisionAfterDelay(float seconds)
    {
        // Espera en tiempo real (no afectado por timeScale)
        float timer = 0f;
        while (timer < seconds)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }
        processingCollision = false;
    }

    private IEnumerator ShowLoseMessage()
    {
        // Mantenemos mensajito simple; la pausa ya la puso GameManager.EndGame()
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

        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var rect = text.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(800, 200);
        rect.anchoredPosition = Vector2.zero;

        yield return null;
    }
}