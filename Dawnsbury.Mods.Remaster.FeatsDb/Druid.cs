using Dawnsbury.Core.CharacterBuilder.Feats;

namespace Dawnsbury.Mods.Remaster.FeatsDb
{
    public static class Druid
    {
        // The following feats are excluded because they aren't useful enough in gameplay
        // * Animal Empathy
        // * Leshy Familiar
        // * Plant Empathy
        // * Enhanced Familiar?
        // * Anthropomorphic Shape
        // * Forest Passage
        // * Form Control - not enough exploration activity
        // * Leshy Familiar Secrets
        // * Verdant Weapon - no situation where you can't carry weapons

        // * Wild Shape renamed to Untamed Form
        // * Poison Resistance should be a level 2 feat
        // * Stormborn should be named Storm Born

        // Not implemented because of difficulty, but I may reconsider
        // * Elemental Summons - there's really no gameplay between encounters
        // * Mature Animal Companion
        // * Snowdrift Spell - kind of cool way to get difficult terrain
        // * Storm Born - real feat has different functionality, but the balance may be better with the houserule

        // * Grown of Oak
        // * Instinctive Support

        // * Floral Restoration
        // * Raise Menhir

        public static IEnumerable<Feat> LoadAll()
        {
            yield break;
        }
    }
}
