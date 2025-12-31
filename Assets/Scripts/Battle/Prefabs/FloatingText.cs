using UnityEngine;
using TMPro;
using System.Collections;

public class FloatingText : MonoBehaviour
{
    [SerializeField] private TextMeshPro textMesh;
    [SerializeField] private float floatSpeed = 1.5f;
    [SerializeField] private float fadeDuration = 1.0f;
    
    private Color originalColor;
    private float timer;

    public void Setup(string content, Color color, float size)
    {
        if (textMesh == null) textMesh = GetComponent<TextMeshPro>();
        
        textMesh.text = content;
        textMesh.color = color;
        textMesh.fontSize = size;
        originalColor = color;
        timer = 0;
        // transform.rotation = Camera.main.transform.rotation; 
    }

    void Update()
    {
        timer += Time.deltaTime;
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;
        if (timer > fadeDuration * 0.5f)
        {
            float alpha = 1.0f - ((timer - fadeDuration * 0.5f) / (fadeDuration * 0.5f));
            Color c = originalColor;
            c.a = alpha;
            textMesh.color = c;
        }
        if (timer >= fadeDuration)
        {
            gameObject.SetActive(false);
        }
    }
}