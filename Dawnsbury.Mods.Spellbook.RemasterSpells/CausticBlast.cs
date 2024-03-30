using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Roller;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using System.Threading.Tasks;

namespace Dawnsbury.Mods.Spellbook.CausticBlast;

public class CausticBlast
{
    public static string HeightenedDamageIncrease(int level, bool inCombat, int heightenStep, string diceExpression, string persistentDiceExpression)
    {
        return S.HeightenText(level, 1, inCombat, "{b}Heightened (+" + heightenStep + "){/b} The initial damage increases by " + diceExpression + ", and the persistent damage on a critical failure increases by " + persistentDiceExpression + ".");
    }

    public async static Task DealPersistentDamage(Creature target, string diceExpression, DamageKind damageKind)
    {
        DiceFormula diceFormula = DiceFormula.FromText(diceExpression, "Persistent damage");
        if (diceFormula != null)
        {
            target.AddQEffect(QEffect.PersistentDamage(diceFormula, damageKind));
        }
    }

    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        ModManager.RegisterNewSpell("CausticBlast", 0, ((spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
        {
            const int baseRank = 0;
            const int heightenStep = 2;
            int heightenIncrements = (spellLevel - baseRank) / heightenStep;
            return Spells.CreateModern(new ModdedIllustration("AcidicBurstAssets/AcidicBurst.png"), "Caustic Blast", new[] { Trait.Acid, Trait.Cantrip, Trait.Concentrate, Trait.Manipulate, Trait.Arcane, Trait.Primal },
                    "You fling a large glob of acid that immediately detonates, spraying nearby creatures.",
                    "Creatures in the area take " + S.HeightenedVariable(1 + heightenIncrements, 1) + "d8 acid damage with a basic Reflex save; " + 
                    "on a critical failure, the creature also takes " + S.HeightenedVariable(1 + heightenIncrements, 1) + " persistent acid damage." +
                     HeightenedDamageIncrease(spellLevel, inCombat, heightenStep, "1d8", "1"),
                    Target.Burst(6, 1), spellLevel, SpellSavingThrow.Basic(Defense.Reflex))
                .WithSoundEffect(ModManager.RegisterNewSoundEffect("AcidicBurstAssets/AcidicBurstSfx.mp3"))
                .WithEffectOnEachTarget((async (spell, caster, target, result) =>
                {
                    await CommonSpellEffects.DealBasicDamage(spell, caster, target, result, 1 + heightenIncrements + "d8", DamageKind.Acid);
                    if (result == CheckResult.CriticalFailure)
                    {
                        await CausticBlast.DealPersistentDamage(target, (1 + heightenIncrements) + "", DamageKind.Acid);
                    }
                }));
        }));
    }
}