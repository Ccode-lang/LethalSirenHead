using GameNetcodeStuff;
using LethalLib;
using LethalLib.Modules;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

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

        string AIStart = Plugin.AIStart.Value;

        float walkSpeed = Plugin.walkSpeed.Value;

        float runSpeed = Plugin.runSpeed.Value;

        float walkieTimer = 0f;

        float walkieInterval = 0f;

        public override void Start()
        {
            base.Start();
            int rand = Random.Range(0, 2);
            string configValue = Plugin.AIStart.Value;

            if (configValue == "random")
            {
                if (rand == 0)
                {
                    AIStart = "tree";
                }
                else
                {
                    AIStart = "standard";
                }
            }
            else if (configValue == "tree")
            {
                AIStart = "tree";
            }
            if (this.IsHost || this.IsServer)
            {
                walkieInterval = Random.Range(60f, 90f);
                ConfigSyncClientRpc(AIStart, walkSpeed, runSpeed);
            }
        }

        public void PlaySound(AudioClip clip, float volume = 1f)
        {
            this.creatureVoice.PlayOneShot(clip);
            WalkieTalkie.TransmitOneShotAudio(this.creatureVoice, clip);
        }

        public void BroadcastOnWalkie(AudioClip clip, float volume = 1f)
        {
            for (int i = 0; i < WalkieTalkie.allWalkieTalkies.Count; i++)
            {
                if (WalkieTalkie.allWalkieTalkies[i].isBeingUsed)
                {
                    WalkieTalkie.allWalkieTalkies[i].target.PlayOneShot(clip, volume);
                }
            }
        }

        [ClientRpc]
        public void walkieChatterClientRpc()
        {
            BroadcastOnWalkie(Plugin.walkieChatter);
        }

        [ClientRpc]
        public void maketreeClientRpc()
        {
            if (this.IsHost || this.IsServer)
            {
                this.creatureAnimator.SetBool("Tree", true);
                this.agent.speed = 0f;
                this.agent.angularSpeed = 0f;
                SwitchToBehaviourClientRpc((int)State.TREEING);
            }
        }

        [ClientRpc]
        public void makewanderClientRpc()
        {
            this.agent.speed = walkSpeed;
            base.StartSearch(base.transform.position, wander);
            SwitchToBehaviourClientRpc((int)State.WANDERING);
        }

        [ClientRpc]
        public void makechaseClientRpc()
        {
            this.agent.speed = runSpeed;
            PlaySound(Plugin.spotSound);
            SwitchToBehaviourClientRpc((int)State.CHASING);
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
                    if (players != null)
                    {
                        base.StopSearch(wander);
                        makechaseClientRpc();
                    }
                    break;
                case (int)State.TREEING:
                    if (closePlayers != null)
                    {
                        if (this.IsHost || this.IsServer)
                        {
                            UntreeClientRpc();
                        }
                        else
                        {
                            RequestUntreeServerRpc();
                        }
                    }
                    break;
                case (int)State.CHASING:
                    if (players == null)
                    {
                        makewanderClientRpc();
                        return;
                    }
                    SetDestinationToPosition(players[0].transform.position);
                    break;
            }
        }

        public override void Update()
        {
            base.Update();
            if (IsServer || IsHost)
            {
                // Plugin.Log.LogInfo($"{walkieTimer} : {walkieInterval}");
                walkieTimer += Time.deltaTime;
                if (walkieTimer >= walkieInterval)
                {
                    walkieChatterClientRpc();
                    walkieTimer -= walkieInterval;
                    walkieInterval = Random.Range(60f, 90f);
                }
            }

            if (GameNetworkManager.Instance.localPlayerController == null)
            {
                return;
            }
            if (currentBehaviourStateIndex == (int)State.CHASING && players[0] == GameNetworkManager.Instance.localPlayerController)
            {
                GameNetworkManager.Instance.localPlayerController.IncreaseFearLevelOverTime(1.4f, 1f);
                return;
            }
            if (base.HasLineOfSightToPosition(GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position, 45f, 70, -1f))
            {
                if (Vector3.Distance(base.transform.position, GameNetworkManager.Instance.localPlayerController.transform.position) < 15f)
                {
                    GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.7f, true);
                    return;
                }
                GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.4f, true);
            }
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

        public void PlayFootstep()
        {
            PlaySound(Plugin.stepSound, 0.4f);
        }

        [ClientRpc]
        public void ConfigSyncClientRpc(string AIStart, float walkSpeed, float runSpeed)
        {
            this.AIStart = AIStart;
            this.walkSpeed = walkSpeed;
            this.runSpeed = runSpeed;
            if (AIStart == "tree")
            {
                maketreeClientRpc();
            }
            else
            {
                makewanderClientRpc();
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
        public void RequestUntreeServerRpc()
        {
            UntreeClientRpc();
        }
        [ClientRpc]
        public void UntreeClientRpc()
        {
            Plugin.Log.LogInfo("UnTreeing");
            this.StartCoroutine(UntreeAndSwitch());
        }

        public IEnumerator UntreeAndSwitch()
        {
            this.inSpecialAnimation = true;
            if (this.IsHost || this.IsServer)
            {
                this.creatureAnimator.SetBool("UnTree", true);
                this.creatureAnimator.SetBool("Tree", false);
            }
            yield return new WaitForSeconds(2.5416f);
            if (this.IsHost || this.IsServer)
            {
                this.creatureAnimator.SetBool("UnTree", false);
            }
            makechaseClientRpc();
            this.agent.angularSpeed = 100f;
            this.inSpecialAnimation = false;
            yield break;
        }

        public IEnumerator EatPlayer(ulong player)
        {
            PlayerControllerB PlayerObject = StartOfRound.Instance.allPlayerScripts[player];
            if (this.IsHost || this.IsServer)
            {
                this.creatureAnimator.SetBool("Eating", true);
            }
            this.inSpecialAnimation = true;
            PlayerObject.isInElevator = false;
            PlayerObject.isInHangarShipRoom = false;
            yield return new WaitForSeconds(7.29f);
            this.inSpecialAnimation = false;
            PlayerObject.KillPlayer(Vector3.zero, false, CauseOfDeath.Crushing, 0);
            makewanderClientRpc();
            if (this.IsHost || this.IsServer)
            {
                this.creatureAnimator.SetBool("Eating", false);
            }
            this.inSpecialAnimationWithPlayer = null;
            PlayerObject.inSpecialInteractAnimation = false;
            PlayerObject.inAnimationWithEnemy = null;
            yield break;
        }
    }
}
