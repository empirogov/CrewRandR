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
 
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using KSP.UI;
using KSP.UI.Screens;
using FingerboxLib;

namespace CrewRandR.Interface
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    class SpaceCenterModule : SceneModule
    {
        private bool astronautComplexSpawned;
        private bool updateLabelOnce;
        private int updateCnt;
        private static int UPDATE_CNT = 5;

        private new void Start()
        {
            Logging.Info("SpaceCenterModule.Start");
            CrewRandRRoster.RestoreVacationingCrew();
        }

        protected override void Awake()
        {
            base.Awake();
            GameEvents.onGUILaunchScreenVesselSelected.Add(onVesselSelected);
            GameEvents.onGUIAstronautComplexSpawn.Add(onGUIAstronautComplexSpawn);
            GameEvents.onGUIAstronautComplexDespawn.Add(onGUIAstronautComplexDespawn);
            GameEvents.onGUILaunchScreenSpawn.Add(onGUILaunchScreenSpawn);
            GameEvents.onGUILaunchScreenDespawn.Add(onGUILaunchScreenDespawn);
            GameEvents.OnCrewmemberHired.Add(onCrewmemberHired);

            updateLabelOnce = true;
            updateCnt = UPDATE_CNT;
        }

        // Doing the update a single time fails since when this is called, it hasn't yet finalized
        // So we do it UPDATE_CNT times and then stop

        protected override void LateUpdate()
        {
            if (astronautComplexSpawned && updateLabelOnce)
            {
                if (updateCnt-- <= 0)
                    updateLabelOnce = false;

                Logging.Debug("AC is spawned...");
                AstronautComplex ac = GameObject.FindObjectOfType<AstronautComplex>();
                if (ac)
                {
                    foreach (var s in ac.ScrollListAvailable)
                    {
                        Logging.Info("");
                        IEnumerable<CrewListItem> crewItemContainers = GameObject.FindObjectsOfType<CrewListItem>().Where(x => x.GetCrewRef().rosterStatus == ProtoCrewMember.RosterStatus.Available);
                        foreach (CrewListItem crewContainer in crewItemContainers)
                        {
                            if (crewContainer.GetCrewRef().VacationExpiry() - Planetarium.GetUniversalTime() > 0)
                            {
                                Logging.Debug("relabeling: " + crewContainer.GetName());
                                string label = "Ready In: " + Utilities.GetFormattedTime(crewContainer.GetCrewRef().VacationExpiry() - Planetarium.GetUniversalTime());
                                crewContainer.SetLabel(label);
                            }
                        }
                    }
                }
            }
        }

        private void onCrewmemberHired(ProtoCrewMember member, int crewCount)
        {
            if (astronautComplexSpawned)
            {
                updateLabelOnce = true;
                updateCnt = UPDATE_CNT;
            }
        }

        private void onGUIAstronautComplexDespawn()
        {
            astronautComplexSpawned = false;
        }

        private void onGUIAstronautComplexSpawn()
        {
            astronautComplexSpawned = true;
            updateLabelOnce = true;
            updateCnt = UPDATE_CNT;
        }

        private void onGUILaunchScreenSpawn(GameEvents.VesselSpawnInfo info)
        {
            CrewRandRRoster.HideVacationingCrew();
        }

        private void onGUILaunchScreenDespawn()
        {
            CrewRandRRoster.RestoreVacationingCrew();
        }

        private void onVesselSelected(ShipTemplate shipTemplate)
        {
            CleanManifest();
            RemapFillButton();
        }
    }
}
