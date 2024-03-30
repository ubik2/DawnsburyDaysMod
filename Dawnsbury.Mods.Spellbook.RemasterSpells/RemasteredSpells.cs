using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Spellbook.RemasteredSpells
{

    public class RemasteredSpells
    {
        [DawnsburyDaysModMainMethod]
        public static void LoadMod()
        {
            Cantrips.RegisterSpells();
            FocusSpells.RegisterSpells();
        }
    }
}
