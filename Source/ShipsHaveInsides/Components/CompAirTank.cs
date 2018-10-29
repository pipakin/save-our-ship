using ShipsHaveInsides.MapComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
    [StaticConstructorOnStartup]
    public class CompAirTank : ThingComp
    {
        private static readonly Vector2 BarSize = new Vector2(0.8f, 0.07f);
        private static readonly Material PowerPlantSolarBarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.5f, 0.475f, 0.1f), false);
        private static readonly Material PowerPlantSolarBarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.15f, 0.15f, 0.15f), false);
        private GasMixture gas = GasMixture.Vacuum;

        private float MaxPressure => (props as CompProperties_AirTank).maxPressure;

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Deep.Look(ref gas, "gas");
        }

        public override string CompInspectStringExtra()
        {
            return new StringBuilder(base.CompInspectStringExtra())
                .Append("Total Pressure: ")
                .Append(gas.totalPressure.ToString("0.00"))
                .AppendLine(" kPa")
                .Append("Max Pressure: ")
                .Append(MaxPressure.ToString("0.00"))
                .Append(" kPa")
                .ToString();
        }

        public override void PostDraw()
        {
            base.PostDraw();
            GenDraw.FillableBarRequest r = new GenDraw.FillableBarRequest();
            r.center = this.parent.DrawPos + Vector3.up * 0.1f;
            r.size = BarSize;
            r.fillPercent = gas.totalPressure / MaxPressure;
            r.filledMat = PowerPlantSolarBarFilledMat;
            r.unfilledMat = PowerPlantSolarBarUnfilledMat;
            r.margin = 0.15f;
            Rot4 rotation = this.parent.Rotation;
            rotation.Rotate(RotationDirection.Clockwise);
            r.rotation = rotation;
            GenDraw.DrawFillableBar(r);
        }

        public override void CompTick()
        {
            base.CompTick();

            if (Find.TickManager.TicksAbs % 50 != 0)
                return;

            var def = parent.Map.GetSpaceAtmosphereMapComponent().DefinitionAt(parent.Position);
            var calc = new ShipDefinition.GasCalculator(def);
            var rg = parent.GetRoomGroup();
            var roomGas = def.GetGas(rg);

            if (parent.Map.IsSpace())
            {
                //dump air into the current environment
                var pressureDif = GasMixture.EarthNorm.totalPressure - roomGas.mixture.totalPressure;
                if (pressureDif > 0.0f)
                {
                    //release enough gas (or whatever's left) to get the gas up to 101.325 kPa.
                    var pressureNeeded = pressureDif * rg.CellCount;

                    if(pressureNeeded > gas.totalPressure)
                    {
                        calc.GasExchange(rg, added: new GasVolume(GasMixture.atPressure(gas, gas.totalPressure / (float)rg.CellCount), rg.CellCount));
                    }
                    else
                    {
                        calc.GasExchange(rg, added: new GasVolume(GasMixture.atPressure(gas, pressureDif), rg.CellCount));
                    }
                    gas = gas - pressureNeeded;
                }
            }
            else
            {
                if (gas.totalPressure >= MaxPressure)
                    return;
                //don't steal gas from the ship while in atmo. Deal with this later.
                //calc.GasExchange(rg, removed: new GasVolume(roomGas.mixture, 2f));
                //calc.Execute();

                gas = gas + GasMixture.EarthNorm * 2f;

                if (gas.totalPressure > MaxPressure)
                    gas = GasMixture.atPressure(gas, MaxPressure);
            }
        }
    }
}
