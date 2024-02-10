using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace LethalSirenHead.Enemy
{
    public abstract class SirenHeadAI : EnemyAI
    {
    }

    public class Uitls
    {
        public void Spawn()
        {
            GameObject enemy = UnityEngine.Object.Instantiate(Plugin.Enemy);
            enemy.AddComponent<NetworkObject>();
            enemy.AddComponent<SirenHeadAI>();
            ScanNodeProperties Scan = enemy.transform.Find("ScanNode").gameObject.AddComponent<ScanNodeProperties>();
            Scan.headerText = "Siren Head";
            Scan.subText = "RUN";
        }
    }
}
