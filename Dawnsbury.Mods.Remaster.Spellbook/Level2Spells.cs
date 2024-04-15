using System;
using Dawnsbury.Audio;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.Animations;
using Dawnsbury.Modding;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Display.Text;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Roller;
using Dawnsbury.Core.Mechanics;

namespace Dawnsbury.Mods.Remaster.Spellbook
{
    public static class Level2Spells
    {
        // The following spells are excluded because they aren't useful enough in gameplay
        // * Animal Messenger
        // * Augury
        // * Cleanse Affliction
        // * Create Food
        // * Darkvision
        // * Deafness
        // * Dispel Magic
        // * Embed Message
        // * Environmental Endurance (formerly Endure Elements)
        // * Everlight
        // * Gecko Grip
        // * Humanoid Form
        // * Knock
        // * Marvelous Mount
        // * One with Plants
        // * Peaceful Rest
        // * Shape Wood
        // * Shatter
        // * Speak with Animals
        // * Status
        // * Sure Footing
        // * Translate
        // * Water Breathing
        // * Water Walk
        // The following spells are excluded because of their difficulty
        // * Animal Form
        // * Darkness (no light model)
        // * Enlarge (creature sizes not supported)
        // * Ghostly Carrier (creature management)
        // * Paranoia
        // * Shrink (creature sizes not supported)
        // * Silence
        // The following are in limbo
        // * Clear Mind
        // * Entangling Flora (from Entangle)
        // * Illusory Creature
        // * Share Life
        // * Sound Body

