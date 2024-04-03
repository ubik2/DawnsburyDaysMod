using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Remaster.Spellbook
{

    public class RemasterSpells
    {
        public class Trait
        {
            public static Core.Mechanics.Enumerations.Trait Remaster = ModManager.RegisterTrait("Remaster");
            public static Core.Mechanics.Enumerations.Trait Sanctified = ModManager.RegisterTrait("Sanctified");
            public static Core.Mechanics.Enumerations.Trait Spirit = ModManager.RegisterTrait("Spirit");
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
