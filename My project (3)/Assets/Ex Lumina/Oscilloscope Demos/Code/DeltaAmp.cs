namespace ExLumina.Assets.Oscilloscope
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.UI;

    public class DeltaAmp : MonoBehaviour
    {
        public GameObject scope;
        public GameObject text;
        public bool lowering;
        public AudioClip beepUp;
        public AudioClip beepDown;

        Oscilloscope oscilloscope;
        Text ampText;

        static int ampTimesHundred = 100;
        int dAmp0 = 1;
        AudioSource audioSource;
        Vector3 pushPos;
        Vector3 popPos;

        const int div = 15;
        const int maxAmp = 2000;
        const int minAmp = 1;

        Coroutine changing = null;

        void Start()
        {
            oscilloscope = scope.GetComponent<Oscilloscope>();
            audioSource = gameObject.AddComponent<AudioSource>();
            ampText = text.GetComponent<Text>();

            popPos = transform.position;
            pushPos = popPos;
            pushPos.z += .005f;

            if (lowering)
            {
                dAmp0 *= -1;
            }
        }

        private void OnMouseDown()
        {
            audioSource.PlayOneShot(beepUp);
            transform.position = pushPos;
            if (changing != null)
            {
                StopCoroutine(changing);
            }
            changing = StartCoroutine(Changing());
        }

        private void OnMouseUp()
        {
            if (changing != null)
            {
                transform.position = popPos;
                audioSource.PlayOneShot(beepDown);
                StopCoroutine(changing);
                changing = null;
            }
        }

        private void OnMouseExit()
        {
            OnMouseUp();
        }

        IEnumerator Changing()
        {
            int dAmp = dAmp0;

            int downCount = 0;
            int chgCount = 0;

            while (true)
            {
                while (downCount % div != 0)
                {
                    yield return null;
                    downCount = downCount + 1;
                }

                ampTimesHundred += dAmp;

                ampTimesHundred =
                    ampTimesHundred > maxAmp ?
                        maxAmp : ampTimesHundred < minAmp ?
                            minAmp : ampTimesHundred;

                float amp = ampTimesHundred / 100f;

                oscilloscope.generatorAmplitude = amp;
                oscilloscope.LoadNew();

                ampText.text = string.Format("{0,6:F2}", amp);

                downCount = downCount + 1;
                chgCount = chgCount + 1;

                if (chgCount % 10 == 0)
                {
                    dAmp *= 10;
                    chgCount = 0;
                }
            }
        }
    }
}