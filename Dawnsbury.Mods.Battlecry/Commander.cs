using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.Mechanics.Enumerations;

namespace Dawnsbury.Mods.Battlecry
{
    internal class Commander
    {
        // Armored Regiment Training - not useful in game
        // Combat Assessment - not useful in game
        // Combat Medic - TODO
        // Commander's Steed - no horses (large), but maybe implement?
        // Deceptive Tactics - TODO
        // Plant Banner - TODO

        // Adaptive Stratagem - Maybe? Probably skip
        // Defensive Swap - too many prompts?
        // Guiding Shot - TODO
        // Set-up Strike - TODO
        // Tactical Expansion - probably skip
        // Rapid Assessment - not useful in game

        // Banner Twirl - TODO
        // Observational Analysis - not useful in game
        // Shielded Recovery - TODO
        // Wave the Flag - TODO
        public static IEnumerable<Feat> LoadAll()
        {
            yield return new ClassSelectionFeat(BattlecryMod.FeatName.Commander,
                "You approach battle with the knowledge that tactics and strategy are every bit as crucial as brute strength or numbers. You may have trained in classical theories of warfare and strategy at a military school or you might have refined your techniques through hard-won experience as part of an army or mercenary company. Regardless of how you came by your knowledge, you have a gift for signaling your allies from across the battlefield and shouting commands to rout even the most desperate conflicts, allowing your squad to exceed their limits and claim victory.",
                BattlecryMod.Trait.Commander, new EnforcedAbilityBoost(Ability.Intelligence), 8,
                new[] { Trait.Fortitude, Trait.Armor, Trait.UnarmoredDefense, Trait.Society, Trait.Simple, Trait.Martial, Trait.Unarmed }, // Trait.WarfareLore
                new[] { Trait.Reflex, Trait.Will, Trait.Perception },
                2,
                "{b}1. Commander's Banner{/b} A commander needs a battle standard so their allies can locate them on the field. You start play with a custom banner that you can use to signal allies when using tactics or to deploy specific abilities.\n\n" +
                "{b}2. Tactics{/b}By studying and practicing the strategic arts of war, you can guide your allies to victory.\n\n" +
                "{b}3. Drilled Reactions{/b}\n\n" +
                "{b}4. Shield Block {icon:Reaction}.{/b}You gain the Shield Block general feat.",
                null)
                .WithOnSheet((CalculatedCharacterSheetValues sheet) =>
                {
                    sheet.GrantFeat(FeatName.ShieldBlock);
                    sheet.AddSelectionOption(new SingleFeatSelectionOption("CommanderFeat1", "Commander feat", 1, (feat) => feat.HasTrait(BattlecryMod.Trait.Commander)));
                    sheet.AddSelectionOption(new SingleFeatSelectionOption("CommanderTactics1", "Commander tactics", 1, (feat) => feat.HasTrait(BattlecryMod.Trait.CommanderTactic)));
                    sheet.AddSelectionOption(new SingleFeatSelectionOption("CommanderTactics2", "Commander tactics", 1, (feat) => feat.HasTrait(BattlecryMod.Trait.CommanderTactic)));
                });
        }

        // Defensive Retreat - not too strict on movement
        // Form Up - TODO
        // Mountaineering Training - not useful in game
        // Naval Training - not useful in game
        // Passage of Lines - maybe? might be complicated
        // Coordinating Maneuvers - TODO
        // Double Team - TODO
        // End It - maybe
        // Reload - TODO
        // Shields Up! - TODO
        // Strike Hard! - TODO
        public static IEnumerable<Feat> LoadTactics()
        {
            yield break;
        }
    }
}
