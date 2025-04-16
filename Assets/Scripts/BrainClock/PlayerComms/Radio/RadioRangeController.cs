using Assets.Scripts.Objects;
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

    public enum RangeMode
    {
        Radio = 0,
        Tower = 1
    }

    public class RadioRangeController : MonoBehaviour
    {

        [Tooltip("Owner radio of this range controller")]
        public Assets.Scripts.Objects.Thing ParentThing;
        public RangeMode AntennaRangeMode = RangeMode.Radio;
        public bool Ready = false;
        float _range = 0f;

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
            get { return _range; }
            set {
                _range = value;
                CalculateIntruders();
            }
        }

        /// <summary>
        /// Recalculate the radios within the range area
        /// </summary>
        public void CalculateIntruders()
        {
            if (!Ready)
                return;

            foreach (Radio radio in Radio.AllRadios)
            {
                if (radio.GetAsThing == ParentThing)
                    continue;

                float sqrDistance = (transform.position - radio.transform.position).sqrMagnitude;
                if (sqrDistance < _range * _range)
                {
                    if (!_radios.Contains(radio))
                    {
                        _radios.Add(radio);
                        if (AntennaRangeMode == RangeMode.Tower)
                            radio.OnTowerInRadius(ParentThing as Tower);
                    }
                }
                else
                {
                    if (_radios.Contains(radio))
                    {
                        _radios.Remove(radio);
                        if (AntennaRangeMode == RangeMode.Tower)
                            radio.OnTowerOutRadius(ParentThing as Tower);
                    }
                }
            }
        }

        private List<Radio> _radios;

        // Start is called before the first frame update
        void Start()
        {
            _radios = new List<Radio>();
            Ready = true;
            CalculateIntruders();
        }

    }
}
