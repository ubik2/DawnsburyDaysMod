using System.Collections.Generic;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Modding;
using Dawnsbury.Mods.Remaster.FeatsDb.TrueFeatsDb;

namespace Dawnsbury.Mods.Remaster.FeatsDb
{

    public class RemasterFeats
    {
        public class Trait
        {
            // It's fine to register the same trait more than once (as will likely happen here from multiple modules)
            public static Core.Mechanics.Enumerations.Trait Remaster = ModManager.RegisterTrait("Remaster");

            // Aliases
            public static Core.Mechanics.Enumerations.Trait Vitality = Core.Mechanics.Enumerations.Trait.Positive;
            public static Core.Mechanics.Enumerations.Trait Void = Core.Mechanics.Enumerations.Trait.Negative;
        }

        public class FeatName
        {
            // Cleric
            public static Core.CharacterBuilder.Feats.FeatName DivineCastigation = ModManager.RegisterFeatName("DivineCastigation", "Divine Castigation");
            public static Core.CharacterBuilder.Feats.FeatName PanicTheDead = ModManager.RegisterFeatName("PanicTheDead", "Panic the Dead");
            public static Core.CharacterBuilder.Feats.FeatName WarpriestsArmor = ModManager.RegisterFeatName("WarpriestsArmor", "Warpriest's Armor");
            public static Core.CharacterBuilder.Feats.FeatName ChannelSmite = ModManager.RegisterFeatName("ChannelSmite", "Channel Smite");
            public static Core.CharacterBuilder.Feats.FeatName RaiseSymbol = ModManager.RegisterFeatName("RaiseSymbol", "Raise Symbol");

            // Fighter
            public static Core.CharacterBuilder.Feats.FeatName SlamDown = ModManager.RegisterFeatName("SlamDown", "Slam Down");
        }

        [DawnsburyDaysModMainMethod]
        public static void LoadMod()
        {
            AddOrReplaceFeats(Cleric.LoadAll());
            AddOrReplaceFeats(Fighter.LoadAll());
        }

        public static void AddOrReplaceFeats(IEnumerable<Feat> feats)
        {
            foreach (var feat in feats)
            {
                // Remove any feats that have the same name as one of our new feats
                AllFeats.All.RemoveAll((existingFeat) => existingFeat.Name == feat.Name);
                if (!feat.HasTrait(Trait.Remaster))
                {
                    feat.Traits.Add(Trait.Remaster);
                }
                ModManager.AddFeat(feat);
            }

        }
    }
}
