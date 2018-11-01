using Harmony;
using RimWorld;
using ShipsHaveInsides.MapComponents;
using System;
using System.Collections.Generic;
using System.IO;
using Verse;

namespace ShipsHaveInsides.Mod
{
    [HarmonyPatch(typeof(ShipCountdown), "CountdownEnded", null)]
    public static class SaveShip
    {
        [HarmonyPrefix]
        public static bool SaveShipAndRemoveItemStacks()
        {
            if (ShipInteriorMod.saveShip)
            {
                string str1 = Path.Combine(GenFilePaths.SaveDataFolderPath, "Ships");
                DirectoryInfo directoryInfo = new DirectoryInfo(str1);
                if (!directoryInfo.Exists)
                    directoryInfo.Create();
                string name = Faction.OfPlayer.Name;

                var def = ShipInteriorMod.shipRoot.Map.GetSpaceAtmosphereMapComponent().DefinitionAt(ShipInteriorMod.shipRoot.Position);

                
                string str2 = Path.Combine(str1, name + "-" + def.Name + ".rwship2");
                
                SafeSaver.Save(str2, "RWShip", (Action)(() =>
                {
                    ScribeMetaHeaderUtility.WriteMetaHeader();
                    Scribe_Values.Look<FactionDef>(ref Faction.OfPlayer.def, "playerFactionDef", null, false);
                    Scribe_Values.Look(ref name, "playerFactionName");
                    def.ShouldSaveThings = true;
                    Scribe_Deep.Look(ref def, "shipDefinition");
                    def.ShouldSaveThings = false;
                    Scribe_Deep.Look<ResearchManager>(ref Current.Game.researchManager, false, "researchManager", new object[0]);
                    Scribe_Deep.Look<TaleManager>(ref Current.Game.taleManager, false, "taleManager", new object[0]);
                    Scribe_Deep.Look<UniqueIDsManager>(ref Current.Game.uniqueIDsManager, false, "uniqueIDsManager", new object[0]);
                    Scribe_Deep.Look<TickManager>(ref Current.Game.tickManager, false, "tickManager", new object[0]);
                    Scribe_Deep.Look<DrugPolicyDatabase>(ref Current.Game.drugPolicyDatabase, false, "drugPolicyDatabase", new object[0]);
                    Scribe_Deep.Look<OutfitDatabase>(ref Current.Game.outfitDatabase, false, "outfitDatabase", new object[0]);
                    Scribe_Deep.Look<PlayLog>(ref Current.Game.playLog, false, "playLog", new object[0]);
                }));
                def.Destroy(DestroyMode.Vanish);
            }
            return true;
        }
    }
}
