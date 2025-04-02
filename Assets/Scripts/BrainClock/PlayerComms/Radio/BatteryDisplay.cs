using System.Collections;
using System.Collections.Generic;
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

        public void SetBatteryStatus(float batteryCharge)
        {
            Battery20.SetActive(batteryCharge > 0.0f);
            Battery40.SetActive(batteryCharge > 0.2f);
            Battery60.SetActive(batteryCharge > 0.4f);
            Battery80.SetActive(batteryCharge > 0.6f);
            Battery100.SetActive(batteryCharge > 0.8f);
        }

    }
}
