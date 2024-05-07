using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.Mechanics.Enumerations;

namespace Dawnsbury.Mods.Remaster.FeatsDb
{
    // Plant Evidence - no reason to plant an object
    // Trap Finder - not enough traps

    // Overextending Feint - alternate success options for a feint that provide penalty instead of off-guard
    // Tumble Behind (discussed?)
    // Clever Gambit - unimplemented mastermind racket
    // Strong Arm - better thrown weapon ranges

    // These should probably be implemented
    // * Distracting Feint
    // * Underhanded Assault

    // You're Next should let Demoralize be used on a target within 12 squares instead of 6
    // Trained in martial weapons
    // Ruffian Sneak Attack restrictions lessened
    // Thief can add dex modifier to unarmed attack damage

    public static class Rogue
    {
        public static IEnumerable<Feat> LoadAll()
        {
            PatchRogue();
            yield break;
        }

        private static void PatchRogue()
        {
            ClassSelectionFeat classFeat = (ClassSelectionFeat)AllFeats.All.First((feat) => feat.FeatName == FeatName.Rogue);
            // Grant trained with martial weapons
            classFeat.RulesText = classFeat.RulesText.Replace("You're trained in all simple weapons, as well as the rapier, shortbow and shortsword.", "You're trained in all simple and martial weapons.");
            classFeat.OnSheet = (Action<CalculatedCharacterSheetValues>)Delegate.Combine(classFeat.OnSheet, (CalculatedCharacterSheetValues sheet) => sheet.SetProficiency(Trait.Martial, Proficiency.Trained));
            // TODO: Should probably add a QEffect from the ThiefRacket that provides a strike modifier which will add dex modifier to unarmed attack damage
            // TODO: Should alter the QEffect from the RuffianRacket that does sneak attack to exclude simple weapons over d8 and include martial weapons of d6 or less.
        }
    }
}
