using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

namespace Hex
{
    [RequireComponent(typeof(DirectionalLight))]
    public class DayNightCycle : MonoBehaviour
    {
        [SerializeField] private float _degreesPerSecond = 1f;

        private void FixedUpdate()
        {
            transform.Rotate(Vector3.right, _degreesPerSecond * Time.deltaTime);
            Vector3 eulerAngles = transform.rotation.eulerAngles;
            if (eulerAngles.x is > 180f or < 0f)
            {
                eulerAngles.x = 180f;
                transform.rotation = Quaternion.Euler(eulerAngles);
            }
        }
    }
}