        public static void RegisterSpells()
        {
            // Acid Arrow => Acid Grip
            // Renamed from Acid Arrow. Updated traits, description, and functionality
            ModManager.RegisterNewSpell("AcidGrip", 2, ((spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                int heightenIncrements = (spellLevel - 2) / 2;

                return Spells.CreateModern(IllustrationName.AcidArrow, "Acid Grip", new[] { Trait.Acid, Trait.Concentrate, Trait.Manipulate, Trait.Arcane, Trait.Primal, RemasterSpells.Trait.Remaster },
                    "An ephemeral, taloned hand grips the target, burning it with magical acid.",
                    "The target takes " + S.HeightenedVariable(2 + 2 * heightenIncrements, 2) + "d8 acid damage plus " + S.HeightenedVariable(1 + heightenIncrements, 1) + "d6 persistent acid damage depending on its Reflex save. A creature taking persistent damage from this spell takes a –10-foot status bonus to its Speeds." +
                    S.FourDegreesOfSuccess("The creature is unaffected.", 
                        "The creature takes half damage and no persistent damage, and the claw moves it up to 5 feet in a direction of your choice.",
                        "The creature takes full damage and persistent damage, and the claw moves it up to 10 feet in a direction of your choice.", 
                        "The creature takes double damage and full persistent damage, and the claw moves it up to 20 feet in a direction of your choice."),
                    // S.HeightenText(spellLevel, 2, inCombat, "{b}Heightened (+2){/b} The initial damage increases by 2d8, and the persistent acid damage increases by 1d6."),
                    Target.Ranged(24), spellLevel, SpellSavingThrow.Basic(Defense.Reflex))
                .WithSoundEffect(SfxName.AcidSplash)
                .WithGoodnessAgainstEnemy((Target t, Creature a, Creature d) => (float)((t.OwnerAction.SpellLevel - 1) * 2) * 4.5f)
                .WithEffectOnEachTarget(async delegate (CombatAction spell, Creature caster, Creature target, CheckResult checkResult)
                {
                    await CommonSpellEffects.DealBasicDamage(spell, caster, target, checkResult, (2 + 2 * heightenIncrements) + "d8", DamageKind.Acid);
                    if (checkResult <= CheckResult.Failure)
                    {
                        DiceFormula diceFormula = DiceFormula.FromText((1 + heightenIncrements) + "d6", "Persistent damage");
                        target.AddQEffect(QEffect.PersistentDamage(diceFormula, DamageKind.Acid));
                    }
                    int moveDistance = checkResult switch { CheckResult.Success => 1, CheckResult.Failure => 2, CheckResult.CriticalFailure => 4, _ => 0 };
                    if (moveDistance > 0)
                    {
                        // FIXME: should be able to move any direction
                        await caster.PushCreature(target, moveDistance);
                    }
                });
            }));

            // ScorchingRay => Blazing Bolts
            ModManager.RegisterNewSpell("BlazingBolt", 2, ((spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                Func<CreatureTarget> func = () => Target.Ranged(12, (Target tg, Creature attacker, Creature defender) => attacker.AI.DealDamage(defender, 14f, tg.OwnerAction));
                return Spells.CreateModern(IllustrationName.BurningHands, "Blazing Bolt", new[] { Trait.Attack, Trait.Concentrate, Trait.Fire, Trait.Manipulate, Trait.Arcane, Trait.Primal, RemasterSpells.Trait.Remaster },
                    "You fire a ray of heat and flame.",
                    "{b}Range{/b} 60 feet\n{b}Targets{/b} 1 or more creatures\n\n" +
                    "Make a spell attack roll against a single creature. On a hit, the target takes " + S.HeightenedVariable(spellLevel, 2) + "d6 fire damage, and on a critical hit, the target takes double damage.\n\n" +
                    "For each additional action you use when Casting the Spell, you can fire an additional ray at a different target, to a maximum of three rays targeting three different targets for 3 actions.\n\n" +
                    "These attacks each increase your multiple attack penalty, but you don't increase your multiple attack penalty until after you make all the spell attack rolls for blazing bolt. If you spend 2 or more actions Casting the Spell, the damage increases to " + S.HeightenedVariable(2 * spellLevel, 4) + "d6 fire damage on a hit, and it still deals double damage on a critical hit.",
                    //S.HeightenText(spellLevel, 2, inCombat, "{b}Heightened (+1){/b} The damage to each target increases by 1d6 for the 1-action version, or by 2d6 for the 2- and 3-action versions."),
                    Target.DependsOnActionsSpent(
                        Target.MultipleCreatureTargets(func()).WithMustBeDistinct().WithOverriddenTargetLine("1 creature", plural: false),
                        Target.MultipleCreatureTargets(func(), func()).WithMustBeDistinct().WithOverriddenTargetLine("1 or 2 creatures", plural: true),
                        Target.MultipleCreatureTargets(func(), func(), func()).WithMustBeDistinct().WithOverriddenTargetLine("1, 2 or 3 creatures", plural: true)), spellLevel, null)
                .WithActionCost(-1).WithSoundEffect(SfxName.MagicMissile)
                .WithProjectileCone(IllustrationName.MagicMissile, 15, ProjectileKind.Ray)
                .WithCreateVariantDescription((int actionCost, SpellVariant? variant) => (actionCost != 1)
                    ? ("You fire " + actionCost + " rays of heat and flame. Make a spell attack roll against a single creature. On a hit, the target takes " + S.HeightenedVariable(2 * spellLevel, 4) + "d6 fire damage, and on a critical hit, the target takes double damage.")
                    : ("You fire a ray of heat and flame. Make a spell attack roll against a single creature. On a hit, the target takes " + S.HeightenedVariable(spellLevel, 2) + "d6 fire damage, and on a critical hit, the target takes double damage."))
                .WithSpellAttackRoll()
                .WithEffectOnChosenTargets(async delegate (CombatAction spell, Creature caster, ChosenTargets targets)
                {
                    string damageDice = (spell.SpentActions > 1) ? (2 * spellLevel + "d6") : (spellLevel + "d6");
                    foreach (Creature target in targets.GetTargetCreatures()) 
                    {
                        await CommonSpellEffects.DealAttackRollDamage(spell, caster, target, targets.CheckResults[target], damageDice, DamageKind.Fire);
                    }
                    // Each extra ray increases our attack penalty (after all rolls)
                    caster.Actions.AttackedThisManyTimesThisTurn += spell.SpentActions - 1;
                })
                .WithTargetingTooltip(delegate (CombatAction power, Creature creature, int index)
                {
                    string ordinal = index switch
                    {
                        0 => "first",
                        1 => "second",
                        2 => "third",
                        _ => index + "th",
                    };
                    return "Send the " + ordinal + " ray at " + creature?.ToString() + ". (" + (index + 1) + "/" + power.SpentActions + ")";
                });
            }));

            // Calm (formerly Calm Emotions)            
            // False Vitality (handle like Mystic Armor)
            // Floating Flame (formerly Flaming Sphere)
            // Laughing Fit (formerly Hideous Laughter)
            // Mist (formerly Obscuring Mist)
            
            // Noise Blast (formerly Sound Burst)
            ModManager.RegisterNewSpell("NoiseBlast", 2, ((spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.SoundBurst, "Noise Blast", new[] { Trait.Concentrate, Trait.Manipulate, Trait.Sonic, Trait.Arcane, Trait.Divine, Trait.Occult, RemasterSpells.Trait.Remaster },
                    "A cacophonous noise blasts out dealing " + S.HeightenedVariable(spellLevel, 2) + "d10 sonic damage.",
                    "Each creature must attempt a Fortitude save." + 
                    S.FourDegreesOfSuccess("The creature is unaffected.", "The creature takes half damage.", "The creature takes full damage is deafened for 1 round.", "The creature takes double damage and is deafened for 1 minute, and stunned 1."),
                    Target.Burst(6, 2), spellLevel, SpellSavingThrow.Basic(Defense.Fortitude))
                .WithSoundEffect(SfxName.SoundBurst)
                .WithEffectOnEachTarget(async delegate (CombatAction spell, Creature caster, Creature target, CheckResult checkResult)
                {
                    await CommonSpellEffects.DealBasicDamage(spell, caster, target, checkResult, "2d10", DamageKind.Sonic);
                    if (checkResult == CheckResult.Failure)
                    {
                        target.AddQEffect(QEffect.Deafened().WithExpirationAtStartOfSourcesTurn(caster, 1));
                    }
                    if (checkResult == CheckResult.CriticalFailure)
                    {
                        target.AddQEffect(QEffect.Deafened().WithExpirationNever());
                        target.AddQEffect(QEffect.Stunned(1));
                    }
                });
            }));

