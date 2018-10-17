using HugsLib;
using HugsLib.Utils;
using System.Collections.Generic;
using Verse;

namespace ShipsHaveInsides.Mod
{
    public class ShipInteriorMod : ModBase
    {
        public static List<Building> closedSet = new List<Building>();
        public static List<Building> openSet = new List<Building>();
        public static ModLogger instLogger;
        public static bool saveShip;
        public static Building shipRoot;

        public override string ModIdentifier
        {
            get
            {
                return nameof(ShipInteriorMod);
            }
        }

        public override void Initialize()
        {
            ShipInteriorMod.instLogger = this.Logger;
        }

        public static void Log(string toLog)
        {
            ShipInteriorMod.instLogger.Message(toLog, new object[0]);
        }
    }
}