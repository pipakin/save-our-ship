using ShipsHaveInsides.MapComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace ShipsHaveInsides
{
    public class Dialog_NameShip : Dialog_Rename
    {
        private ShipDefinition ship;

        public Dialog_NameShip(ShipDefinition ship)
        {
            this.ship = ship;
            curName = ship.Name;
        }

        protected override void SetName(string name)
        {
            if (name == ship.Name || string.IsNullOrEmpty(name))
                return;

            ship.Name = name;
        }
    }
}