            // Oaken Resilience (formerly Barkskin)
            ModManager.RegisterNewSpell("OakenResilience", 2, ((spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.Barkskin, "Oaken Resilience", new[] { Trait.Concentrate, Trait.Manipulate, Trait.Plant, Trait.Wood, Trait.Arcane, Trait.Primal, RemasterSpells.Trait.Remaster },
                    "The target's skin becomes tough, with a consistency like bark or wood.",
                    "The target gains resistance 2 to bludgeoning and piercing damage and weakness 3 to fire. After the target takes fire damage, it can Dismiss the spell as a free action triggered by taking the damage; doing so doesn't reduce the fire damage the target was dealt.", 
                    Target.AdjacentFriendOrSelf((Target tg, Creature a, Creature d) => (a == d && !a.HasEffect(QEffectId.Barkskin) && !a.HasEffect(QEffectId.EndedBarkskin)) ? 15 : int.MinValue), spellLevel, null).WithSoundEffect(SfxName.ArmorDon)
                .WithEffectOnEachTarget(async delegate (CombatAction spell, Creature caster, Creature target, CheckResult result)
                {
                    QEffect updatedEffect = QEffect.Barkskin();
                    updatedEffect.Name = "Oaken Resilience";
                    target.AddQEffect(updatedEffect);
                });
            }));

            // Revealing Light (formerly Faerie Fire)
            // See the Unseen (formerly See Invisible)
            // Spiritual Armament (formerly Spiritual Weapon)
            // Stupefy (formerly Touch of Idiocy)
        }
    }
}
