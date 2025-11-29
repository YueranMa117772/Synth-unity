namespace ExLumina.Assets.Oscilloscope
{
    using UnityEngine;

    public class TimeTurner : MonoBehaviour
    {
        public GameObject oscilloscope;
        public GameObject timeKnob;
        public bool turnRight;
        public AudioClip audioClip;

        Oscilloscope scope;

        Transform knob;

        AudioSource audioSource;

        static Vector3 knobAngles;

        static float[] times =
        {
        5, 2, 1, .5f, .2f, .1f, .05f, .02f, .01f, .005f, .002f, .001f, .0005f, .0002f, .0001f
    };

        static int timeIndex = 5;

        void Start()
        {
            scope = oscilloscope.GetComponent<Oscilloscope>();
            audioSource = gameObject.AddComponent<AudioSource>();

            knob = timeKnob.transform;
            knobAngles = knob.localEulerAngles;
        }

        private void OnMouseDown()
        {
            audioSource.PlayOneShot(audioClip);

            if (turnRight)
            {
                knobAngles.z += 336;

                timeIndex = (timeIndex + 1) % times.Length;
            }
            else
            {
                knobAngles.z += 24;

                timeIndex = (timeIndex + times.Length - 1) % times.Length;
            }

            scope.SecondsPerDiv = times[timeIndex];
            scope.LoadNew();

            knobAngles.z %= 360;

            knob.localEulerAngles = knobAngles;
        }
    }
}