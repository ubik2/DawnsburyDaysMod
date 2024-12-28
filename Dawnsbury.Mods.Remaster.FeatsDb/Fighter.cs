using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Core;

namespace Dawnsbury.Mods.Remaster.FeatsDb.TrueFeatsDb
{

    public static class Fighter
    {
        // The following feats are excluded because they aren't useful enough in gameplay
        // * Combat Assessment - players can see the enemy stats
        // * Blade Brake - negligible forced movement from the AI

        // The following feats are not yet implemented, but may be
        // * Sleek Reposition (there are no polearms, and finesse fighters are niche)
        // * Point Blank Stance

        // * Aggressive Block
        // * Brutish Shove
        // * Dueling Parry

        // * Barreling Charge
        // * Dual-Handed Assault
        // * Parting Shot
        // * Powerful Shove
        // * Shielded Stride
        // * Twin Parry

        // The following feats probably won't be implemented due to difficulty
        // * Lightning Swap (also pretty niche)
        // * Quick Reversal - It's a bit tricky to tell when you're flanked and by whom
      
        public static IEnumerable<Feat> LoadAll()
        {
            yield return new TrueFeat(RemasterFeats.FeatName.SlamDown, 4, "You make an attack to knock a foe off balance, then follow up immediately with a sweep to topple them.",
                "Make a melee Strike. If it hits and deals damage, you can attempt an Athletics check to Trip the creature you hit. If you’re wielding a two-handed melee weapon, you can ignore Trip's requirement that you have a hand free. Both attacks count toward your multiple attack penalty, but the penalty doesn't increase until after you've made both of them.",
                 [Trait.Fighter, Trait.Flourish])
                .WithActionCost(2)
                .WithPrerequisite((CalculatedCharacterSheetValues sheet) => sheet.GetProficiency(Trait.Athletics) >= Proficiency.Trained, "You must be trained in Athletics.")
                .WithPermanentQEffect("You make an attack to knock a foe off balance, then follow up immediately with a sweep to topple them.", (QEffect qEffect) =>
                {
                    qEffect.ProvideStrikeModifier = (Item item) =>
                    {
                        CombatAction combatAction = qEffect.Owner.CreateStrike(item).WithActionCost(2);
                        combatAction.Traits.Add(Trait.Flourish);
                        combatAction.Illustration = new SideBySideIllustration(combatAction.Illustration, IllustrationName.Trip);
                        combatAction.Name = "Slam Down";
                        combatAction.Description = StrikeRules.CreateBasicStrikeDescription2(combatAction.StrikeModifiers, null, "You can attempt an Athletics check to Trip the creature you hit.", "You can attempt an Athletics check to Trip the creature you hit.");
                        StrikeModifiers strikeModifiers = combatAction.StrikeModifiers;
                        strikeModifiers.OnEachTarget = (Func<Creature, Creature, CheckResult, Task>)Delegate.Combine(strikeModifiers.OnEachTarget, async (Creature caster, Creature target, CheckResult checkResult) =>
                        {
                            // TODO: also need to check to see if we do damage
                            if (checkResult >= CheckResult.Success)
                            {
                                CombatAction tripAction = CombatManeuverPossibilities.CreateTripAction(caster, item);
                                tripAction.ChosenTargets = new ChosenTargets
                                {
                                    ChosenCreature = target
                                };
                                await tripAction.WithActionCost(0).AllExecute();
                            }
                        });
                        ((CreatureTarget)combatAction.Target).WithAdditionalConditionOnTargetCreature((Creature self, Creature target) => (!self.HasFreeHand && !item.HasTrait(Trait.TwoHanded)) ? Usability.CommonReasons.NoFreeHandForManeuver : Usability.Usable);
                        return combatAction;
                    };
                });

            yield return new TrueFeat(RemasterFeats.FeatName.ViciousSwing, 1, "You unleash a particularly powerful attack that clobbers your foe but leaves you a bit unsteady.",
                "Make a melee Strike. This counts as two attacks when calculating your multiple attack penalty. If this Strike hits, you deal an extra die of weapon damage.",
                [Trait.Fighter, Trait.Flourish])
                .WithActionCost(2)
                .WithPermanentQEffect("You unleash a particularly powerful attack.", (QEffect qEffect) =>
                {
                    qEffect.ProvideStrikeModifier = (Item item) =>
                    {
                        if (!item.HasTrait(Trait.Melee))
                        {
                            return null;
                        }
                        StrikeModifiers strikeModifiers = new StrikeModifiers()
                        {
#if V3
                            AdditionalWeaponDamageDice = 1,
                            OnEachTarget = async (Creature a, Creature d, CheckResult result) =>
                            {
                                if (!item.HasTrait(Trait.TwoHanded) || !a.HasEffect(QEffectId.FuriousFocus))
                                {
                                    a.Actions.AttackedThisManyTimesThisTurn++;
                                }
                            }
#else
                            PowerAttack = true,
                            OnEachTarget = async (Creature a, Creature d, CheckResult result) => ++a.Actions.AttackedThisManyTimesThisTurn
#endif
                        };
                        CombatAction strike = qEffect.Owner.CreateStrike(item, strikeModifiers: strikeModifiers);
                        strike.Name = "Vicious Swing";
                        strike.Illustration = new SideBySideIllustration(strike.Illustration, (Illustration)IllustrationName.StarHit);
                        strike.ActionCost = 2;
                        strike.Traits.Add(Trait.Basic);
                        strike.Traits.Add(Trait.Flourish);
                        return strike;
                    };
                });
        }
    }
}
