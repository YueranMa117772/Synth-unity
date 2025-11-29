namespace ExLumina.Assets.Oscilloscope
{
    using UnityEngine;

    public class PowerOnOff : MonoBehaviour
    {
        public GameObject oscilloscope;
        public AudioClip audioClip;
        public float distance;

        Oscilloscope scope;

        Vector3 offPosition;
        Vector3 onPosition;

        AudioSource audioSource;

        bool isOn;

        void Start()
        {
            scope = oscilloscope.GetComponent<Oscilloscope>();
            audioSource = gameObject.AddComponent<AudioSource>();

            offPosition = transform.localPosition;
            onPosition = offPosition;
            onPosition.z += distance;

            if (scope.isOnAtStart)
            {
                isOn = true;
                transform.localPosition = onPosition;
            }
        }

        private void OnMouseDown()
        {
            audioSource.PlayOneShot(audioClip);

            if (isOn)
            {
                transform.localPosition = offPosition;
                scope.Off();
                isOn = false;
            }
            else
            {
                transform.localPosition = onPosition;
                scope.On();
                isOn = true;
            }
        }
    }
}
