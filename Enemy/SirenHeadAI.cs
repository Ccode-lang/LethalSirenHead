using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace LethalSirenHead.Enemy
{
    public class SirenHeadAI : EnemyAI
    {
        enum State
        {
            WANDERING,
            CHASING
        }

        AISearchRoutine wander = new AISearchRoutine();

        PlayerControllerB[] players;

        public override void DoAIInterval()
        {
            base.DoAIInterval();
            // Make sure to set the eye in the prefab or this won't work.
            players = base.GetAllPlayersInLineOfSight(50f, 70, this.eye, 3f, StartOfRound.Instance.collidersRoomDefaultAndFoliage);
            switch (currentBehaviourStateIndex)
            {
                case (int)State.WANDERING:
                    if (!wander.inProgress)
                    {
                        base.StartSearch(base.transform.position, wander);
                    }

                    if (players != null)
                    {
                        base.StopSearch(wander);
                        SwitchToBehaviourClientRpc((int)State.CHASING);
                    }
                    break;
                case (int)State.CHASING:
                    if (players == null)
                    {
                        SwitchToBehaviourClientRpc((int)(State.WANDERING));
                        return;
                    }
                    SetDestinationToPosition(players[0].transform.position);
                    break;
            }
        }

        public override void OnCollideWithPlayer(UnityEngine.Collider other)
        {
            base.OnCollideWithPlayer(other);
            PlayerControllerB player = other.gameObject.GetComponent<PlayerControllerB>();
            if (player != null)
            {
                if (player.isPlayerDead)
                {
                    return;
                }
                if (!player.AllowPlayerDeath())
                {
                    return;
                }
                if (this.IsHost || this.IsServer)
                {
                    StartEatingPlayerClientRpc(player);
                }
                else
                {
                    RequestStartEatingPlayerServerRpc(player);
                }
            }
        }
        [ServerRpc(RequireOwnership = false)]
        public void RequestStartEatingPlayerServerRpc(PlayerControllerB player)
        {
            StartEatingPlayerClientRpc(player);
        }

        [ClientRpc]
        public void StartEatingPlayerClientRpc(PlayerControllerB player)
        {
            this.StartCoroutine(EatPlayer(player));
        }

        public IEnumerator EatPlayer(PlayerControllerB player)
        {
            this.creatureAnimator.SetBool("Eating", true);
            this.inSpecialAnimation = true;
            player.isInElevator = false;
            player.isInHangarShipRoom = false;
            yield return new WaitForSeconds(2.916f);
            this.inSpecialAnimation = false;
            player.KillPlayer(Vector3.zero, false, CauseOfDeath.Crushing, 0);
            base.SwitchToBehaviourState((int)State.WANDERING);
            yield break;
        }
    }
}
