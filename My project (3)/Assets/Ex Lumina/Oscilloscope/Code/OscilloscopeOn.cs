namespace ExLumina.Assets.Oscilloscope
{
    using UnityEngine;

    public class OscilloscopeOn : MonoBehaviour
    {
        public GameObject oscilloscope;

        private void OnMouseUpAsButton()
        {
            oscilloscope.GetComponent<Oscilloscope>().On(false);
        }
    }
}