/*
 * The MIT License (MIT)
 *
 * Copyright (c) 2015 Alexander Taylor
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using UnityEngine;
using UnityEngine.SceneManagement;
using KSPPluginFramework;
using FingerboxLib;

// Start reading here!
namespace CrewRandR
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class CrewRandR : MonoBehaviourExtended
    {
        // ITS OVER NINE THOUSAND!!!!111
        internal const ProtoCrewMember.RosterStatus ROSTERSTATUS_VACATION = (ProtoCrewMember.RosterStatus)9001;

        // Singleton boilerplate
        private static CrewRandR _Instance;
        public static CrewRandR Instance
        {
            get
            {
                if (_Instance == null)
                {
                    throw new Exception("ERROR: Attempted to access CrewRandR before it was loaded");
                }
                return _Instance;
            }
        }

        // MonoBehaviour Methods
        protected override void Awake()
        {
            DontDestroyOnLoad(this);
            _Instance = this;
            GameEvents.onVesselRecovered.Add(OnVesselRecovered);
            // GameEvents.onLevelWasLoaded.Add(OnLevelWasLoaded);
        }

        // KSP Events
        void OnVesselRecovered(ProtoVessel vessel, bool quick)
        {
            foreach (ProtoCrewMember kerbal in vessel.GetVesselCrew())
            {
                kerbal.SetLastMissionData(vessel.missionTime, Planetarium.GetUniversalTime());
            }
        }



        void OnEnable()
        {
            //Tell our 'OnLevelFinishedLoading' function to start listening for a scene change as soon as this script is enabled.
            SceneManager.sceneLoaded += OnLevelFinishedLoading;
        }

        void OnDisable()
        {
            //Tell our 'OnLevelFinishedLoading' function to stop listening for a scene change as soon as this script is disabled. Remember to always have an unsubscription for every delegate you subscribe to!
            SceneManager.sceneLoaded -= OnLevelFinishedLoading;
        }

        void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
        {
            CrewRandRRoster.RestoreVacationingCrew();
        }

        internal IEnumerable<ProtoCrewMember> GetCrewForPart(Part partPrefab, IEnumerable<ProtoCrewMember> exemptList, bool preferVeterans = false)
        {
            IList<ProtoCrewMember> partCrew = new List<ProtoCrewMember>();
            IEnumerable<ProtoCrewMember> availableCrew = (preferVeterans ? CrewRandRRoster.Instance.MostExperiencedCrew : CrewRandRRoster.Instance.AvailableCrew).Except(exemptList);
            string[] crewCompositionStrings;
            int numToSelect = partPrefab.CrewCapacity;
            Dictionary<string, IEnumerable<ProtoCrewMember>> crewComposition = new Dictionary<string, IEnumerable<ProtoCrewMember>>();

            foreach (ProtoCrewMember crew in availableCrew)
            {
                Logging.Debug(" + " + crew.name);
            }

            Logging.Debug("Part: " + partPrefab.partInfo.name + "   Selecting " + numToSelect + " crew members");

            //Get Crew Composition
            if (partPrefab.Modules.OfType<ModuleCrewRandR>().Any())
            {
                crewCompositionStrings = partPrefab.Modules["ModuleCrewRandR"].Fields.GetValue<string>("crewComposition").Split(',').Select(x => x.Trim()).ToArray();
            }
            else
            {
                crewCompositionStrings = new string[] { "Pilot", "Engineer", "Scientist", "Tourist" };
            }

            Logging.Debug("Using Composition String ... \"" + string.Join(",", crewCompositionStrings) + "\"");

            foreach (string element in crewCompositionStrings)
            {
                crewComposition.Add(element, availableCrew.Where(x => x.experienceTrait.Title == element));
                foreach (var s in crewComposition[element])
                {
                    Logging.Debug("Available crew for: " + element + "     " + s);
                }
            }

            // First Pass
            bool primaryAdded = false;
            Logging.Debug("First pass, numToSelect: " + numToSelect.ToString());
            foreach (string type in crewCompositionStrings)
            {
                Logging.Debug("Selecting: " + type);
                if (numToSelect > 0)
                {
                    if (crewComposition[type].Count() > 0)
                    {
                        partCrew.Add(crewComposition[type].FirstOrDefault());
                        crewComposition[type] = crewComposition[type].Except(partCrew);
                        numToSelect--;
                        primaryAdded = true;
                    }
                }
            }
            if (!primaryAdded)
                return partCrew;
            Logging.Debug("Second pass, numToSelect: " + numToSelect.ToString());

            for (int i = 0; i < numToSelect; i++)
            {
                int numAvail = 0;
                for (int x = 0; x < crewCompositionStrings.Count(); x++)
                    numAvail += crewComposition[crewCompositionStrings[x]].Count();
                if (numAvail == 0)
                    break;
                string type = crewCompositionStrings[new System.Random().Next(crewCompositionStrings.Length)];
                if (crewComposition[type].Count() > 0)
                {
                    partCrew.Add(crewComposition[type].FirstOrDefault());
                    crewComposition[type] = crewComposition[type].Except(partCrew);
                }
                else
                {
                    i--;
                }
            }

            Logging.Debug("Listing Candidates...");
            foreach (ProtoCrewMember crew in partCrew)
            {
                Logging.Debug("Candidate: " + crew.name);
            }

            return partCrew;
        }
    }

    public class ModuleCrewRandR : PartModule
    {
        [KSPField]
        public string crewComposition;
    }
}
