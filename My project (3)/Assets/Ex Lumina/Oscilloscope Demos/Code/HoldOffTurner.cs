namespace ExLumina.Assets.Oscilloscope
{
    using UnityEngine;

    public class HoldOffTurner : MonoBehaviour
    {
        public GameObject oscilloscope;
        public GameObject holdOffKnob;
        public bool turnRight;
        public AudioClip audioClip;

        const float turnAngle = 30;

        Oscilloscope scope;

        Transform knob;

        AudioSource audioSource;

        static Vector3 knobAngles;

        static float[] angles =
        {
        -.75f, -.5f, -.25f, 0, .25f, .5f, .75f
    };

        static int index = 3;

        void Start()
        {
            scope = oscilloscope.GetComponent<Oscilloscope>();
            audioSource = gameObject.AddComponent<AudioSource>();

            knob = holdOffKnob.transform;
            knobAngles = knob.localEulerAngles;
        }

        private void OnMouseDown()
        {
            audioSource.PlayOneShot(audioClip);

            if (turnRight && index < angles.Length - 1)
            {
                knobAngles.x += 360 - turnAngle;
                index = index + 1;
            }
            else if (index > 0)
            {
                knobAngles.x += turnAngle;
                index = index - 1;
            }

            scope.phaseAngle = angles[index];
            scope.LoadNew();

            knobAngles.x %= 360;

            knob.localEulerAngles = knobAngles;
        }
    }
}