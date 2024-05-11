using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.IO;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Battlecry
{
    public class BattlecryMod
    {
        public class Trait
        {
            public static readonly Core.Mechanics.Enumerations.Trait Commander = ModManager.RegisterTrait("Commander", new TraitProperties("Commander", relevant: true) { IsClassTrait = true });
            public static readonly Core.Mechanics.Enumerations.Trait Guardian = ModManager.RegisterTrait("Guardian", new TraitProperties("Guardian", relevant: true) { IsClassTrait = true });

            public static readonly Core.Mechanics.Enumerations.Trait CommanderTactic = ModManager.RegisterTrait("CommanderTactic");
        }

        public class FeatName
        {
            public static readonly Core.CharacterBuilder.Feats.FeatName Commander = ModManager.RegisterFeatName("CommanderClass", "Commander");
            public static readonly Core.CharacterBuilder.Feats.FeatName Guardian = ModManager.RegisterFeatName("GuardianClass", "Guardian");

            public static readonly Core.CharacterBuilder.Feats.FeatName InterceptStrike = ModManager.RegisterFeatName("InterceptStrike", "Intercept Strike");
        }

        [DawnsburyDaysModMainMethod]
        public static void LoadMod()
        {
            PatchFeats();

            // https://downloads.paizo.com/042924_Pathfinder_Battlecry_Playtest.pdf
            AddOrReplaceFeats(Commander.LoadAll());
            AddOrReplaceFeats(Guardian.LoadAll());

            // A list of legacy spell ids for each level (starting at 0)
            //SpellId[] legacySpells = new[] {
            //   SpellId.AcidSplash, SpellId.RayOfFrost, SpellId.ProduceFlame, SpellId.DisruptUndead, SpellId.ChillTouch,
            //   SpellId.BurningHands, SpellId.ColorSpray, SpellId.MagicMissile, SpellId.MageArmor, SpellId.MagicWeapon, SpellId.TrueStrike, SpellId.ShockingGrasp,
            //   SpellId.AcidArrow, SpellId.CalmEmotions, SpellId.FlamingSphere, SpellId.HideousLaughter, SpellId.ObscuringMist, SpellId.SoundBurst, SpellId.Barkskin, SpellId.SpiritualWeapon, SpellId.TouchOfIdiocy
            //};
            //ModManager.RegisterActionOnEachSpell((spell) =>
            //{
            //    if (legacySpells.Contains(spell.SpellId) && !spell.Traits.Contains(Trait.SpellCannotBeChosenInCharacterBuilder))
            //    {
            //        spell.Traits.Add(Trait.SpellCannotBeChosenInCharacterBuilder);
            //    }
            //});

            //GeneralLog.Log("Loaded HideLegacySpells mod");
        }


        private static void AddOrReplaceFeats(IEnumerable<Feat> feats)
        {
            foreach (var feat in feats)
            {
                // Remove any feats that have the same name as one of our new feats
                AllFeats.All.RemoveAll((existingFeat) => existingFeat.FeatName == feat.FeatName);
                ModManager.AddFeat(feat);
            }
        }

        private static void PatchFeats()
        {
            AllFeats.All.ForEach((feat) =>
            {
                if (feat.FeatName == Core.CharacterBuilder.Feats.FeatName.ReactiveShield && !feat.HasTrait(Trait.Guardian))
                {
                    feat.Traits.Add(Trait.Guardian);
                    if (feat.Traits.Any((trait) => trait.GetTraitProperties().IsClassTrait))
                    {
                        feat.Traits.Add(Core.Mechanics.Enumerations.Trait.ClassFeat);
                        // Remove any existing ClassPrerequisite, and add an updated one
                        feat.Prerequisites = feat.Prerequisites.Where((prereq) => prereq is not ClassPrerequisite).Append(new ClassPrerequisite(feat.Traits.Where((trait) => trait.GetTraitProperties().IsClassTrait).ToList())).ToList();
                    }
                }
            });
        }
    }
}
