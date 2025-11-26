using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Añade este script a tus botones (GameObject que tiene Button).
/// Proporciona efecto de escala al hacer hover y reproduce SFX (usa MenuUIManager.Instance para los sonidos).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class AnimatedButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public float hoverScale = 1.08f;
    public float animSpeed = 10f;
    public bool pulse = true;
    public float pulseAmount = 1.03f;
    public float pulseSpeed = 2f;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private Coroutine pulseCoroutine;

    private void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * animSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScale;
        if (MenuUIManager.Instance != null)
            MenuUIManager.Instance.PlayHover();
        if (pulse && pulseCoroutine == null)
            pulseCoroutine = StartCoroutine(PulseRoutine());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Reproducir click SFX
        if (MenuUIManager.Instance != null)
            MenuUIManager.Instance.PlayClick();
    }

    private IEnumerator PulseRoutine()
    {
        while (true)
        {
            // aumentar
            float t = 0f;
            Vector3 start = originalScale * hoverScale;
            Vector3 end = start * pulseAmount;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * pulseSpeed;
                transform.localScale = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            // disminuir
            t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * pulseSpeed;
                transform.localScale = Vector3.Lerp(end, start, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
        }
    }
}