namespace ExLumina.Assets.Oscilloscope
{
    using System.Collections;
    using UnityEngine;

    public class Flyer : MonoBehaviour
    {
        public Vector3 toPosition;
        public Vector3 toRotation;
        public float flightTime;
        public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public GameObject turnOn;
        public GameObject turnOff;

        Transform cam;

        Vector3 fromPosition;
        Quaternion fromRotationQ;

        Quaternion toRotationQ;

        static bool isFlying = false;

        private void Start()
        {
            cam = transform.parent.transform;
            toRotationQ = Quaternion.identity;

            toRotationQ.eulerAngles = toRotation;
        }

        private void OnMouseDown()
        {
            if (isFlying) return;

            isFlying = true;

            fromPosition = cam.position;
            fromRotationQ = cam.rotation;

            StartCoroutine(Fly());
        }

        IEnumerator Fly()
        {
            turnOff.SetActive(false);

            float t0 = Time.time;
            float dt = 0;

            while (dt < flightTime)
            {
                dt = Time.time - t0;

                if (dt > flightTime)
                {
                    dt = flightTime;
                }

                float t = curve.Evaluate(dt / flightTime);

                Vector3 position = t * toPosition + (1 - t) * fromPosition;
                Quaternion rotation = Quaternion.Lerp(fromRotationQ, toRotationQ, t);

                cam.position = position;
                cam.rotation = rotation;

                yield return null;
            }

            turnOn.SetActive(true);
            isFlying = false;
        }
    }
}