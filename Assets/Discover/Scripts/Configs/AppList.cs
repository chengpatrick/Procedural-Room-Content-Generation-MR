// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;

namespace Discover.Configs
{
    [CreateAssetMenu(menuName = "Discover/App List")]
    public class AppList : ScriptableObject
    {
        public List<AppManifest> AppManifests;

        public AppManifest GetManifestFromName(string appName)
        {
            appName = appName.ToLower();
            foreach (var manifest in AppManifests)
            {
                if (manifest.UniqueName.ToLower() == appName)
                {
                    return manifest;
                }
            }

            return null;
        }
    }
}