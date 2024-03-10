using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LethalSirenHead
{
    internal class Utils
    {
        public static AudioClip[] LoadSounds(AssetBundle bundle, string prefix)
        {
            List<AudioClip> clips = new List<AudioClip>();
            foreach (string name in bundle.GetAllAssetNames())
            {
                if (name.Contains(prefix))
                {
                    clips.Add(bundle.LoadAsset<AudioClip>(name));
                }
            }
            return clips.ToArray();
        }
    }
}
