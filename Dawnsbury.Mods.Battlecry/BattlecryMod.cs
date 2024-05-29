using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Battlecry
{
    public class BattlecryMod
    {
        public class Trait
        {
            public static readonly Core.Mechanics.Enumerations.Trait Commander = ModManager.RegisterTrait("Commander", new TraitProperties("Commander", relevant: true) { IsClassTrait = true });
            public static readonly Core.Mechanics.Enumerations.Trait Guardian = ModManager.RegisterTrait("Guardian", new TraitProperties("Guardian", relevant: true) { IsClassTrait = true });

            public static readonly Core.Mechanics.Enumerations.Trait Tactic = ModManager.RegisterTrait("Tactic");
            public static readonly Core.Mechanics.Enumerations.Trait Banner = ModManager.RegisterTrait("Banner");

        }

        public class FeatName
        {
            public static readonly Core.CharacterBuilder.Feats.FeatName Commander = ModManager.RegisterFeatName("CommanderClass", "Commander");
            public static readonly Core.CharacterBuilder.Feats.FeatName Guardian = ModManager.RegisterFeatName("GuardianClass", "Guardian");

            public static readonly Core.CharacterBuilder.Feats.FeatName CombatMedic = ModManager.RegisterFeatName("CombatMedic", "Combat Medic");
            public static readonly Core.CharacterBuilder.Feats.FeatName CommandersSteed = ModManager.RegisterFeatName("CommandersSteed", "Commander's Steed");
            
            public static readonly Core.CharacterBuilder.Feats.FeatName PincerAttack = ModManager.RegisterFeatName("PincerAttack", "Pincer Attack");
            public static readonly Core.CharacterBuilder.Feats.FeatName StrikeHard = ModManager.RegisterFeatName("StrikeHard", "Strike Hard");

            public static readonly Core.CharacterBuilder.Feats.FeatName GuardiansArmor = ModManager.RegisterFeatName("GuardiansArmor", "Guardian's Armor");
            public static readonly Core.CharacterBuilder.Feats.FeatName InterceptStrike = ModManager.RegisterFeatName("InterceptStrike", "Intercept Strike");
            public static readonly Core.CharacterBuilder.Feats.FeatName Taunt = ModManager.RegisterFeatName("Taunt");

            public static readonly Core.CharacterBuilder.Feats.FeatName LongDistanceTaunt = ModManager.RegisterFeatName("LongDistanceTaunt", "Long-Distance Taunt");
            public static readonly Core.CharacterBuilder.Feats.FeatName UnkindShove = ModManager.RegisterFeatName("UnkindShove", "Unkind Shove");

            public static readonly Core.CharacterBuilder.Feats.FeatName HamperingSweeps = ModManager.RegisterFeatName("HamperingSweeps", "Hampering Sweeps");
            public static readonly Core.CharacterBuilder.Feats.FeatName RaiseHaft = ModManager.RegisterFeatName("RaiseHaft", "Raise Haft");
            public static readonly Core.CharacterBuilder.Feats.FeatName ShieldedTaunt = ModManager.RegisterFeatName("ShieldedTaunt", "Shielded Taunt");

            public static readonly Core.CharacterBuilder.Feats.FeatName FlyingTackle = ModManager.RegisterFeatName("FlyingTackle", "Flying Tackle");
            public static readonly Core.CharacterBuilder.Feats.FeatName InterceptEnergy = ModManager.RegisterFeatName("InterceptEnergy", "Intercept Energy");
        }

        [DawnsburyDaysModMainMethod]
        public static void LoadMod()
        {
            PatchFeats();

            // https://downloads.paizo.com/042924_Pathfinder_Battlecry_Playtest.pdf
            AddOrReplaceFeats(Commander.LoadAll());
            AddOrReplaceFeats(Guardian.LoadAll());
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
                if (feat.FeatName == Core.CharacterBuilder.Feats.FeatName.ReactiveShield && !feat.HasTrait(Trait.Guardian) && feat is TrueFeat trueFeat)
                {
                    trueFeat.WithAllowsForAdditionalClassTrait(Trait.Guardian);
                }
            });
        }
    }
}
