using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Spellbook.RemasterSpells
{

    public class RemasterSpells
    {
        [DawnsburyDaysModMainMethod]
        public static void LoadMod()
        {
            Cantrips.RegisterSpells();
            Level1Spells.RegisterSpells();
            FocusSpells.RegisterSpells();
            Feats.ReplaceExistingFeats();
        }
    }
}
