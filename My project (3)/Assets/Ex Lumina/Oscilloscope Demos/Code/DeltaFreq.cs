namespace ExLumina.Assets.Oscilloscope
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.UI;

    public class DeltaFreq : MonoBehaviour
    {
        public GameObject scope;
        public GameObject text;
        public bool lowering;
        public AudioClip beepUp;
        public AudioClip beepDown;

        Oscilloscope oscilloscope;
        Text freqText;
        static int freqTimesTen = 100;
        int dFreq0 = 1;
        AudioSource audioSource;
        Vector3 pushPos;
        Vector3 popPos;

        const int div = 15;
        const int maxFreq = 20000;
        const int minFreq = 1;

        Coroutine changing = null;

        void Start()
        {
            oscilloscope = scope.GetComponent<Oscilloscope>();
            audioSource = gameObject.AddComponent<AudioSource>();
            freqText = text.GetComponent<Text>();

            popPos = transform.position;
            pushPos = popPos;
            pushPos.z += .005f;

            if (lowering)
            {
                dFreq0 *= -1;
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
            int dFreq = dFreq0;

            int downCount = 0;
            int chgCount = 0;

            while (true)
            {
                while (downCount % div != 0)
                {
                    yield return null;
                    downCount = downCount + 1;
                }

                freqTimesTen += dFreq;

                freqTimesTen =
                    freqTimesTen > maxFreq ?
                        maxFreq : freqTimesTen < minFreq ?
                            minFreq : freqTimesTen;

                float freq = freqTimesTen / 10f;

                oscilloscope.generatorFrequency = freq;
                oscilloscope.LoadNew();

                freqText.text = string.Format("{0,6:F1}", freq);

                downCount = downCount + 1;
                chgCount = chgCount + 1;

                if (chgCount % 10 == 0)
                {
                    dFreq *= 10;
                    chgCount = 0;
                }
            }
        }
    }
}