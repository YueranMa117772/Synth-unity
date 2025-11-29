namespace ExLumina.Assets.Oscilloscope
{
    using UnityEngine;

    public class OscilloscopeOff : MonoBehaviour
    {
        public GameObject oscilloscope;

        private void OnMouseUpAsButton()
        {
            oscilloscope.GetComponent<Oscilloscope>().Off();
        }
    }
}