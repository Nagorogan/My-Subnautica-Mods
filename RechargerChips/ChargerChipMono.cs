using RechargerChips.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Logger = QModManager.Utility.Logger;

namespace RechargerChips
{
    internal class ChargerChipMono : MonoBehaviour
    {
        public enum ChipType
        {
            Solar,
            Thermal,
            Combo
        }
        public List<ChipType> chipTypesEquipped = new List<ChipType>();

        public static ChargerChipMono main;

        public Dictionary<ChipType, TechType> chipTechTypes = new Dictionary<ChipType,TechType>();

        public const float MaxSolarDepth = 150;
        public const float solarMultiplier = 0.25f;
        public const float thermalMultiplier = 0.5f;

        public void Awake()
        {
            if (main != null) Destroy(this);
            main = this;

            chipTechTypes.Add(ChipType.Solar, SolarChargerChip.thisTechType);
            chipTechTypes.Add(ChipType.Thermal, ThermalChargerChip.thisTechType);
            chipTechTypes.Add(ChipType.Combo, ComboChargerChip.thisTechType);

            UpdateEquipped();

            Inventory.main.equipment.onAddItem += EquipmentChanged;
            Inventory.main.equipment.onRemoveItem += EquipmentChanged;
        }
        public void EquipmentChanged(InventoryItem item)
        {
            UpdateEquipped();
        }
        public void UpdateEquipped()
        {
            chipTypesEquipped.Clear();

            foreach(KeyValuePair<ChipType, TechType> pair in chipTechTypes)
            {
                for(var i=0; i < Inventory.main?.equipment?.GetCount(pair.Value); i++)
                {
                    chipTypesEquipped.Add(pair.Key);
                }
            }
        }

        public void Update()
        {
            GameObject storageRoot = Inventory.main?.storageRoot;

            if (storageRoot == null) return;

            Battery[] batteries = storageRoot.GetComponentsInChildren<Battery>(true);
            float chargeAmount = 0;

            foreach(ChipType chipType in chipTypesEquipped)
            {
                switch (chipType)
                {
                    case ChipType.Solar:
                        chargeAmount += GetSolarCharge();
                        break;
                    case ChipType.Thermal:
                        chargeAmount += GetThermalCharge();
                        break;
                    case ChipType.Combo:
                        chargeAmount += GetThermalCharge() + GetSolarCharge();
                        break;
                }
            }

            foreach(Battery battery in batteries)
            {
                float missingCharge = battery.capacity - battery.charge;

                //Logger.Log(Logger.Level.Info, $"battery missing charge {missingCharge}, amount to charge {chargeAmount}", null, true); 

                if(missingCharge > 0)
                {
                    float amountGiven = chargeAmount;
                    if(missingCharge < chargeAmount)
                    {
                        amountGiven = missingCharge;
                    }
                    //Logger.Log(Logger.Level.Info, $"giving batter {amountGiven}", null, true); 
                    battery.charge += amountGiven;
                    chargeAmount -= amountGiven;

                    if (chargeAmount <= 0)
                    {
                        //Logger.Log(Logger.Level.Info, "charge amount empty, breaking", null, true); 
                        break;
                    }
                }
            }
        }
        private float GetSolarCharge()
        {
            Player player = Player.main;

            if(player == null) return 0;

            DayNightCycle dayNightCycle = DayNightCycle.main;
            if (dayNightCycle == null)
                return 0;

            float depthMultiplier = Mathf.Clamp01((MaxSolarDepth + player.transform.position.y) / MaxSolarDepth);
            float lightScalar = dayNightCycle.GetLocalLightScalar();

            //Log.LogDebug($"Charging Hoverbike battery with depthMultiplier of {depthMultiplier}, lightScalar = {lightScalar}, fSolarChargeMultiplier = {fSolarChargeMultiplier}, and deltaTime of {deltaTime}");
            return Time.deltaTime * solarMultiplier * depthMultiplier * lightScalar;
        }
        private float GetThermalCharge()
        {
            Player player = Player.main;

            if (player == null) return 0;

            WaterTemperatureSimulation waterSim = WaterTemperatureSimulation.main;

            if (waterSim == null)
                return 0f;

            float temperature = waterSim.GetTemperature(player.gameObject.transform.position);

            AnimationCurve curve = CraftData.GetPrefabForTechType(TechType.Exosuit).GetComponent<Exosuit>().thermalReactorCharge;

            return curve.Evaluate(temperature) * thermalMultiplier * Time.deltaTime;
        }
    }
}
