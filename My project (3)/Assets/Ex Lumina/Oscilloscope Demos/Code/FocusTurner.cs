namespace ExLumina.Assets.Oscilloscope
{
    using UnityEngine;

    public class FocusTurner : MonoBehaviour
    {
        public GameObject oscilloscope;
        public GameObject focusKnob;
        public bool turnRight;
        public AudioClip audioClip;

        const float turnAngle = 30;

        Oscilloscope scope;

        Transform knob;

        AudioSource audioSource;

        static Vector3 knobAngles;

        static float[] widths =
        {
        9, 5, 3, 1.5f, 3, 5, 9
    };

        static int focusIndex = 3;

        void Start()
        {
            scope = oscilloscope.GetComponent<Oscilloscope>();
            audioSource = gameObject.AddComponent<AudioSource>();

            knob = focusKnob.transform;
            knobAngles = knob.localEulerAngles;
        }

        private void OnMouseDown()
        {
            audioSource.PlayOneShot(audioClip);

            if (turnRight && focusIndex < widths.Length - 1)
            {
                knobAngles.x += 360 - turnAngle;
                focusIndex = focusIndex + 1;
            }
            else if (focusIndex > 0)
            {
                knobAngles.x += turnAngle;
                focusIndex = focusIndex - 1;
            }

            scope.fringeWidth = widths[focusIndex];
            scope.LoadNew();

            knobAngles.x %= 360;

            knob.localEulerAngles = knobAngles;
        }
    }
}