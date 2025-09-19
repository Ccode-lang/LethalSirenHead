using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
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

        public Transform headPos;

        ulong playerIdOfCaughtPlayer = 10000;

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

        // Borrowing from Zeekerss for v49 compatability
        public PlayerControllerB[] CheckLineOfSightForPositionCompat(float width = 45f, int range = 60, Transform eyeObject = null, float proximityCheck = -1f, int layerMask = -1)
        {
            if (layerMask == -1)
            {
                layerMask = StartOfRound.Instance.collidersAndRoomMaskAndDefault;
            }
            if (eyeObject == null)
            {
                eyeObject = this.eye;
            }
            if (this.isOutside && !this.enemyType.canSeeThroughFog && TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Foggy)
            {
                range = Mathf.Clamp(range, 0, 30);
            }
            List<PlayerControllerB> list = new List<PlayerControllerB>(4);
            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                if (this.PlayerIsTargetable(StartOfRound.Instance.allPlayerScripts[i], false, false))
                {
                    Vector3 position = StartOfRound.Instance.allPlayerScripts[i].gameplayCamera.transform.position;
                    if (Vector3.Distance(this.eye.position, position) < (float)range && !Physics.Linecast(eyeObject.position, position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
                    {
                        Vector3 to = position - eyeObject.position;
                        if (Vector3.Angle(eyeObject.forward, to) < width || Vector3.Distance(base.transform.position, StartOfRound.Instance.allPlayerScripts[i].transform.position) < proximityCheck)
                        {
                            list.Add(StartOfRound.Instance.allPlayerScripts[i]);
                        }
                    }
                }
            }
            if (list.Count == 4)
            {
                return StartOfRound.Instance.allPlayerScripts;
            }
            if (list.Count > 0)
            {
                return list.ToArray();
            }
            return null;
        }

        public bool LineOfSightForPositionCompat(Vector3 objectPosition, float width = 45f, int range = 60, float proximityAwareness = -1f, Transform overrideEye = null)
        {
            if (!this.isOutside)
            {
                if (objectPosition.y > -80f)
                {
                    return false;
                }
            }
            else if (objectPosition.y < -100f)
            {
                return false;
            }
            Transform transform;
            if (overrideEye != null)
            {
                transform = overrideEye;
            }
            else if (this.eye == null)
            {
                transform = base.transform;
            }
            else
            {
                transform = this.eye;
            }
            RaycastHit raycastHit;
            if (Vector3.Distance(transform.position, objectPosition) < (float)range && !Physics.Linecast(transform.position, objectPosition, out raycastHit, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
            {
                Vector3 to = objectPosition - transform.position;
                if (this.debugEnemyAI)
                {
                    Debug.DrawRay(transform.position, objectPosition - transform.position, Color.green, 2f);
                }
                if (Vector3.Angle(transform.forward, to) < width || Vector3.Distance(base.transform.position, objectPosition) < proximityAwareness)
                {
                    return true;
                }
            }
            return false;
        }

        // End of borrowed functions

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
        public void playSpotOneshotClientRpc(int index)
        {
            PlaySound(Plugin.spotSound[index]);
        }


        [ClientRpc]
        public void playWalkOneshotClientRpc(int index)
        {
            PlaySound(Plugin.stepSound[index], 0.4f);
        }

        [ClientRpc]
        public void walkieChatterClientRpc(int index)
        {
            BroadcastOnWalkie(Plugin.walkieChatter[index], 0.5f);
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
            playSpotOneshotClientRpc(Random.Range(0, Plugin.spotSound.Length));
            SwitchToBehaviourClientRpc((int)State.CHASING);
        }
        public override void DoAIInterval()
        {
            base.DoAIInterval();
            // Make sure to set the eye in the prefab or this won't work.
            players = CheckLineOfSightForPositionCompat(50f, 70, this.eye, 15f, StartOfRound.Instance.collidersRoomDefaultAndFoliage);

            closePlayers = CheckLineOfSightForPositionCompat(50f, 20, this.eye, 10f, StartOfRound.Instance.collidersRoomDefaultAndFoliage);

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
                    walkieChatterClientRpc(Random.Range(0, Plugin.walkieChatter.Length));
                    walkieTimer -= walkieInterval;
                    walkieInterval = Random.Range(60f, 90f);
                }
            }

            if (GameNetworkManager.Instance.localPlayerController == null)
            {
                return;
            }
            if (players != null)
            {
                if (currentBehaviourStateIndex == (int)State.CHASING && players[0] == GameNetworkManager.Instance.localPlayerController)
                {
                    GameNetworkManager.Instance.localPlayerController.IncreaseFearLevelOverTime(1.4f, 1f);
                    return;
                }
            }
            if (LineOfSightForPositionCompat(GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position, 45f, 70, -1f))
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
                this.inSpecialAnimationWithPlayer.transform.LookAt(headPos);
            }
            if (GameNetworkManager.Instance.localPlayerController.playerClientId == playerIdOfCaughtPlayer)
            {
                GameNetworkManager.Instance.localPlayerController.thisPlayerModelArms.enabled = false;
                GameNetworkManager.Instance.localPlayerController.localVisor.gameObject.GetComponentsInChildren<MeshRenderer>()[0].enabled = false;
                GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.LookAt(headPos);
            }
            else
            {
                GameNetworkManager.Instance.localPlayerController.localVisor.gameObject.GetComponentsInChildren<MeshRenderer>()[0].enabled = true;
            }
        }

        public override void OnCollideWithPlayer(UnityEngine.Collider other)
        {
            base.OnCollideWithPlayer(other);

            if (StartOfRound.Instance.shipIsLeaving)
            {
                return;
            }

            PlayerControllerB player = other.gameObject.GetComponent<PlayerControllerB>();
            if (this.inSpecialAnimationWithPlayer != null) {
                if (player.playerClientId == this.inSpecialAnimationWithPlayer.playerClientId)
                {
                    return;
                }
            }

            if (player.AllowPlayerDeath()) {
                this.inSpecialAnimationWithPlayer = player;
                this.inSpecialAnimationWithPlayer.inSpecialInteractAnimation = true;
                this.inSpecialAnimationWithPlayer.inAnimationWithEnemy = this;
            }

            if (player != null)
            {
                if (player.isPlayerDead)
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

        [ClientRpc]
        public void UpdatePlayerIdOfCaughtPlayerClientRpc(ulong id)
        {
            Plugin.Log.LogInfo(id);
            playerIdOfCaughtPlayer = id;
        }

        public void PlayFootstep()
        {
            if (this.IsHost || this.IsServer)
            {
                playWalkOneshotClientRpc(Random.Range(0, Plugin.stepSound.Length));
            }
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

        [ClientRpc]
        public void startThePartyClientRpc()
        {
            PlaySound(Plugin.OhMyGodIts);
        }

        public IEnumerator EatPlayer(ulong player)
        {
            PlayerControllerB PlayerObject = StartOfRound.Instance.allPlayerScripts[player];

            if (PlayerObject.AllowPlayerDeath()) {
                UpdatePlayerIdOfCaughtPlayerClientRpc(PlayerObject.playerClientId);
            }

            if (this.IsHost || this.IsServer)
            {
                this.creatureAnimator.SetBool("Eating", true);
                try
                {
                    //Plugin.Log.LogInfo(PlayerObject.currentlyHeldObjectServer.gameObject.GetComponent<MeshFilter>().sharedMesh.name);
                    if (PlayerObject.currentlyHeldObjectServer.GetType().Name == "NoisemakerProp" && PlayerObject.currentlyHeldObjectServer.gameObject.GetComponent<MeshFilter>().sharedMesh.name == "Airhorn" && UnityEngine.Random.Range(0, 9) == 0)
                    {
                        startThePartyClientRpc();
                    }
                }
                catch
                {
                    ; //e
                }
            }

            this.inSpecialAnimation = true;

            if (PlayerObject.AllowPlayerDeath())
            {
                PlayerObject.isInElevator = false;
                PlayerObject.isInHangarShipRoom = false;
            }

            yield return new WaitForSeconds(5f);
            this.inSpecialAnimation = false;

            if (PlayerObject.AllowPlayerDeath())
            {
                PlayerObject.KillPlayer(Vector3.zero, false, CauseOfDeath.Crushing, 0);
                if (CoronerCompatibility.enabled)
                {
                    CoronerCompatibility.CoronerRegister();
                    CoronerCompatibility.CoronerSetCauseOfDeathSirenHead(PlayerObject);
                }
            }
            // this number is big because of lobby number mods.
            UpdatePlayerIdOfCaughtPlayerClientRpc(10000);
            makewanderClientRpc();
            if (this.IsHost || this.IsServer)
            {
                this.creatureAnimator.SetBool("Eating", false);
            }
            this.inSpecialAnimationWithPlayer = null;

            if (PlayerObject.AllowPlayerDeath())
            {
                PlayerObject.inSpecialInteractAnimation = false;
                PlayerObject.inAnimationWithEnemy = null;
            }
            
            yield break;
        }
    }
}
