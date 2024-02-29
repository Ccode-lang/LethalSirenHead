using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LethalSirenHead.Enemy
{
    public class SirenHeadAnimationCalls : MonoBehaviour
    {
        public void PlayFootstep(float filler)
        {
            gameObject.GetComponentInParent<SirenHeadAI>().PlayFootstep();
        }
    }
}
