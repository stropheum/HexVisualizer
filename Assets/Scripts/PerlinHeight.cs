using UnityEngine;

public class PerlinHeight : MonoBehaviour
{
    [field: SerializeField] public float PerlinZoom { get; set; } = 1f;
    [field: SerializeField] public float PerlinTimeScale { get; set; } = 1f;
    
    private void FixedUpdate()
    {
        float delta = Mathf.PerlinNoise(
            transform.position.x * PerlinZoom + Time.time * PerlinTimeScale, 
            transform.position.z * PerlinZoom + Time.time * PerlinTimeScale);
        transform.localPosition = new Vector3(transform.position.x, delta, transform.position.z);
    }
}
