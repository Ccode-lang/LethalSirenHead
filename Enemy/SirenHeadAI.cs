using GameNetcodeStuff;
using LethalLib.Modules;
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

        public void LateUpdate()
        {
            if (this.inSpecialAnimationWithPlayer != null)
            {
                SirenHeadVars vars = gameObject.GetComponent<SirenHeadVars>();
                this.inSpecialAnimationWithPlayer.transform.position = vars.holdPlayerPoint.position;
                this.inSpecialAnimationWithPlayer.transform.rotation = Quaternion.Euler(vars.holdPlayerPoint.rotation.x, vars.holdPlayerPoint.rotation.y + 180, vars.holdPlayerPoint.rotation.z);
            }
        }

        public override void OnCollideWithPlayer(UnityEngine.Collider other)
        {
            base.OnCollideWithPlayer(other);
            PlayerControllerB player = other.gameObject.GetComponent<PlayerControllerB>();
            this.inSpecialAnimationWithPlayer = player;
            this.inSpecialAnimationWithPlayer.inSpecialInteractAnimation = true;
            this.inSpecialAnimationWithPlayer.inAnimationWithEnemy = this;
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
                    StartEatingPlayerClientRpc(player.playerClientId);
                }
                else
                {
                    RequestStartEatingPlayerServerRpc(player.playerClientId);
                }
            }
        }
        [ServerRpc(RequireOwnership = false)]
        public void RequestStartEatingPlayerServerRpc(ulong player)
        {
            StartEatingPlayerClientRpc(player);
        }

        [ClientRpc]
        public void StartEatingPlayerClientRpc(ulong player)
        {
            this.StartCoroutine(EatPlayer(player));
        }

        public IEnumerator EatPlayer(ulong player)
        {
            PlayerControllerB PlayerObject = StartOfRound.Instance.allPlayerScripts[player];
            this.creatureAnimator.SetBool("Eating", true);
            this.inSpecialAnimation = true;
            PlayerObject.isInElevator = false;
            PlayerObject.isInHangarShipRoom = false;
            yield return new WaitForSeconds(7.29f);
            this.inSpecialAnimation = false;
            PlayerObject.KillPlayer(Vector3.zero, false, CauseOfDeath.Crushing, 0);
            base.SwitchToBehaviourState((int)State.WANDERING);
            this.creatureAnimator.SetBool("Eating", false);
            this.inSpecialAnimationWithPlayer = null;
            yield break;
        }
    }
}
