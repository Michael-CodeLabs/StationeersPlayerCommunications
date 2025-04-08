using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Structures;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BrainClock.PlayerComms
{
    public class Tower : LargeElectrical
    {
        //Assign Constructions Materials
        public override void Start()
        {
            base.Start();
            AssignToolExit();
            AssignEntryTool();
        }

        private void AssignToolExit()
        {
            var ItemAngleGrinder = Prefab.Find<Item>("ItemAngleGrinder");
            var ItemWireCutters = Prefab.Find<Item>("ItemWireCutters");
            var ItemScrewdriver = Prefab.Find<Item>("ItemScrewdriver");
            var ItemCrowbar = Prefab.Find<Item>("ItemCrowbar");

            if (BuildStates == null) return;

            if (BuildStates.Count > 0)
                BuildStates[0].Tool.ToolExit = ItemAngleGrinder;
            if (BuildStates.Count > 1)
                BuildStates[1].Tool.ToolExit = ItemCrowbar;
            if (BuildStates.Count > 2)
                BuildStates[2].Tool.ToolExit = ItemScrewdriver;
            if (BuildStates.Count > 3)
                BuildStates[3].Tool.ToolExit = ItemWireCutters;
        }

        private void AssignEntryTool()
        {
            var ItemWeldingTorch = Prefab.Find<Item>("ItemWeldingTorch");
            var ItemArcWelder = Prefab.Find<Item>("ItemArcWelder");
            var ItemScrewdriver = Prefab.Find<Item>("ItemScrewdriver");
            var ItemWireCutters = Prefab.Find<Item>("ItemWireCutters");
            var ItemSteelSheets = Prefab.Find<Item>("ItemSteelSheets");
            var ItemPlasticSheets = Prefab.Find<Item>("ItemPlasticSheets");
            var ItemCableCoilHeavy = Prefab.Find<Item>("ItemCableCoilHeavy");

            if (BuildStates == null) return;

            if (BuildStates.Count > 0)
            {
                BuildStates[1].Tool.ToolEntry = ItemWeldingTorch ?? ItemArcWelder;
                BuildStates[1].Tool.ToolEntry2 = ItemSteelSheets;
            }

            if (BuildStates.Count > 1)
            {
                BuildStates[2].Tool.ToolEntry = ItemScrewdriver;
                BuildStates[2].Tool.ToolEntry2 = ItemPlasticSheets;
            }

            if (BuildStates.Count > 2)
            {
                BuildStates[3].Tool.ToolEntry = ItemWireCutters;
                BuildStates[3].Tool.ToolEntry2 = ItemCableCoilHeavy;
            }
        }
    }
}
