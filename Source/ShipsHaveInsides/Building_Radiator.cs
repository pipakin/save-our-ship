using ShipsHaveInsides.Mod;
using UnityEngine;
using Verse;

namespace RimWorld
{
    public class Building_Radiator : Building_TempControl
    {
        private const float HeatOutputMultiplier = 1.25f;
        private const float EfficiencyLossPerDegreeDifference = 0.007692308f;
        private const int EVAL_TIME = 250;
        private int timeTillEval = EVAL_TIME;

        private UnfoldComponent unfoldComponent;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            unfoldComponent = GetComp<UnfoldComponent>();
        }

        public override void Tick()
        {
            base.Tick();
            if (this.compPowerTrader.PowerOn)
            {
                timeTillEval--;
                if (timeTillEval <= 0)
                {
                    IntVec3 intVec3_1 = this.Position + IntVec3.North.RotatedBy(this.Rotation);

                    IntVec3 intVec3_2 = this.Position + IntVec3.South.RotatedBy(this.Rotation);
                    IntVec3 intVec3_3 = this.Position + (IntVec3.South.RotatedBy(this.Rotation) * 2);
                    IntVec3 intVec3_4 = this.Position + (IntVec3.South.RotatedBy(this.Rotation) * 3);
                    bool flag = false;
                    if (!intVec3_4.Impassable(this.Map) && !intVec3_3.Impassable(this.Map) && !intVec3_2.Impassable(this.Map) && !intVec3_1.Impassable(this.Map))
                    {
                        float temperature1 = intVec3_2.GetTemperature(this.Map);
                        float temperature2 = intVec3_1.GetTemperature(this.Map);
                        float num1 = temperature1 - temperature2;
                        if ((double)temperature1 - 40.0 > (double)num1)
                            num1 = temperature1 - 40f;
                        float num2 = (float)(1.0 - (double)num1 * (1.0 / 130.0));
                        if ((double)num2 < 0.0)
                            num2 = 0.0f;
                        float energyLimit = (float)(((double)this.compTempControl.Props.energyPerSecond) * (double)num2 * 4.16666650772095);
                        float a = GenTemperature.ControlTemperatureTempChange(intVec3_1, this.Map, energyLimit, this.compTempControl.targetTemperature);
                        flag = !Mathf.Approximately(a, 0.0f);
                        if (flag)
                        {
                            intVec3_1.GetRoomGroup(this.Map).Temperature += a;
                            GenTemperature.PushHeat(intVec3_2, this.Map, (float)(-(double)energyLimit * 1.25));
                            GenTemperature.PushHeat(intVec3_3, this.Map, (float)(-(double)energyLimit * 1.25));
                            GenTemperature.PushHeat(intVec3_4, this.Map, (float)(-(double)energyLimit * 1.25));
                        }
                    }
                    CompProperties_Power props = this.compPowerTrader.Props;
                    if (flag)
                        this.compPowerTrader.PowerOutput = -props.basePowerConsumption;
                    else
                        this.compPowerTrader.PowerOutput = -props.basePowerConsumption * this.compTempControl.Props.lowPowerConsumptionFactor;
                    this.compTempControl.operatingAtHighPower = flag;

                    unfoldComponent.Target = flag ? 1.0f : 0.0f;

                    timeTillEval = EVAL_TIME;
                }
            }
            else
            {
                unfoldComponent.Target = 0.0f;
            }
        }
    }
}
