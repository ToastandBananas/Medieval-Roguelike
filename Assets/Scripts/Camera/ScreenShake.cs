using UnityEngine;
using Cinemachine;

namespace CameraSystem
{
    public class ScreenShake : MonoBehaviour
    {
        public static ScreenShake Instance { get; private set; }

        CinemachineImpulseSource cinemachineImpulseSource;

        bool canScreenShake = true;

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("There's more than one ScreenShake! " + transform + " - " + Instance);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            cinemachineImpulseSource = GetComponent<CinemachineImpulseSource>();
        }

        public void Shake(float intensity = 1f)
        {
            if (canScreenShake)
                cinemachineImpulseSource.GenerateImpulse(intensity);
        }

        public void SetCanScreenShake(bool canScreenShake) => this.canScreenShake = canScreenShake;
    }
}
