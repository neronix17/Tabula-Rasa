﻿using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static RimWorld.QuestGen.QuestNode_GetRandomPawnKindForFaction;

namespace TabulaRasa
{
    [StaticConstructorOnStartup]
    public static class TabulaRasaStartup
    {
        static TabulaRasaStartup()
        {
            FillLinkablesAutomatically();
            FillRaceAlternatesAutomatically();
            DisableCorpseRottingAsNeeded();
        }

        public static void DisableCorpseRottingAsNeeded()
        {
            foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefs)
            {
                DefModExt_ArtificialPawn modExt = thingDef.GetModExtension<DefModExt_ArtificialPawn>();
                if (modExt != null)
                {
                    ThingDef corpseDef = thingDef?.race?.corpseDef;
                    if (corpseDef != null)
                    {
                        if (!modExt.corpseRots)
                        {
                            corpseDef.comps.RemoveAll(compProperties => compProperties is CompProperties_Rottable);
                            corpseDef.comps.RemoveAll(compProperties => compProperties is CompProperties_SpawnerFilth);
                        }
                        if (!modExt.corpseEdible)
                        {
                            if (corpseDef.modExtensions == null)
                            {
                                corpseDef.modExtensions = new List<DefModExtension>();
                            }
                            corpseDef.modExtensions.Add(new DefModExt_ArtificialPawn() { corpseEdible = false });
                        }
                    }
                }
            }
        }

        public static void FillRaceAlternatesAutomatically()
        {
            foreach(RaceSpawningDef rsd in DefDatabase<RaceSpawningDef>.AllDefs)
            {
                if (CheckRaceSpawningDefForFlaws(rsd))
                {
                    DistributeRaceAmongFactionKinds(rsd);
                }
            }
        }

        public static void DistributeRaceAmongFactionKinds(RaceSpawningDef rsd)
        {
            List<PawnKindDef> viableKinds = new List<PawnKindDef>();
            foreach(PawnKindDef pkd in DefDatabase<PawnKindDef>.AllDefs.Where(pk => pk.race.race.Humanlike && pk.defaultFactionType != null))
            {
                if(rsd.factions.Contains(pkd.defaultFactionType))
                {
                    viableKinds.Add(pkd);
                }
            }

            for (int i = 0; i < viableKinds.Count(); i++)
            {
                try
                {
                    LogUtil.LogDebug($"Patching races into {viableKinds[i].defName}...");
                    PawnKindDef curPkd = viableKinds[i];
                    if (!curPkd.HasModExtension<DefModExt_PawnKindRaces>())
                    {
                        if (curPkd.modExtensions.NullOrEmpty())
                        {
                            curPkd.modExtensions = new List<DefModExtension>();
                        }
                        curPkd.modExtensions.Add(new DefModExt_PawnKindRaces());
                    }
                    for (int j = 0; j < rsd.races.Count(); j++)
                    {
                        DefModExt_PawnKindRaces modExt = curPkd.GetModExtension<DefModExt_PawnKindRaces>();
                        if (modExt.altRaces.NullOrEmpty())
                        {
                            modExt.altRaces = new List<WeightedRaceChoice>();
                        }
                        if (!modExt.altRaces.Any(ar => ar.race == rsd.races[j]))
                        {
                            WeightedRaceChoice weightedRace = new WeightedRaceChoice(rsd.races[j], rsd.weight);
                            modExt.altRaces.Add(weightedRace);
                            LogUtil.LogDebug($"- {weightedRace.race} : {weightedRace.weight}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"Exception caught in {viableKinds[i].defName} while trying to distribute races among pawnKinds.\n\n{ex}");
                }
            }
        }

        public static bool CheckRaceSpawningDefForFlaws(RaceSpawningDef rsd)
        {
            if (rsd.races.NullOrEmpty())
            {
                LogUtil.LogWarning($"RaceSpawning Def {rsd.defName} has no races provided, skipping...");
                return false;
            }
            if (rsd.factions.NullOrEmpty())
            {
                LogUtil.LogWarning($"RaceSpawning Def {rsd.defName} has no races provided, skipping...");
                return false;
            }
            return true;
        }

        public static void FillLinkablesAutomatically()
        {
            List<ThingDef> linkableNonFacilities = DefDatabase<ThingDef>.AllDefs.Where(def => def.HasComp(typeof(CompProperties_AffectedByFacilities)) && def.HasModExtension<DefModExt_AutomatedLinkables>()).ToList();

            List<ThingDef> linkableFacilities = DefDatabase<ThingDef>.AllDefs.Where(def => def.HasComp(typeof(CompProperties_Facility)) && def.HasModExtension<DefModExt_AutomatedLinkables>()).ToList();

            for (int i = 0; i < linkableNonFacilities.Count(); i++)
            {
                ThingDef currDef = linkableNonFacilities[i];
                DefModExt_AutomatedLinkables defExt = currDef.GetModExtension<DefModExt_AutomatedLinkables>();
                if (!defExt.linkableTags.NullOrEmpty())
                {
                    for (int j = 0; j < linkableFacilities.Count(); j++)
                    {
                        ThingDef currFac = linkableFacilities[j];
                        DefModExt_AutomatedLinkables facExt = currFac.GetModExtension<DefModExt_AutomatedLinkables>();
                        if (!facExt.linkableTags.NullOrEmpty() && !facExt.linkableTags.Intersect(defExt.linkableTags).EnumerableNullOrEmpty() && !currDef.GetCompProperties<CompProperties_AffectedByFacilities>().linkableFacilities.Contains(currFac))
                        {
                            currDef.GetCompProperties<CompProperties_AffectedByFacilities>().linkableFacilities.Add(currFac);
                        }
                    }
                }
            }
        }
    }
}
