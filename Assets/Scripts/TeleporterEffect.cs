using UnityEngine;
using System.Collections;

public class TeleporterEffect : MonoBehaviour
{
    [Header("Effect Settings")]
    public float effectDuration = 1.0f;
    public float rotationSpeed = 360f;
    public float scaleSpeed = 2f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    
    [Header("Colors")]
    public Color startColor = Color.cyan;
    public Color endColor = Color.blue;
    
    private SpriteRenderer spriteRenderer;
    private Transform effectTransform;
    private float startTime;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        effectTransform = transform;
        startTime = Time.time;
        
        // Auto-destroy after effect duration
        Destroy(gameObject, effectDuration);
        
        // Start the effect coroutine
        StartCoroutine(PlayEffect());
    }
    
    private IEnumerator PlayEffect()
    {
        Vector3 originalScale = effectTransform.localScale;
        
        while (Time.time - startTime < effectDuration)
        {
            float progress = (Time.time - startTime) / effectDuration;
            
            // Rotation
            effectTransform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
            
            // Scale animation
            float scaleMultiplier = scaleCurve.Evaluate(progress);
            effectTransform.localScale = originalScale * scaleMultiplier;
            
            // Color and alpha animation
            if (spriteRenderer != null)
            {
                Color currentColor = Color.Lerp(startColor, endColor, progress);
                currentColor.a = alphaCurve.Evaluate(progress);
                spriteRenderer.color = currentColor;
            }
            
            yield return null;
        }
    }
}