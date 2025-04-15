using Assets.Scripts;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrainClock.PlayerComms
{
    public class Tower : LargeElectrical, ISetable, ILogicable, IReferencable
    {
        // Sets Modestrings to the TowerModeStrings enum
        public static string[] TowerModeString = Enum.GetNames(typeof(TowerMode));
        public override string[] ModeStrings => TowerModeString;

        //Reference to Tower ToolAssigner Class
        private ToolAssigner _toolAssigner;
        
        // Global registeration so radios or a manager can iterate over all towers.
        public static List<Tower> AllTowers = new();
        public static Dictionary<int, long> TowerModes = new Dictionary<int, long>();

        //Add Events later ** **

        [Header("Tower")]
        public TowerMode TowerMode = TowerMode.Default;
        public float RangeDefault = 250;
        public float RangeMax = 1000;
        public float PowerScale = 15f;
        public RadioRangeController RadioRangeController;


        // Needed for ISetable
        private float _setting;
        [ByteArraySync]
        public double Setting
        {
            get
            {
                return _setting;
            }
            set
            {
                _setting = Mathf.Clamp((float)value, 0f, RangeMax);
                if (NetworkManager.IsServer)
                {
                    base.NetworkUpdateFlags |= 256;
                }
                _setting = (float)value;

                if (RadioRangeController != null)
                    RadioRangeController.Range = _setting;

            }
        }

        //LogicTypes ReadWrite HashSet
        private static readonly HashSet<LogicType> ReadLogicTypes = new()
        {
            LogicType.Power,
            LogicType.Setting,
            LogicType.Error,
            LogicType.RequiredPower,
            LogicType.ReferenceId,
            LogicType.PrefabHash,
            LogicType.NameHash,
            LogicType.Lock,
            LogicType.On,
            LogicType.Mode // 0 Default, 1 Transmit 2 Recive | Maybe? (Not really needed as this adds too much to work on, but would be nice to have. Mode 3 could be both 0 and 1 combined.
        };

        private static readonly HashSet<LogicType> WriteLogicTypes = new()
        {
            LogicType.Setting,
            LogicType.Lock,
            LogicType.On,
            LogicType.Mode // 0 Default // 1 Transmit // 2 Recive
        };
        
        // Can Logic Read Write
        public override bool CanLogicRead(LogicType logicType)
        {
            return ReadLogicTypes.Contains(logicType) || base.CanLogicRead(logicType);
        }
        public override bool CanLogicWrite(LogicType logicType)
        {
            return WriteLogicTypes.Contains(logicType) || base.CanLogicWrite(logicType);
        }

        //Handle Setting change
        public override double GetLogicValue(LogicType logictype)
        {
            if (logictype == LogicType.Setting)
            {
                return Setting;
            }
            return base.GetLogicValue(logictype);
        }

        public override void SetLogicValue(LogicType logicType, double value)
        {
            base.SetLogicValue(logicType, value);
            if (logicType == LogicType.Setting)
            {
                Setting = Mathf.Clamp((float)value, 0f, RangeMax);
            }
        }
        //Update Dedicated Server
        public virtual void OnSettingChanged()
        {
            if (NetworkManager.IsServer)
            {
                base.NetworkUpdateFlags |= 256;
            }


        }

        //Serialize - Deserialize On Join
        public override void SerializeOnJoin(RocketBinaryWriter writer)
        {
            base.SerializeOnJoin(writer);
            writer.WriteDouble(Setting);
        }
        public override void DeserializeOnJoin(RocketBinaryReader reader)
        {
            base.DeserializeOnJoin(reader);
            Setting = reader.ReadDouble();
        }

        // Serialize - Deserialze On World Save
        public override ThingSaveData SerializeSave()
        {
            ThingSaveData savedData = new LogicBaseSaveData();
            InitialiseSaveData(ref savedData);
            return savedData;
        }

        public override void DeserializeSave(ThingSaveData savedData)
        {
            base.DeserializeSave(savedData);
            if (savedData is LogicBaseSaveData logicBaseSaveData)
            {
                Setting = logicBaseSaveData.Setting;
            }
        }

        // Initalise Save Data
        protected override void InitialiseSaveData(ref ThingSaveData savedData)
        {
            base.InitialiseSaveData(ref savedData);
            if (savedData is LogicBaseSaveData logicBaseSaveData)
            {
                logicBaseSaveData.Setting = Setting;
            }
        }

        //Process Setting Updates on dedicated servers? (Might be completly useless in this code?)
        public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
        {
            base.BuildUpdate(writer, networkUpdateType);
            if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
            {
                writer.WriteDouble(Setting);
            }
        }

        public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
        {
            base.ProcessUpdate(reader, networkUpdateType);
            if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
            {
                Setting = reader.ReadDouble();
            }
        }

        //Scale Power With Setting
        public override float GetUsedPower(CableNetwork cableNetwork)
        {

            if (!OnOff || cableNetwork != base.PowerCableNetwork || base.PowerCableNetwork == null)
            {
                return 0f;
            }
            return UsedPower + PowerScale * _setting;
        }

        public override void Awake()
        {

            //Assign Default Signal
            Setting = RangeDefault;
            Debug.Log("Set Tower Default Signal strength to " + Setting);
            base.Awake();
        }
        public override void Start()
        {
            base.Start();


            //Setting = DefaultSignalStrength;
            _toolAssigner = new ToolAssigner(this);
            _toolAssigner.AssignAllTools();

            //Add towers to AllTower list
            AllTowers.Add(this);

            // Adjust range to default for now
            if (RadioRangeController != null)
                RadioRangeController.Range = (float)Setting;
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            AllTowers.Remove(this);
        }
    }
}
