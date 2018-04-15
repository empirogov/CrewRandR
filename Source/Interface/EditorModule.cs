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
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using KSP.UI.Screens;
using KSPPluginFramework;
using FingerboxLib;

namespace CrewRandR.Interface
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class EditorModule : SceneModule
    {
        bool rootNeedsCleaning = false;

        // Monobehaviour Methods
        protected override void Awake()
        {
            base.Awake();
            GameEvents.onEditorPodPicked.Add(OnEditorPodPicked);
            GameEvents.onEditorLoad.Add(OnEditorLoad);
            GameEvents.onEditorScreenChange.Add(OnEditorScreenChanged);
        }
#if false
        protected override void OnDestroy()
        {
            CrewRandRRoster.RestoreVacationingCrew();
            base.OnDestroy();
        }
#endif
        // KSP Events
        protected void OnEditorPodPicked(Part part)
        {
            // There's now a root part, and it needs to be cleaned.
            rootNeedsCleaning = true;
        }

        protected void OnEditorLoad(ShipConstruct ship, CraftBrowserDialog.LoadType loadType)
        {
            if (loadType == CraftBrowserDialog.LoadType.Normal)
            {
                // Loading a new ship design replaces the root part, and the new
                // root hasn't been cleaned even if the old one had been.
                rootNeedsCleaning = true;
            }
        }

        protected void OnEditorScreenChanged(EditorScreen screen)
        {
            if (screen == EditorScreen.Crew)
            {
                RemapFillButton();
                CrewRandRRoster.HideVacationingCrew();
            }
            else
            {
                CrewRandRRoster.RestoreVacationingCrew();
            }
        }

        // Our methods
        protected override void Update()
        {
            if (CrewRandRSettings.Instance != null && CrewRandRSettings.Instance.AssignCrews)
            {
                try
                {
                    if (rootNeedsCleaning)
                    {
                        CleanManifest();
                        rootNeedsCleaning = false;
                    }
                }
                catch (Exception)
                {
                    // No worries!
                    Logging.Debug("If there is a problem with clearing the roster, look here.");
                }
            }
        }
    }
}