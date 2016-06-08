﻿using Landis.Core;
using Landis.SpatialModeling;
using Landis.Library.Succession;
using Landis.Library.InitialCommunities;
using System.Collections.Generic;
using Edu.Wisc.Forest.Flel.Util;
using System;
using System.Linq;
using Landis.Library.Parameters.Species;


namespace Landis.Extension.Succession.BiomassPnET
{
    /// <summary>
    /// Allocates litters that result from disturbanches. 
    /// Input parameters are fractions of litter that are allocated to different pools
    /// </summary>
    public class Allocation
    {
        // These labels are used as input parameters in the input txt file
        private  static readonly List<string> Disturbances = new List<string>() { "disturbance:fire", "disturbance:wind", "disturbance:bda", "disturbance:harvest" };

        private static readonly List<string> Reductions = new List<string>() { "WoodReduction", "FolReduction", "RootReduction" };

        public static void Initialize(string fn,   SortedDictionary<string, Parameter<string>> parameters)
        {
            Dictionary<string, Parameter<string>> AgeOnlyDisturbancesParameters = PlugIn.LoadTable(Names.AgeOnlyDisturbances, Reductions, Disturbances);
            foreach (KeyValuePair<string, Parameter<string>> parameter in AgeOnlyDisturbancesParameters)
            {
                if (parameters.ContainsKey(parameter.Key)) throw new System.Exception("Parameter " + parameter.Key + " was provided twice");

                foreach (string value in parameter.Value.Values)
                {
                    double v;
                    if (double.TryParse(value, out v) == false) throw new System.Exception("Expecting digit value for " + parameter.Key);

                    if (v > 1 || v < 0) throw new System.Exception("Expecting value for " + parameter.Key + " between 0.0 and 1.0. Found " + v);
                }
            }
            AgeOnlyDisturbancesParameters.ToList().ForEach(x => parameters.Add(x.Key, x.Value));
       
        }


        public static void Allocate(object sitecohorts, Cohort cohort, ExtensionType disturbanceType)
        {
            if (sitecohorts == null)
            {
                throw new System.Exception("sitecohorts should not be null");
            }
            // By default, all material is allocated to the woody debris or the litter pool
            float pwoodlost = 0;
            float prootlost = 0;
            float pfollost = 0;

            Parameter<string> parameter;

            if (disturbanceType != null && PlugIn.TryGetParameter(disturbanceType.Name, out parameter))
            {
                // If parameters are available, then set the loss fractions here.
                if (parameter.ContainsKey("WoodReduction"))
                {
                    pwoodlost = float.Parse(parameter["WoodReduction"]);
                }
                if (parameter.ContainsKey("RootReduction"))
                {
                    prootlost = float.Parse(parameter["RootReduction"]);
                }
                if (parameter.ContainsKey("FolReduction"))
                {
                    pfollost = float.Parse(parameter["FolReduction"]);
                }

            }
            float woodLost = (float)((1 - pwoodlost) * cohort.Wood);
            float rootLost = (float)((1 - prootlost) * cohort.Root);
            float folLost = (float)((1 - pfollost) * cohort.Fol);

            ((SiteCohorts)sitecohorts).AddWoodyDebris(woodLost, cohort.SpeciesPNET.KWdLit);
            ((SiteCohorts)sitecohorts).AddWoodyDebris(rootLost, cohort.SpeciesPNET.KWdLit);
            ((SiteCohorts)sitecohorts).AddLitter(folLost, cohort.SpeciesPNET);

            cohort.AccumulateSenescence((int)(woodLost + rootLost));

        }
    }
}
