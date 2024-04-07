using Dawnsbury.Core.Creatures;
using Dawnsbury.Core;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Modding;
using System.Collections.Generic;

namespace Dawnsbury.Mods.Remaster.Spellbook
{

    public class RemasterSpells
    {
        public class Trait
        {
            public static Core.Mechanics.Enumerations.Trait Remaster = ModManager.RegisterTrait("Remaster");
            public static Core.Mechanics.Enumerations.Trait Sanctified = ModManager.RegisterTrait("Sanctified");
            public static Core.Mechanics.Enumerations.Trait Spirit = ModManager.RegisterTrait("Spirit");
            public static Core.Mechanics.Enumerations.Trait Disease = ModManager.RegisterTrait("Disease");
        }

        [DawnsburyDaysModMainMethod]
        public static void LoadMod()
        {
            Cantrips.RegisterSpells();
            Level1Spells.RegisterSpells();
            FocusSpells.RegisterSpells();
        }
    }
}
