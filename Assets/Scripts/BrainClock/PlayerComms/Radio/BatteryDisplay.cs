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

        public void SetBatteryStatus(float CurrentPowerPercentage)
        {
            Battery20.SetActive(CurrentPowerPercentage > 0.2f);
            Battery40.SetActive(CurrentPowerPercentage > 0.4f);
            Battery60.SetActive(CurrentPowerPercentage > 0.6f);
            Battery80.SetActive(CurrentPowerPercentage > 0.8f);
            Battery100.SetActive(CurrentPowerPercentage > 0.9f);
        }

    }
}
