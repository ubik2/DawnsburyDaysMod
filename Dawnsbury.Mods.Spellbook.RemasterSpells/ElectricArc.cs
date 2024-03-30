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

namespace Dawnsbury.Mods.Spellbook.CausticBlast;

public class Cantrips
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        ModManager.RegisterNewSpell("CausticBlast", 0, ((spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
        {
            const int baseRank = 0;
            const int heightenStep = 2;
            int heightenIncrements = (spellLevel - baseRank) / heightenStep;
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
                    DiceFormula diceFormula = DiceFormula.FromText((1 + heightenIncrements).ToString(), "Persistent damage");
                    if (diceFormula != null)
                    {
                        target.AddQEffect(QEffect.PersistentDamage(diceFormula, DamageKind.Acid));
                    }
                }
            }));
        }));

        ModManager.RegisterNewSpell("ElectricArc", 0, ((spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
        {
            const int baseRank = 0;
            const int heightenStep = 2;
            int heightenIncrements = (spellLevel - baseRank) / heightenStep;
            return Spells.CreateModern(IllustrationName.ElectricArc, "Electric Arc", new[] { Trait.Cantrip, Trait.Concentrate, Trait.Electricity, Trait.Manipulate, Trait.Arcane, Trait.Primal },
                "An arc of lightning leaps from one target to another.",
                "Each target takes " + S.HeightenedVariable(2 + heightenIncrements, 2) + "d4 electricity damage with a basic Reflex save." +
                S.HeightenText(spellLevel, 1, inCombat, "{b}Heightened (+" + heightenStep + "){/b} The damage increases by 1d4."),
                Target.MultipleCreatureTargets(Target.Ranged(6), Target.Ranged(6)).WithMinimumTargets(1).WithMustBeDistinct()
            .WithSimultaneousAnimation()
            .WithOverriddenTargetLine("1 or 2 enemies", plural: true), spellLevel, SpellSavingThrow.Basic(Defense.Reflex)).WithSoundEffect(SfxName.ElectricArc).WithGoodnessAgainstEnemy((Target t, Creature a, Creature d) => (float)t.OwnerAction.SpellLevel * 5f)
            .WithEffectOnEachTarget(async delegate (CombatAction spell, Creature caster, Creature target, CheckResult checkResult)
            {
                DiceFormula diceFormula = DiceFormula.FromText((2 + heightenIncrements) + "d4", "Electric Arc");
                await CommonSpellEffects.DealBasicDamage(spell, caster, target, checkResult, diceFormula, DamageKind.Electricity);
            });
        }));
    }
}