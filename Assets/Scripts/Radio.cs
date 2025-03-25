using Assets.Scripts;
using Assets.Scripts.Objects.Items;
using UnityEngine;

namespace BrainClock.PlayerComms
{
    public class Radio : PowerTool
    {

        /// <summary>
        /// Ensure the paintable material is the valid one.
        /// </summary>
        public override void Start()
        {
            base.Start();

            this.CustomColor = GameManager.GetColorSwatch("ColorBlue");
            this.PaintableMaterial = this.CustomColor.Normal;
        }
    }
}
