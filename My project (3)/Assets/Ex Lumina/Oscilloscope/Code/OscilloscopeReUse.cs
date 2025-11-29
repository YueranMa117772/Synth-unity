namespace ExLumina.Assets.Oscilloscope
{
    using UnityEngine;

    public class OscilloscopeReUse : MonoBehaviour
    {
        public GameObject oscilloscope;

        private void OnMouseUpAsButton()
        {
            oscilloscope.GetComponent<Oscilloscope>().On(true);
        }
    }
}