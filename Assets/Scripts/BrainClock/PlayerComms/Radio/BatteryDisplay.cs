using UnityEngine;
namespace BrainClock.PlayerComms
{
    public class BatteryDisplay : MonoBehaviour
    {
        public GameObject Battery20;
        public GameObject Battery40;
        public GameObject Battery60;
        public GameObject Battery80;
        public GameObject Battery100;

        public void SetBatteryStatus(float PowerRatio)
        {
            Battery20.SetActive(PowerRatio > 0.2f);
            Battery40.SetActive(PowerRatio > 0.4f);
            Battery60.SetActive(PowerRatio > 0.6f);
            Battery80.SetActive(PowerRatio > 0.8f);
            Battery100.SetActive(PowerRatio > 0.9f);
        }

    }
}
