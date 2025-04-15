using Genetics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BrainClock.PlayerComms
{
    /// <summary>
    /// Sets a sphere collider of a desired range radius and tracks all
    /// Radio elements inside this zone.
    /// </summary>

    [RequireComponent(typeof(SphereCollider))]
    public class RadioRangeController : MonoBehaviour
    {

        /// <summary>
        /// Returns a list of radios inside the range zone.
        /// </summary>
        public List<Radio> RadiosInRange
        {
            get { return _radios; } 
        }

        /// <summary>
        /// Set/Get the current radio range.
        /// </summary>
        public float Range
        {
            get { return rangeZone != null ? rangeZone.radius : 0f; }
            set {
                if (rangeZone != null)
                    rangeZone.radius = value;
                else
                    Debug.LogWarning("Trying to set Range, but rangeZone is null."); 
            }
        }

        [SerializeField]
        private SphereCollider rangeZone;

        private List<Radio> _radios;

        // Start is called before the first frame update
        void Awake()
        {
            _radios = new List<Radio>();
            if (rangeZone == null)
                rangeZone = this.gameObject.GetComponent<SphereCollider>();
            rangeZone.isTrigger = true;
        }

        private void Start()
        {
            //if (rangeZone == null)
            //    rangeZone = this.gameObject.GetComponent<SphereCollider>();
            //rangeZone.isTrigger = true;
        }


        private void OnTriggerEnter(Collider other)
        {
            Radio radio = other.gameObject.GetComponent<Radio>();
            if (radio != null)
            {
                Debug.Log($"{this.name} + Adding radio {radio.name}, in range");
                _radios.Add(radio);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            Radio radio = other.gameObject.GetComponent<Radio>();
            if (radio != null)
            {
                Debug.Log($"{this.name} - Removing radio {radio.name}, not in range");
                _radios.Remove(radio);
            }
        }


    }
}
