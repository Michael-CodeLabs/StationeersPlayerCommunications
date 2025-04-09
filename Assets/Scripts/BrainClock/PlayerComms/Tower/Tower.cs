using Assets.Scripts;
using Assets.Scripts.Objects;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace BrainClock.PlayerComms
{
    public class Tower : LargeElectrical
    {        
        public override void Start()
        {
            base.Start();

            //AssignTools
            AssignToolExit();
            AssignToolEntry();
            AssignToolRepair();
        }

        private void AssignToolExit()
        {
            var ItemAngleGrinder = Prefab.Find<Item>("ItemAngleGrinder");
            var ItemDrill = Prefab.Find<Item>("ItemDrill");
            var ItemWireCutters = Prefab.Find<Item>("ItemWireCutters");
            var ItemScrewdriver = Prefab.Find<Item>("ItemScrewdriver");
            var ItemCrowbar = Prefab.Find<Item>("ItemCrowbar");

            if (BuildStates == null) return;

            if (BuildStates.Count > 0)
                BuildStates[0].Tool.ToolExit = ItemAngleGrinder;
            if (BuildStates.Count > 1)
                BuildStates[1].Tool.ToolExit = ItemCrowbar;
            if (BuildStates.Count > 2)
                BuildStates[2].Tool.ToolExit = ItemDrill;
            if (BuildStates.Count > 3)
                BuildStates[3].Tool.ToolExit = ItemWireCutters;

            if (BrokenBuildStates != null)
            {
                BrokenBuildStates[0].BuildState.Tool.ToolExit = ItemAngleGrinder;
            }
        }

        private void AssignToolEntry()
        {
            var ItemWeldingTorch = Prefab.Find<Item>("ItemWeldingTorch");
            var ItemArcWelder = Prefab.Find<Item>("ItemArcWelder");
            var ItemScrewdriver = Prefab.Find<Item>("ItemScrewdriver");
            var ItemDrill = Prefab.Find<Item>("ItemDrill");
            var ItemSteelSheets = Prefab.Find<Item>("ItemSteelSheets");
            var ItemPlasticSheets = Prefab.Find<Item>("ItemPlasticSheets");
            var ItemCableCoilHeavy = Prefab.Find<Item>("ItemCableCoilHeavy");

            if (BuildStates == null) return;

            if (BuildStates.Count > 0)
            {
                BuildStates[1].Tool.ToolEntry = ItemWeldingTorch != null ? ItemWeldingTorch : ItemArcWelder;
                BuildStates[1].Tool.ToolEntry2 = ItemSteelSheets;
            }

            if (BuildStates.Count > 1)
            {
                BuildStates[2].Tool.ToolEntry = ItemDrill;
                BuildStates[2].Tool.ToolEntry2 = ItemPlasticSheets;
            }

            if (BuildStates.Count > 2)
            {
                BuildStates[3].Tool.ToolEntry = ItemScrewdriver;
                BuildStates[3].Tool.ToolEntry2 = ItemCableCoilHeavy;
            }
        }

        private void AssignToolRepair()
        {
            var ItemSteelSheets = Prefab.Find<Item>("ItemSteelSheets");
            var ItemWeldingTorch = Prefab.Find<Item>("ItemWeldingTorch");
            var ItemArcWelder = Prefab.Find<Item>("ItemArcWelder");

            if (BuildStates != null)
            {
                RepairTools.ToolEntry = ItemWeldingTorch != null ? ItemWeldingTorch : ItemArcWelder;
                RepairTools.ToolEntry2 = ItemSteelSheets;
            }
        }
    }
}
