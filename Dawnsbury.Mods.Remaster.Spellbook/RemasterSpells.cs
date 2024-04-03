using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Remaster.Spellbook
{

    public class RemasterSpells
    {
        public static Trait RemasterTrait = ModManager.RegisterTrait("Remaster");

        [DawnsburyDaysModMainMethod]
        public static void LoadMod()
        {
            Cantrips.RegisterSpells();
            Level1Spells.RegisterSpells();
            FocusSpells.RegisterSpells();
        }
    }
}
