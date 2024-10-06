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
