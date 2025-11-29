namespace ExLumina.Assets.Oscilloscope
{
    using UnityEngine;

    public class LockOnOff : MonoBehaviour
    {
        public GameObject oscilloscope;
        public AudioClip audioClip;
        public float distance;

        Oscilloscope scope;

        Vector3 offPosition;
        Vector3 onPosition;

        AudioSource audioSource;

        void Start()
        {
            scope = oscilloscope.GetComponent<Oscilloscope>();
            audioSource = gameObject.AddComponent<AudioSource>();

            offPosition = transform.localPosition;
            onPosition = offPosition;
            onPosition.z += distance;
        }

        private void OnMouseDown()
        {
            audioSource.PlayOneShot(audioClip);

            if (scope.alwaysPhaseLock)
            {
                transform.localPosition = offPosition;
            }
            else
            {
                transform.localPosition = onPosition;
            }

            scope.alwaysPhaseLock = !scope.alwaysPhaseLock;
            scope.LoadNew();
        }
    }
}