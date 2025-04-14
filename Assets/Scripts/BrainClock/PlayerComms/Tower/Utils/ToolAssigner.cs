using Assets.Scripts.Objects;

namespace BrainClock.PlayerComms
{
    public class ToolAssigner
    {
        private Tower _tower;

        public ToolAssigner(Tower tower)
        {
            _tower = tower;
        }

        public void AssignAllTools()
        {
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

            if (_tower.BuildStates == null) return;

            if (_tower.BuildStates.Count > 0)
                _tower.BuildStates[0].Tool.ToolExit = ItemAngleGrinder;
            if (_tower.BuildStates.Count > 1)
                _tower.BuildStates[1].Tool.ToolExit = ItemCrowbar;
            if (_tower.BuildStates.Count > 2)
                _tower.BuildStates[2].Tool.ToolExit = ItemDrill;
            if (_tower.BuildStates.Count > 3)
                _tower.BuildStates[3].Tool.ToolExit = ItemWireCutters;

            if (_tower.BrokenBuildStates != null)
            {
                _tower.BrokenBuildStates[0].BuildState.Tool.ToolExit = ItemAngleGrinder;
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

            if (_tower.BuildStates == null) return;

            if (_tower.BuildStates.Count > 0)
            {
                _tower.BuildStates[1].Tool.ToolEntry = ItemWeldingTorch ?? ItemArcWelder;
                _tower.BuildStates[1].Tool.ToolEntry2 = ItemSteelSheets;
            }

            if (_tower.BuildStates.Count > 1)
            {
                _tower.BuildStates[2].Tool.ToolEntry = ItemDrill;
                _tower.BuildStates[2].Tool.ToolEntry2 = ItemPlasticSheets;
            }

            if (_tower.BuildStates.Count > 2)
            {
                _tower.BuildStates[3].Tool.ToolEntry = ItemScrewdriver;
                _tower.BuildStates[3].Tool.ToolEntry2 = ItemCableCoilHeavy;
            }
        }

        private void AssignToolRepair()
        {
            var ItemSteelSheets = Prefab.Find<Item>("ItemSteelSheets");
            var ItemWeldingTorch = Prefab.Find<Item>("ItemWeldingTorch");
            var ItemArcWelder = Prefab.Find<Item>("ItemArcWelder");

            if (_tower.BuildStates != null)
            {
                _tower.RepairTools.ToolEntry = ItemWeldingTorch ?? ItemArcWelder;
                _tower.RepairTools.ToolEntry2 = ItemSteelSheets;
            }
        }
    }
}
