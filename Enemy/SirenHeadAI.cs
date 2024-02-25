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
        public enum State
        {
            WANDERING,
            TREEING,
            CHASING
        }

        AISearchRoutine wander = new AISearchRoutine();

        PlayerControllerB[] players;

        PlayerControllerB[] closePlayers;

        State LastState = State.WANDERING;
        public override void Start()
        {
            base.Start();
            SwitchToBehaviourClientRpc((int)State.TREEING);
        }
        public override void DoAIInterval()
        {
            base.DoAIInterval();
            // Make sure to set the eye in the prefab or this won't work.
            players = base.GetAllPlayersInLineOfSight(50f, 70, this.eye, 3f, StartOfRound.Instance.collidersRoomDefaultAndFoliage);

            closePlayers = base.GetAllPlayersInLineOfSight(50f, 20, this.eye, 3f, StartOfRound.Instance.collidersRoomDefaultAndFoliage);

            switch (currentBehaviourStateIndex)
            {
                case (int)State.WANDERING:
                    if (!wander.inProgress)
                    {
                        this.agent.speed = 3.5f;
                        base.StartSearch(base.transform.position, wander);
                    }

                    if (players != null)
                    {
                        base.StopSearch(wander);
                        this.agent.speed = 12f;
                        SwitchToBehaviourClientRpc((int)State.CHASING);
                    }
                    break;
                case (int)State.TREEING:
                    if (LastState != State.TREEING)
                    {
                        this.creatureAnimator.SetBool("Tree", true);
                        this.agent.speed = 0f;
                        this.agent.angularSpeed = 0f;
                    }

                    Plugin.Log.LogInfo(closePlayers);

                    if (closePlayers != null)
                    {
                        this.agent.speed = 12f;
                        if (this.IsHost || this.IsServer)
                        {
                            UntreeClientRpc((int)State.CHASING);
                        }
                        else
                        {
                            RequestUntreeServerRpc((int)State.CHASING);
                        }
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
            LastState = (State)currentBehaviourStateIndex;
        }

        public void LateUpdate()
        {
            if (this.inSpecialAnimationWithPlayer != null)
            {
                SirenHeadVars vars = gameObject.GetComponent<SirenHeadVars>();
                this.inSpecialAnimationWithPlayer.transform.position = new Vector3(vars.holdPlayerPoint.position.x, vars.holdPlayerPoint.position.y - 0.2f, vars.holdPlayerPoint.position.z);
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

        [ServerRpc(RequireOwnership = false)]
        public void RequestUntreeServerRpc(int state)
        {
            UntreeClientRpc(state);
        }
        [ClientRpc]
        public void UntreeClientRpc(int state)
        {
            Plugin.Log.LogInfo("UnTreeing");
            this.StartCoroutine(UntreeAndSwitch((State)state));
        }

        public IEnumerator UntreeAndSwitch(State state)
        {
            this.inSpecialAnimation = true;
            this.creatureAnimator.SetBool("UnTree", true);
            this.creatureAnimator.SetBool("Tree", false);
            yield return new WaitForSeconds(2.5416f);
            this.creatureAnimator.SetBool("UnTree", false);
            base.SwitchToBehaviourClientRpc((int)state);
            this.agent.angularSpeed = 100f;
            this.inSpecialAnimation = false;
            yield break;
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
            PlayerObject.inSpecialInteractAnimation = false;
            PlayerObject.inAnimationWithEnemy = null;
            yield break;
        }
    }
}
