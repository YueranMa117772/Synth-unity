namespace ExLumina.Assets.Oscilloscope
{
    using UnityEngine;

    public class WaveForm : MonoBehaviour
    {
        public GameObject scope;
        public GameObject pop1;
        public GameObject pop2;
        public bool isPushed;
        public FunctionGenerator generator;
        public AudioClip chunk;

        Vector3 popPos;
        Vector3 pushPos;
        Oscilloscope oscilloscope;
        WaveForm wave1;
        WaveForm wave2;
        AudioSource audioSource;

        void Start()
        {
            oscilloscope = scope.GetComponent<Oscilloscope>();
            audioSource = gameObject.AddComponent<AudioSource>();

            popPos = transform.position;
            pushPos = popPos;
            pushPos.z += .005f;

            wave1 = pop1.GetComponent<WaveForm>();
            wave2 = pop2.GetComponent<WaveForm>();

            if (isPushed)
            {
                transform.position = pushPos;
            }
        }

        private void OnMouseDown()
        {
            audioSource.PlayOneShot(chunk);
            Push();
        }

        void Push()
        {
            oscilloscope.generator = generator;
            oscilloscope.LoadNew();
            transform.position = pushPos;
            wave1.Pop();
            wave2.Pop();
        }

        public void Pop()
        {
            transform.position = popPos;
        }
    }
}