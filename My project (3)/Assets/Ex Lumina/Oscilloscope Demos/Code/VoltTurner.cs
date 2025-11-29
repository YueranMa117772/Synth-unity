namespace ExLumina.Assets.Oscilloscope
{
    using UnityEngine;

    public class VoltTurner : MonoBehaviour
    {
        public GameObject oscilloscope;
        public GameObject voltKnob;
        public bool turnRight;
        public AudioClip audioClip;

        const float turnAngle = 30;

        Oscilloscope scope;

        Transform knob;

        AudioSource audioSource;

        static Vector3 knobAngles;

        static float[] volts =
        {
        5, 2, 1, .5f, .2f, .1f, .05f, .02f, .01f, .005f, .002f, .001f
    };

        static int voltIndex = 3;

        void Start()
        {
            scope = oscilloscope.GetComponent<Oscilloscope>();
            audioSource = gameObject.AddComponent<AudioSource>();

            knob = voltKnob.transform;
            knobAngles = knob.localEulerAngles;
        }

        private void OnMouseDown()
        {
            audioSource.PlayOneShot(audioClip);

            if (turnRight)
            {
                knobAngles.z += 360 - turnAngle;

                voltIndex = (voltIndex + 1) % volts.Length;
            }
            else
            {
                knobAngles.z += turnAngle;

                voltIndex = (voltIndex + volts.Length - 1) % volts.Length;
            }

            scope.voltsPerDiv = volts[voltIndex];
            scope.LoadNew();

            knobAngles.z %= 360;

            knob.localEulerAngles = knobAngles;
        }
    }
}