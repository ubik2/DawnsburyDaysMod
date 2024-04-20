using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Roller;
using Dawnsbury.Core;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Dawnsbury.Audio;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Possibilities;

namespace Dawnsbury.Mods.Remaster.Spellbook;

public class Cantrips
{
    public static void RegisterSpells()
    {
        // The following spells are excluded because they aren't useful enough in gameplay
        // * Detect Magic
        // * Figment (previously Ghost Sound)
        // * Know the Way (previously Know Direction)
        // * Light
        // * Message
        // * Prestidigitation
        // * Read Aura
        // * Sigil
        // * Telekinetic Hand (previously Mage Hand)
        // The following spells are from RoE, and I consider them low priority
        // * Slashing Gust
        // * Timber
        ModManager.RegisterNewSpell("CausticBlast", 0, ((spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
        {
            const int heightenStep = 2;
            int heightenIncrements = (spellLevel - 1) / heightenStep;
            return Spells.CreateModern(IllustrationName.AcidSplash, "Caustic Blast", new[] { Trait.Acid, Trait.Cantrip, Trait.Concentrate, Trait.Manipulate, Trait.Arcane, Trait.Primal, RemasterSpells.Trait.Remaster },
                "You fling a large glob of acid that immediately detonates, spraying nearby creatures.",
                "Creatures in the area take " + S.HeightenedVariable(1 + heightenIncrements, 1) + "d8 acid damage with a basic Reflex save; " +
                "on a critical failure, the creature also takes " + S.HeightenedVariable(1 + heightenIncrements, 1) + " persistent acid damage." +
                S.HeightenText(spellLevel, 1, inCombat, "{b}Heightened (+" + heightenStep + "){/b} The initial damage increases by 1d8, and the persistent damage on a critical failure increases by 1."),
                Target.Burst(6, 1), spellLevel, SpellSavingThrow.Basic(Defense.Reflex))
            .WithSoundEffect(ModManager.RegisterNewSoundEffect("AcidicBurstAssets/AcidicBurstSfx.mp3"))
            .WithEffectOnEachTarget((async (spell, caster, target, checkResult) =>
            {
                await CommonSpellEffects.DealBasicDamage(spell, caster, target, checkResult, (1 + heightenIncrements) + "d8", DamageKind.Acid);
                if (checkResult == CheckResult.CriticalFailure)
                {
                    DiceFormula diceFormula = DiceFormula.FromText((1 + heightenIncrements).ToString(), "Caustic Blast persistent damage");
                    if (diceFormula != null)
                    {
                        target.AddQEffect(QEffect.PersistentDamage(diceFormula, DamageKind.Acid));
                    }
                }
            }));
        }));

        ModManager.ReplaceExistingSpell(SpellId.Daze, 0, ((spellcaster, spellLevel, inCombat, spellInformation) =>
        {
            const int heightenStep = 2;
            int heightenIncrements = (spellLevel - 1) / heightenStep;
            return Spells.CreateModern(IllustrationName.Daze, "Daze", new[] { Trait.Cantrip, Trait.Concentrate, Trait.Manipulate, Trait.Mental, Trait.Nonlethal, Trait.Arcane, Trait.Divine, Trait.Occult, RemasterSpells.Trait.Remaster },
                "You push into the target's mind and daze it with a mental jolt.",
                "The jolt deals " + S.HeightenedVariable(1 + heightenIncrements, 1) + "d6 mental damage, with a basic Will save. If the target critically fails the save, it is also stunned 1." +
                S.HeightenText(spellLevel, 1, inCombat, "{b}Heightened (+" + heightenStep + "){/b} The damage increases by 1d6."),
                Target.Ranged(12), spellLevel, SpellSavingThrow.Basic(Defense.Will))
            .WithSoundEffect(SfxName.Mental)
            .WithGoodnessAgainstEnemy((Target t, Creature a, Creature d) => (float)(1 + heightenIncrements) * 3.5f)
            .WithEffectOnEachTarget(async delegate (CombatAction spell, Creature caster, Creature target, CheckResult checkResult)
            {
                await CommonSpellEffects.DealBasicDamage(spell, caster, target, checkResult, (1 + heightenIncrements) + "d6", DamageKind.Mental);
                if (checkResult == CheckResult.CriticalFailure)
                {
                    target.AddQEffect(QEffect.Stunned(1));
                }
            });
        }));

        ModManager.ReplaceExistingSpell(SpellId.DivineLance, 0, ((spellcaster, spellLevel, inCombat, spellInformation) =>
        {
            int heightenIncrements = spellLevel - 1;
            return Spells.CreateModern(IllustrationName.DivineLance, "Divine Lance", new[] { Trait.Attack, Trait.Cantrip, Trait.Concentrate, Trait.Manipulate, RemasterSpells.Trait.Sanctified, RemasterSpells.Trait.Spirit, Trait.Divine, RemasterSpells.Trait.Remaster },
                "You unleash a beam of divine energy.",
                "Make a ranged spell attack against the target's AC. On a hit, the target takes " + S.HeightenedVariable(2 + heightenIncrements, 2) + "d4 spirit damage (double damage on a critical hit)." +
                S.HeightenedDamageIncrease(spellLevel, inCombat, "1d4"),
                Target.Ranged(12), spellLevel, null)
            .WithSpellAttackRoll()
            .WithSoundEffect(SfxName.DivineLance)
            .WithGoodnessAgainstEnemy((Target t, Creature a, Creature d) => (float)(2 + heightenIncrements) * 2.5f)
            .WithEffectOnEachTarget(async delegate (CombatAction spell, Creature caster, Creature target, CheckResult checkResult)
            {
                // For now, just do the better of good or untyped. We don't have core support for Spirit damage type
                DamageKind damageKind = target.WeaknessAndResistance.WhatDamageKindIsBestAgainstMe(new[] { DamageKind.Good, DamageKind.Untyped });
                await CommonSpellEffects.DealAttackRollDamage(spell, caster, target, checkResult, (2 + heightenIncrements) + "d4", damageKind);
            });
        }));

        ModManager.ReplaceExistingSpell(SpellId.ElectricArc, 0, ((spellcaster, spellLevel, inCombat, spellInformation) =>
        {
            int heightenIncrements = spellLevel - 1;
            return Spells.CreateModern(IllustrationName.ElectricArc, "Electric Arc", new[] { Trait.Cantrip, Trait.Concentrate, Trait.Electricity, Trait.Manipulate, Trait.Arcane, Trait.Primal, RemasterSpells.Trait.Remaster },
                "An arc of lightning leaps from one target to another.",
                "Each target takes " + S.HeightenedVariable(2 + heightenIncrements, 2) + "d4 electricity damage with a basic Reflex save." +
                S.HeightenedDamageIncrease(spellLevel, inCombat, "1d4"),
                Target.MultipleCreatureTargets(Target.Ranged(6), Target.Ranged(6)).WithMinimumTargets(1).WithMustBeDistinct()
                    .WithSimultaneousAnimation()
                    .WithOverriddenTargetLine("1 or 2 enemies", plural: true), spellLevel, SpellSavingThrow.Basic(Defense.Reflex))
            .WithSoundEffect(SfxName.ElectricArc).WithGoodnessAgainstEnemy((Target t, Creature a, Creature d) => (float)t.OwnerAction.SpellLevel * 5f)
            .WithEffectOnEachTarget(async delegate (CombatAction spell, Creature caster, Creature target, CheckResult checkResult)
            {
                DiceFormula diceFormula = DiceFormula.FromText((2 + heightenIncrements) + "d4", "Electric Arc");
                await CommonSpellEffects.DealBasicDamage(spell, caster, target, checkResult, diceFormula, DamageKind.Electricity);
            });
        }));

        ModManager.RegisterNewSpell("Frostbite", 0, ((spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
        {
            const int heightenStep = 1;
            int heightenIncrements = spellLevel - 1;
            return Spells.CreateModern(IllustrationName.RayOfFrost, "Frostbite", new[] { Trait.Cantrip, Trait.Concentrate, Trait.Cold, Trait.Manipulate, Trait.Arcane, Trait.Primal, RemasterSpells.Trait.Remaster },
                "An orb of biting cold coalesces around your target, freezing its body.",
                "The target takes " + S.HeightenedVariable(2 + heightenIncrements, 2) + "d4 cold damage with a basic Fortitude save. On a critical failure, the target also gains weakness 1 to bludgeoning until the start of your next turn." +
                S.HeightenText(spellLevel, 1, inCombat, "{b}Heightened (+" + heightenStep + "){/b} The damage increases by 1d4 and the weakness on a critical failure increases by 1."),
                Target.Ranged(12), spellLevel, SpellSavingThrow.Basic(Defense.Fortitude))
            .WithSoundEffect(SfxName.RayOfFrost)
            .WithGoodnessAgainstEnemy((Target t, Creature a, Creature d) => (float)t.OwnerAction.SpellLevel * 5f)
            .WithEffectOnEachTarget(async delegate (CombatAction spell, Creature caster, Creature target, CheckResult checkResult)
            {
                await CommonSpellEffects.DealBasicDamage(spell, caster, target, checkResult, (2 + heightenIncrements) + "d4", DamageKind.Cold);
                if (checkResult == CheckResult.CriticalSuccess)
                {
                    target.AddQEffect(QEffect.DamageWeakness(DamageKind.Bludgeoning, 1 + heightenIncrements).WithExpirationAtStartOfOwnerTurn());
                }
            });
        }));


        ModManager.RegisterNewSpell("GougingClaw", 0, ((spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
        {
            const int heightenStep = 1;
            int heightenIncrements = spellLevel - 1;
            return Spells.CreateModern(IllustrationName.DragonClaws, "Gouging Claw", new[] { Trait.Attack, Trait.Cantrip, Trait.Concentrate, Trait.Manipulate, Trait.Morph, Trait.Arcane, Trait.Primal, RemasterSpells.Trait.Remaster },
                "You temporarily morph your limb into a clawed appendage.",
                "Make a melee spell attack roll against your target's AC. If you hit, you deal your choice of " + S.HeightenedVariable(2 + heightenIncrements, 2) + "d6 slashing damage or " + S.HeightenedVariable(2 + heightenIncrements, 2) + "d6 piercing damage, plus 2 persistent bleed damage. On a critical success, you deal double damage and double bleed damage." +
                S.HeightenText(spellLevel, 1, inCombat, "{b}Heightened (+" + heightenStep + "){/b} The damage increases by 1d6 and the persistent bleed damage increases by 1."),
                Target.AdjacentCreature(), spellLevel, null)
            .WithSpellAttackRoll()
            .WithSoundEffect(SfxName.ZombieAttack)
            .WithGoodnessAgainstEnemy((Target t, Creature a, Creature d) => (float)(2 + heightenIncrements) * 3.5f)
            .WithEffectOnEachTarget(async delegate (CombatAction spell, Creature caster, Creature target, CheckResult checkResult)
            {
                DamageKind damageKind = target.WeaknessAndResistance.WhatDamageKindIsBestAgainstMe(new[] { DamageKind.Slashing, DamageKind.Piercing });
                await CommonSpellEffects.DealAttackRollDamage(spell, caster, target, checkResult, (2 + heightenIncrements) + "d6", damageKind);
                if (checkResult == CheckResult.CriticalSuccess)
                {
                    target.AddQEffect(QEffect.PersistentDamage((2 + heightenIncrements).ToString(), DamageKind.Bleed));
                }
            });
        }));


        ModManager.RegisterNewSpell("Ignition", 0, ((spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
        {
            const int heightenStep = 1;
            int heightenIncrements = spellLevel - 1;
            return Spells.CreateModern(IllustrationName.ProduceFlame, "Ignition", new[] { Trait.Attack, Trait.Cantrip, Trait.Concentrate, Trait.Fire, Trait.Manipulate, Trait.Arcane, Trait.Primal, Trait.VersatileMelee, RemasterSpells.Trait.Remaster },
                "You snap your fingers and point at a target, which begins to smolder.",
                "Make a spell attack roll against the target's AC, dealing " + S.HeightenedVariable(2 + heightenIncrements, 2) + "d4 fire damage on a hit. If the target is within your melee reach, you can choose to make a melee spell attack with the flame instead of a ranged spell attack, which increases all the spell's damage dice to d6s." +
                S.HeightenText(spellLevel, 1, inCombat, "{b}Heightened (+" + heightenStep + "){/b} The initial damage increases by 1d4 and the persistent fire damage on a critical hit increases by 1d4."),
                Target.Ranged(6), spellLevel, null)
            .WithSpellAttackRoll().WithSoundEffect(SfxName.FireRay)
            .WithGoodnessAgainstEnemy((Target t, Creature a, Creature d) => (float)t.OwnerAction.SpellLevel * 5f)
            .WithEffectOnEachTarget(async delegate (CombatAction spell, Creature caster, Creature target, CheckResult checkResult)
            {
                string dieSize = caster.DistanceTo(target) == 1 ? "d6" : "d4";
                await CommonSpellEffects.DealAttackRollDamage(spell, caster, target, checkResult, (2 + heightenIncrements) + dieSize, DamageKind.Fire);
                if (checkResult == CheckResult.CriticalSuccess)
                {
                    target.AddQEffect(QEffect.PersistentDamage(spell.SpellLevel + dieSize, DamageKind.Fire));
                }
            });
        }));

        ModManager.ReplaceExistingSpell(SpellId.TelekineticProjectile, 0, ((spellcaster, spellLevel, inCombat, spellInformation) =>
        {
            bool amped = spellInformation.PsychicAmpInformation?.Amped ?? false;
            int heightenIncrements = spellLevel - 1;
            string ampedEffect = "";
            string criticalSuccessEffect = "You deal double damage.";
            string successEffect = "You deal full damage.";
            string diceExpression = (2 + heightenIncrements) + "d6";
            string damageText = diceExpression;
            CreatureTarget creatureTarget = Target.Ranged(6);
            string heightenedEffect = S.HeightenedDamageIncrease(spellLevel, inCombat, "1d6");

            if (spellInformation.PsychicAmpInformation != null)
            {
                creatureTarget = Target.Ranged(12);
                creatureTarget.OverriddenFullTargetLine = "{b}Range{/b} {Blue}60 feet{/Blue}";
                if (amped)
                {
                    diceExpression = (2 + 2 * heightenIncrements) + "d6";
                    damageText = "{Blue}" + S.HeightenedVariable(2 + 2 * heightenIncrements, 2) + "d6{/Blue}";
                    criticalSuccessEffect = "{Blue}You push the target 10 feet away from you{/Blue} in addition to dealing double damage.";
                    successEffect = "{Blue}You push the target 5 feet away from you{/Blue} in addition to dealing damage.";
                    ampedEffect = "On a success, you push the target 5 feet away from you, and on a critical success, you push the target 10 feet away from you in addition to dealing double damage. ";
                }
                if (!inCombat)
                {
                    heightenedEffect += "\n\n{b}Amp{/b} On a success, you push the target 5 feet away from you, and on a critical success, you push the target 10 feet away from you in addition to dealing double damage.";
                    heightenedEffect += "\n\n{b}Amp Heightened (+1){/b} The damage increases by 2d6 instead of 1d6.";
                }
            }


            CombatAction combatAction =
             Spells.CreateModern(IllustrationName.TelekineticProjectile, "Telekinetic Projectile", new[] { Trait.Attack, Trait.Cantrip, Trait.Concentrate, Trait.Manipulate, Trait.Arcane, Trait.Occult, RemasterSpells.Trait.Remaster },
                "You hurl a loose, unattended object that is within range and that has 1 Bulk or less at the target.",
                "Make a spell attack roll against the target's AC. If you hit, you deal " + damageText + " bludgeoning, piercing, or slashing damage—as appropriate for the object you hurled. " + ampedEffect + "No specific traits or magic properties of the hurled item affect the attack or the damage." +
                S.FourDegreesOfSuccess(criticalSuccessEffect, successEffect, null, null) +
                S.HeightenText(spellLevel, 1, inCombat, heightenedEffect),
                creatureTarget, spellLevel, null)
            .WithSpellAttackRoll().WithSoundEffect(SfxName.PhaseBolt)
            .WithGoodness((Target _, Creature _, Creature _) => 7f)
            .WithEffectOnEachTarget(async delegate (CombatAction spell, Creature caster, Creature target, CheckResult checkResult)
            {
                if (checkResult >= CheckResult.Success)
                {
                    DiceFormula diceFormula = DiceFormula.FromText(diceExpression, "Telekinetic Projectile");
                    DamageKind damageKind = target.WeaknessAndResistance.WhatDamageKindIsBestAgainstMe(new[] { DamageKind.Bludgeoning, DamageKind.Piercing, DamageKind.Slashing });
                    await CommonSpellEffects.DealAttackRollDamage(spell, caster, target, checkResult, diceFormula, damageKind);
                    if (amped)
                    {
                        await caster.PushCreature(target, (checkResult != CheckResult.CriticalSuccess) ? 1 : 2);
                    }
                }
            });

            return AddPsiTraits(combatAction, spellInformation);

        }));

        ModManager.RegisterNewSpell("TangleVine", 0, ((spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
        {
            int duration = spellLevel >= 2 ? 2 : 1;
            string durationText = spellLevel >= 2 ? "2 rounds" : "1 round";
            return Spells.CreateModern(IllustrationName.TelekineticManeuver, "Tangle Vine", new[] { Trait.Attack, Trait.Cantrip, Trait.Concentrate, Trait.Manipulate, Trait.Plant, Trait.Wood, Trait.Arcane, Trait.Primal, Trait.VersatileMelee, RemasterSpells.Trait.Remaster },
                "A vine appears from thin air, flicking from your hand and lashing itself to the target.",
                "Attempt a spell attack roll against the target." +
                S.FourDegreesOfSuccess("The target gains the immobilized condition and takes a –10-foot circumstance penalty to its Speeds for " + durationText + ". It can attempt to Escape against your spell DC to remove the penalty and the immobilized condition.",
                                       "The target takes a –10 foot circumstance penalty to its Speeds for " + durationText + ". It can attempt to Escape against your spell DC to remove the penalty.", null, null) +
                S.HeightenText(spellLevel, 1, inCombat, "{b}Heightened (2nd){/b} The effects last for 2 rounds."),
                Target.Ranged(6), spellLevel, null)
            .WithSpellAttackRoll().WithSoundEffect(SfxName.Trip)
            .WithGoodnessAgainstEnemy((Target t, Creature a, Creature d) => (float)t.OwnerAction.SpellLevel * 5f)
            .WithEffectOnEachTarget(async delegate (CombatAction spell, Creature caster, Creature target, CheckResult checkResult)
            {
                if (checkResult == CheckResult.CriticalSuccess)
                {
                    target.AddQEffect(QEffect.Immobilized().WithExpirationAtStartOfSourcesTurn(caster, duration));
                }
                if (checkResult >= CheckResult.Success)
                {
                    target.AddQEffect(new QEffect("Slowed by vines", "You have a -10-foot-penalty to Speed.")
                    {
                        BonusToAllSpeeds = (QEffect _) => new Bonus(-2, BonusType.Circumstance, "Tangle Vine")

                    }.WithExpirationAtStartOfSourcesTurn(caster, duration));
                }
            });
        }));
        
        ModManager.RegisterNewSpell("VitalityLash", 0, ((spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
        {
            int heightenIncrements = spellLevel - 1;
            // Using Positive instead of Vitality here, since otherwise I'd need to update creatures
            return Spells.CreateModern(IllustrationName.DisruptUndead, "Vitality Lash", new[] { Trait.Cantrip, Trait.Concentrate, Trait.Manipulate, RemasterSpells.Trait.Vitality, Trait.Divine, Trait.Primal, RemasterSpells.Trait.Remaster },
                "You demolish the target's corrupted essence with energy from Creation's Forge.",
                "You deal " + S.HeightenedVariable(2 + heightenIncrements, 2) + "d6 vitality damage with a basic Fortitude save. If the creature critically fails the save, it is also enfeebled 1 until the start of your next turn." +
                S.HeightenedDamageIncrease(spellLevel, inCombat, "1d6"),
                Target.Ranged(6).WithAdditionalConditionOnTargetCreature((Creature _, Creature t) => { return t.HasTrait(Trait.Undead) ? Usability.Usable : Usability.CommonReasons.TargetIsNotUndead; }), 
                spellLevel, SpellSavingThrow.Basic(Defense.Fortitude))
            .WithSoundEffect(SfxName.DivineLance)
            .WithGoodnessAgainstEnemy((Target t, Creature a, Creature d) => (float)(2 + heightenIncrements) * 3.5f)
            .WithEffectOnEachTarget(async delegate (CombatAction spell, Creature caster, Creature target, CheckResult checkResult)
            {
                await CommonSpellEffects.DealBasicDamage(spell, caster, target, checkResult, (2 + heightenIncrements) + "d6", DamageKind.Positive);
                if (checkResult == CheckResult.CriticalFailure)
                {
                    target.AddQEffect(QEffect.Enfeebled(1).WithExpirationAtStartOfSourcesTurn(caster, 0));
                }
            });
        }));

        ModManager.RegisterNewSpell("VoidWarp", 0, ((spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
        {
            int heightenIncrements = spellLevel - 1;
            // Using Negative instead of Void here, since otherwise I'd need to update creatures
            return Spells.CreateModern(IllustrationName.Enervation, "Void Warp", new[] { Trait.Cantrip, Trait.Concentrate, Trait.Manipulate, RemasterSpells.Trait.Void, Trait.Arcane, Trait.Divine, Trait.Occult, RemasterSpells.Trait.Remaster },
                "You call upon the Void to harm life force.",
                "The target takes " + S.HeightenedVariable(2 + heightenIncrements, 2) + "d4 void damage with a basic Fortitude save. On a critical failure, the target is also enfeebled 1 until the start of your next turn." +
                S.HeightenedDamageIncrease(spellLevel, inCombat, "1d4"),
                Target.Ranged(6).WithAdditionalConditionOnTargetCreature((Creature _, Creature t) => { return t.IsLivingCreature ? Usability.Usable : Usability.CommonReasons.TargetIsNotAlive; }),
                spellLevel, SpellSavingThrow.Basic(Defense.Fortitude))
            .WithSoundEffect(SfxName.DivineLance)
            .WithGoodnessAgainstEnemy((Target t, Creature a, Creature d) => (float)(2 + heightenIncrements) * 2.5f)
            .WithEffectOnEachTarget(async delegate (CombatAction spell, Creature caster, Creature target, CheckResult checkResult)
            {
                await CommonSpellEffects.DealBasicDamage(spell, caster, target, checkResult, (2 + heightenIncrements) + "d4", DamageKind.Negative);
                if (checkResult == CheckResult.CriticalFailure)
                {
                    target.AddQEffect(QEffect.Enfeebled(1).WithExpirationAtStartOfSourcesTurn(caster, 0));
                }
            });
        }));
    }

    static CombatAction AddPsiTraits(CombatAction combatAction, SpellInformation spellInformation)
    {
        // Psychic cantrips need to be touched up after creation
        combatAction.PsychicAmpInformation = spellInformation.PsychicAmpInformation;
        if (spellInformation.PsychicAmpInformation != null)
        {
            combatAction.Traits.Add(Trait.Psi);
        }

        return combatAction;
    }


}