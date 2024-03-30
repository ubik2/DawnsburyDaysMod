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

namespace Dawnsbury.Mods.Spellbook.RemasterSpells;

public class Cantrips
{
    public static void RegisterSpells() { 
        ModManager.RegisterNewSpell("CausticBlast", 0, ((spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
        {
            const int heightenStep = 2;
            int heightenIncrements = (spellLevel - 1) / heightenStep;
            return Spells.CreateModern(IllustrationName.AcidSplash, "Caustic Blast", new[] { Trait.Acid, Trait.Cantrip, Trait.Concentrate, Trait.Manipulate, Trait.Arcane, Trait.Primal },
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

        ModManager.ReplaceExistingSpell(SpellId.ElectricArc, 0, ((spellcaster, spellLevel, inCombat, spellInformation) =>
        {
            const int heightenStep = 1;
            int heightenIncrements = spellLevel - 1;
            return Spells.CreateModern(IllustrationName.ElectricArc, "Electric Arc", new[] { Trait.Cantrip, Trait.Concentrate, Trait.Electricity, Trait.Manipulate, Trait.Arcane, Trait.Primal },
                "An arc of lightning leaps from one target to another.",
                "Each target takes " + S.HeightenedVariable(2 + heightenIncrements, 2) + "d4 electricity damage with a basic Reflex save." +
                S.HeightenText(spellLevel, 1, inCombat, "{b}Heightened (+" + heightenStep + "){/b} The damage increases by 1d4."),
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

        ModManager.ReplaceExistingSpell(SpellId.TelekineticProjectile, 0, ((spellcaster, spellLevel, inCombat, spellInformation) =>
        {
            // FIXME: Handle Amped
            const int heightenStep = 1;
            int heightenIncrements = spellLevel - 1;
            return Spells.CreateModern(IllustrationName.TelekineticProjectile, "Telekinetic Projectile", new[] { Trait.Attack, Trait.Cantrip, Trait.Concentrate, Trait.Manipulate, Trait.Arcane, Trait.Occult },
                "You hurl a loose, unattended object that is within range and that has 1 Bulk or less at the target.",
                "Make a spell attack roll against the target's AC. If you hit, you deal 2d6 bludgeoning, piercing, or slashing damage—as appropriate for the object you hurled. No specific traits or magic properties of the hurled item affect the attack or the damage." +
                S.FourDegreesOfSuccess("You deal double damage.", "You deal full damage.", null, null) +
                S.HeightenText(spellLevel, 1, inCombat, "{b}Heightened (+" + heightenStep + "){/b} The damage increases by 1d6."),
                Target.Ranged(6), spellLevel, null)
            .WithSpellAttackRoll().WithSoundEffect(SfxName.PhaseBolt)
            .WithGoodness((Target _, Creature _, Creature _) => 7f)
            .WithEffectOnEachTarget(async delegate (CombatAction spell, Creature caster, Creature target, CheckResult checkResult)
            {
                if (checkResult >= CheckResult.Success)
                    {
                        DiceFormula diceFormula = DiceFormula.FromText((2 + heightenIncrements) + "d6", "Telekinetic Projectile");
                        DamageKind damageKind = target.WeaknessAndResistance.WhatDamageKindIsBestAgainstMe(new[] { DamageKind.Bludgeoning, DamageKind.Piercing, DamageKind.Slashing });
                        await CommonSpellEffects.DealAttackRollDamage(spell, caster, target, checkResult, diceFormula, damageKind);
                    //if (amped)
                    //{
                    //    await a.PushCreature(d, (checkResult != CheckResult.CriticalSuccess) ? 1 : 2);
                    //}
                }
            });

        }));
    }
}