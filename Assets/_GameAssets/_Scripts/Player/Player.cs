using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using HLProject.Scriptables;
using HLProject.UI.HUD;
using HLProject.Managers;

namespace HLProject.Characters
{
    public class Player : Character
    {
        [Header("Player")]

        [SerializeField] GameObject playerCamera;
        [SerializeField] GameObject playerCanvasPrefab;
        [SerializeField] protected PlayerMovement movementScript;
        [SerializeField] protected float woundedMaxTime;
        [SerializeField] ClientEffectsController effectsController;
        [SerializeField] protected Transform playerMeshPivot;

        [SyncVar(hook = nameof(OnTeamChange))] protected int playerTeam;
        [SyncVar] protected bool isWounded, firstSpawn;
        [SyncVar] protected string playerName;
        [SyncVar] protected double timeToRespawn;

        public float MaxRespawnTime { get; set; }
        public float BonusWoundTime { get; set; }
        public PlayerCanvas PlayerCanvasScript { get; private set; }
        public PlayerAnimationController AnimController { get; private set; }
        public ClientPlayerModel PlayerModel { get; private set; }

        protected bool onControlPoint;
        protected int currentClassIndex, kills, deaths, revives, score;
        protected double woundedTime;
        protected PlayerInventory inventory;
        protected TeamClassData classData;
        protected PlayerModelData currentModelData;
        protected SkinnedMeshRenderer playerMesh;

        bool connectionCheck_WaitingForServer;
        int connectionCheck_Tries;
        double connectionCheck_Time;
        DummyPlayer controlledDummy;

        AsyncOperationHandle<IList<PlayerModelData>> modelDataArrayHandle;

        #region Hooks

        void OnTeamChange(int oldTeam, int newTeam) => Debug.LogFormat("{0} Team is {1}", playerName, newTeam);

        #endregion

        public override void OnStartServer()
        {
            base.OnStartServer();
            movementScript.freezePlayer = true;
            firstSpawn = true;
            AnimController = GetComponent<PlayerAnimationController>();
        }

        public override void OnStartLocalPlayer()
        {
            PlayerCanvasScript = Instantiate(playerCanvasPrefab).GetComponent<PlayerCanvas>();
            PlayerCanvasScript.Init(this);
            movementScript.FreezeInputs = true;
            GameModeManager.INS.TeamManagerInstance.OnTicketChange += UpdateMatchTickets;

            effectsController.Init(this, movementScript.GetLocalVolumeFromVCam());
            LoadAssets();
        }

        protected virtual void Start()
        {
            if (!isLocalPlayer) Destroy(playerCamera);
            //if (!IsDead && firstSpawn) playerMesh.enabled = false;

            if (!isServer || isLocalPlayer) LoadModelArray();

            inventory = GetComponent<PlayerInventory>();
            inventory.DisablePlayerInputs = true;
        }

        protected override void Update()
        {
            base.Update();

            if (isLocalPlayer) CheckInputs();

            if (!isServer) return;
            PlayerWoundedUpdate();
            ServerHandlePlayerSuppression();
        }

        void OnDisable()
        {
            GameModeManager.INS.TeamManagerInstance.OnTicketChange -= UpdateMatchTickets;
        }

        #region Get/Set

        public string SetPlayerName(string newName) => playerName = newName;
        public string GetPlayerName() => playerName;

        public int GetPlayerTeam() => playerTeam;
        public void SetPlayerTeam(int newPlayerTeam) => playerTeam = newPlayerTeam;

        public bool PlayerIsMoving() => movementScript.PlayerIsMoving;
        public bool PlayerIsRunning() => movementScript.PlayerIsRunning;
        public bool PlayerIsCrouched() => movementScript.PlayerIsCrouched;

        #endregion

        #region Client

        protected void LoadModelArray()
        {
            modelDataArrayHandle = Addressables.LoadAssetsAsync<PlayerModelData>(new List<string>() { "Metrocop PlayerModel" }, null, Addressables.MergeMode.Union);
            modelDataArrayHandle.Completed += OnPlayerModelsLoaded;
        }

        /*[Client]
        public void CheckPlayerConnection()
        {
            if (connectionCheck_WaitingForServer || NetworkTime.localTime < connectionCheck_Time) return;

            Debug.LogFormat("Try {0}", connectionCheck_Tries);

            if (connectionCheck_Tries > 5)
            {
                if (isClient && isServer) GameManager.INS.StopServer();
                else GameManager.INS.DisconnectFromServer();
                return;
            }

            try
            {
                RpcPingPlayerConnection();
            }
            catch (System.Exception e)
            {
                Debug.LogErrorFormat("Error found on Player connection check: {0}", e.Message);
                if (isClient && isServer) GameManager.INS.StopServer();
                else GameManager.INS.DisconnectFromServer();
            }

            connectionCheck_WaitingForServer = true;
            connectionCheck_Tries++;
        }*/

        [Client]
        void OnPlayerModelsLoaded(AsyncOperationHandle<IList<PlayerModelData>> operation)
        {
            if (operation.Status == AsyncOperationStatus.Failed)
                Debug.LogErrorFormat("Couldn't load the player model array for player: {0} - {1}", playerName, operation.OperationException);
        }

        [Client]
        public virtual void ForceClientReload()
        {
            inventory.ForceReload();
        }

        [Client]
        protected virtual void LoadAssets()
        {
            effectsController.LoadAssets();
        }

        [Client]
        protected virtual void OnSoundsLoaded(AsyncOperationHandle<IList<AudioClip>> operation)
        {
            if (operation.Status == AsyncOperationStatus.Failed)
                Debug.LogErrorFormat("Couldn't load Sounds for local player: {0}", operation.OperationException);
        }

        [Client]
        protected virtual void CheckInputs()
        {
            /*if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Escape))
            {
                if (isServer && isClient) GameManager.INS.StopServer();
                else GameManager.INS.DisconnectFromServer();
            }*/

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                GameManager.INS.ToggleMainMenu();
                movementScript.FreezeInputs = !movementScript.FreezeInputs;
                inventory.DisablePlayerInputs = !inventory.DisablePlayerInputs;
                PlayerCanvasScript.ToggleCursor(GameManager.INS.IsMainMenuEnabled);
                return;
            }

            if (GameManager.INS.IsMainMenuEnabled) return;
            if (Input.GetKeyDown(KeyCode.Delete)) GameModeManager.INS.KillPlayer(this);
            if (Input.GetKeyDown(KeyCode.F1)) GameModeManager.INS.EndMatch();
            if (Input.GetKeyDown(KeyCode.F3)) TakeDamage(0, DamageType.Shock);
            if (Input.GetKeyDown(KeyCode.F4)) TakeDamage(0, DamageType.Explosion);
            if (Input.GetKeyDown(KeyCode.F5)) movementScript.spectatorMov = !movementScript.spectatorMov;
            if (Input.GetKeyDown(KeyCode.F6)) OnBulletFlyby(MyTransform.position + MyTransform.right * Random.Range(-1f, 1f));
            if (Input.GetKeyDown(KeyCode.F7)) CmdRequestCommandToDummyPlayer();
            if (Input.GetKeyDown(KeyCode.F8)) CmdRequestMovementOnCommandedDummy();
            if (Input.GetKeyDown(KeyCode.F9)) CmdRequestWeaponCycleDummy();
            if (Input.GetKeyDown(KeyCode.F10)) CmdRequestWeaponShootCycleDummy();

            //if (!PlayerCanvasScript.IsScoreboardMenuOpen && !PlayerCanvasScript.IsScoreboardMenuOpen && !PlayerCanvasScript.IsScoreboardMenuOpen && Input.GetKeyDown(KeyCode.Escape))
            //{
            //    movementScript.FreezeInputs = !movementScript.FreezeInputs;
            //    inventory.DisablePlayerInputs = !inventory.DisablePlayerInputs;
            //}

            if (Input.GetKeyDown(KeyCode.F2))
            {
                movementScript.FreezeInputs = !movementScript.FreezeInputs;
                inventory.DisablePlayerInputs = !inventory.DisablePlayerInputs;
                return;
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (PlayerCanvasScript.IsClassSelectionMenuOpen || PlayerCanvasScript.IsTeamSelectionMenuOpen) return;
                if (PlayerCanvasScript.IsScoreboardMenuOpen)
                {
                    PlayerCanvasScript.ToggleScoreboard(false);
                    return;
                }

                TryGetPlayerInfo();
            }

            if (IsDead || firstSpawn) return;

            if (Input.GetKeyDown(KeyCode.Return))
            {
                if (PlayerCanvasScript.IsScoreboardMenuOpen) return;
                if (PlayerCanvasScript.IsClassSelectionMenuOpen)
                {
                    movementScript.FreezeInputs = false;
                    inventory.DisablePlayerInputs = false;
                    PlayerCanvasScript.ToggleClassSelection(false);
                }
                else
                {
                    movementScript.FreezeInputs = true;
                    inventory.DisablePlayerInputs = true;
                    PlayerCanvasScript.ToggleTeamSelection(false);
                    PlayerCanvasScript.ToggleClassSelection(true);
                }

                return;
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                if (PlayerCanvasScript.IsScoreboardMenuOpen) return;
                if (PlayerCanvasScript.IsTeamSelectionMenuOpen)
                {
                    movementScript.FreezeInputs = false;
                    inventory.DisablePlayerInputs = false;
                    PlayerCanvasScript.ToggleTeamSelection(false);
                }
                else
                {
                    movementScript.FreezeInputs = true;
                    inventory.DisablePlayerInputs = true;
                    PlayerCanvasScript.ToggleClassSelection(false);
                    PlayerCanvasScript.ToggleTeamSelection(true);
                }

                return;
            }
        }

        [Client]
        public void TrySelectTeam(int team)
        {
            if (!isLocalPlayer) return;
            CmdRequestPlayerChangeTeam(team);
        }

        public void TryGetPlayerInfo()
        {
            if (!isLocalPlayer) return;
            CmdRequestPlayerInfo();
            PlayerCanvasScript.ToggleScoreboard(true);
        }

        [Client]
        public void UpdateMatchTickets(int team, int tickets)
        {
            if (isLocalPlayer) PlayerCanvasScript.SetTeamTickets(team - 1, tickets);
            Debug.LogFormat("{0} tickets: {1}", TeamManager.FactionNames[team - 1], tickets);
        }

        [Client]
        public void TrySelectClass(int classIndex)
        {
            if (!isLocalPlayer) return;
            CmdRequestPlayerChangeClass(classIndex);
        }

        [Client]
        public void TryPlayerSpawn()
        {
            if (!isLocalPlayer) return;
            CmdRequestPlayerRespawn();
        }

        [Client]
        public void TryWoundedGiveUp()
        {
            if (!isLocalPlayer) return;
            CmdRequestWoundedGiveUp();
        }

        [Client]
        IEnumerator WaitForPlayerModelSync(string playerModelname)
        {
            WaitForSeconds waitTime = new WaitForSeconds(.1f);
            while (playerMeshPivot.childCount == 1) yield return waitTime;
            PlayerModel = playerMeshPivot.GetComponentInChildren<ClientPlayerModel>();
            PlayerModel.SetIKParent(movementScript.GetFakePivot());
            playerMesh = PlayerModel.ModelMeshRenderer;
            AnimController.SetPlayerModelData(ref currentModelData, PlayerModel.ModelAnimator);

            int size = modelDataArrayHandle.Result.Count;
            for (int i = 0; i < size; i++)
            {
                if (modelDataArrayHandle.Result[i].modelName == playerModelname)
                {
                    currentModelData = modelDataArrayHandle.Result[i];
                    break;
                }
            }

            if (isLocalPlayer) playerMesh.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }

        #endregion

        #region Server

        [Server]
        public virtual void ApplyRecoil(Vector3 ammount) => movementScript.IncreaseCameraRecoil(ammount);

        [Server]
        public virtual void ResetRecoil() => movementScript.ResetCameraRecoil();

        [Server]
        protected virtual void ServerHandlePlayerSuppression()
        {
            if (suppressionAmmount > .45f) movementScript.CameraSensMult = .5f;
        }

        [Server]
        public override void TakeDamage(float ammount, DamageType damageType = DamageType.Base, Vector3 hitDir = default, Vector3 explosionPos = default, float explosionRadius = 0)
        {
            if (isWounded) return;
            base.TakeDamage(ammount, damageType, hitDir, explosionPos, explosionRadius);

            switch (damageType)
            {
                case DamageType.Explosion:
                    movementScript.ShakeCamera(new Vector3(.55f, .55f, 0), 5, .55f);
                    break;
            }
        }

        [Server]
        protected virtual void PlayerWoundedUpdate()
        {
            if (isDead || isInvencible || !isWounded || NetworkTime.time < woundedTime) return;
            isWounded = false;
            isDead = true;

            Transform specPoint = GameModeManager.INS.GetSpectatePointByIndex(0);
            MyTransform.position = specPoint.position;
            MyTransform.rotation = specPoint.rotation;

            NetworkServer.Destroy(PlayerModel.gameObject);
            print("Destroy!");
            AnimController.OnPlayerDies();

            if (connectionToClient != null)
            {
                RpcWoundedCanGiveUp(connectionToClient);
                RpcCharacterDied();
            }
        }

        [Server]
        public void SetAsSpectator()
        {
            isInvencible = true;

            Transform specPoint = GameModeManager.INS.GetSpectatePointByIndex(0);
            MyTransform.position = specPoint.position;
            MyTransform.rotation = specPoint.rotation;

            movementScript.freezePlayer = false;
            movementScript.spectatorMov = true;
            movementScript.FreezeInputs = false;
        }

        [Server]
        public virtual void SpawnPlayer(Vector3 spawnPosition, Quaternion spawnRotation, float spawnTime)
        {
            //print($"NewPos: {MyTransform.position}");
            InitCharacter();
            MaxRespawnTime = spawnTime;
            movementScript.ForceMoveCharacter(spawnPosition, spawnRotation);
            movementScript.freezePlayer = false;
            //movementScript.RpcToggleFreezePlayer(connectionToClient, false);

            currentModelData = classData.playerModels[Random.Range(0, classData.playerModels.Length)];
            PlayerModel = Instantiate(currentModelData.playerModelObject, playerMeshPivot.position, playerMeshPivot.rotation, playerMeshPivot);
            PlayerModel.SetIKParent(movementScript.GetFakePivot());
            AnimController.SetPlayerModelData(ref currentModelData, PlayerModel.ModelAnimator);
            NetworkServer.Spawn(PlayerModel.gameObject);

            Debug.LogFormat("Server: Setup weapon inventory for {0} player - ClassName: {1}", playerName, classData.className);
            inventory.SetupWeaponInventory(classData.classVHands, classData.classWeapons, 0);
            RpcPlayerSpawns(currentModelData.modelName);
        }

        [Server]
        protected override void CharacterDies(bool criticalHit, LastDamageInfo damageInfo)
        {
            if (isDead || isInvencible) return;

            print(criticalHit);
            isBleeding = false;
            timeToRespawn = NetworkTime.time + MaxRespawnTime;
            woundedTime = criticalHit ? 0 : NetworkTime.time + (woundedMaxTime - BonusWoundTime);

            if (PlayerModel != null)
            {
                //PlayerModel.ReleaseRagdoll("", Vector3.forward, 5);
                PlayerModel.ToggleMainModel(false);
                //NetworkServer.Destroy(PlayerModel.gameObject);
            }

            deaths++;
            isWounded = true;
            if (connectionToClient != null) RpcShowWoundedHUD(connectionToClient, woundedTime, timeToRespawn);
            RpcCharacterIsWounded(damageInfo);
            movementScript.freezePlayer = true;
            OnPlayerDead?.Invoke();
            //movementScript.RpcToggleFreezePlayer(connectionToClient, true);
            //base.CharacterDies();
        }

        [Server]
        public void Init()
        {
            GameModeManager.INS.OnMatchEnded += MatchEnded;
        }

        [Server]
        void MatchEnded(int loosingTeam)
        {
            movementScript.freezePlayer = true;
            movementScript.ToggleCharacterController(false);

            Transform spectPoint = GameModeManager.INS.GetSpectatePointByIndex(0);
            movementScript.MyTransform.position = spectPoint.position;
            movementScript.MyTransform.rotation = spectPoint.rotation;

            NetworkServer.Destroy(PlayerModel.gameObject);
            AnimController.OnPlayerDies();

            inventory.HideWeapons();
            RpcMatchEndPlayerSetup(loosingTeam, GameModeManager.INS.timeToChangeLevel);
        }

        [Server]
        public void SetPlayerClass(TeamClassData classData, int classIndex)
        {
            currentClassIndex = classIndex;
            this.classData = classData;
            movementScript.spectatorMov = false;
            Debug.LogFormat("Server: CurrentClass {0}", classData.className);
        }

        [Server]
        public float GetPlayerCameraXAxis() => movementScript.CameraXAxis;

        [Server]
        public PlayerScoreboardInfo GetPlayerScoreboardInfo(int requesterTeam, bool ignoreTeamFilter = false)
        {
            if (ignoreTeamFilter || requesterTeam == playerTeam)
            {
                return new PlayerScoreboardInfo(playerTeam, currentClassIndex, playerName, score, revives, deaths, IsDead, isLocalPlayer);
            }

            return new PlayerScoreboardInfo(playerTeam, playerName, score);
        }

        [Server]
        public void UpdatePlayerScore(int ammount) => score += ammount;

        [Server]
        public void TogglePlayerScopeStatus(bool toggle) => movementScript.ToggleScopeStatus(toggle);

        [Server]
        public void UpdateCurrentWeaponWeight(float weight) => movementScript.GetCurrentWeaponWeight(weight);

        [Server]
        public void TogglePlayerRunAbility(bool toggle) => movementScript.TogglePlayerRunAbility(toggle);

        #endregion

        #region ServerCommands

        /*[Command]
        void RpcPingPlayerConnection(NetworkConnectionToClient playerConnection = null)
        {
            RpcSendConnectionOkToPlayer(playerConnection);
        }*/

        [Command] 
        void CmdRequestCommandToDummyPlayer()
        {
            Transform targetObject = inventory.GetObjectFromPlayerAim();
            if (targetObject == null) return;

            if (controlledDummy != null)
            {
                if (targetObject != controlledDummy.MyTransform)
                {
                    controlledDummy.RpcOnPlayerLostControl(connectionToClient);
                    controlledDummy = null;
                }
                else return;
            }

            HitBox dummyHitBox = targetObject.GetComponent<HitBox>();
            if (dummyHitBox == null) return;

            controlledDummy = dummyHitBox.GetCharacterComponent() as DummyPlayer;
            controlledDummy.RpcOnPlayerControl(connectionToClient);
        }

        [Command]
        void CmdRequestMovementOnCommandedDummy()
        {
            if (controlledDummy == null) return;
            Vector3 targetPos = inventory.GetPositionFromPlayerAim();
            if (targetPos == Vector3.positiveInfinity || targetPos == Vector3.negativeInfinity) return;
            controlledDummy.CommandMoveTo(targetPos);
        }

        [Command]
        void CmdRequestWeaponCycleDummy()
        {
            if (controlledDummy == null) return;
            controlledDummy.CommandToCycleWeapon();
        }

        [Command]
        void CmdRequestWeaponShootCycleDummy()
        {
            if (controlledDummy == null) return;
            controlledDummy.CommandToShoot();
        }

        [Command]
        void CmdRequestPlayerInfo()
        {
            PlayerScoreboardInfo[] info = GameModeManager.INS.GetScoreboardInfo(playerTeam);
            RpcShowScoreboard(connectionToClient, info);
        }

        [Command]
        void CmdRequestPlayerChangeTeam(int team)
        {
            movementScript.freezePlayer = true;
            if (!isDead && !firstSpawn) GameModeManager.INS.KillPlayer(this);
            classData = null;
            GameModeManager.INS.TeamManagerInstance.PlayerSelectedTeam(this, team);
        }

        [Command]
        void CmdRequestPlayerChangeClass(int teamClass)
        {
            GameModeManager.INS.PlayerChangeClass(this, teamClass);
        }

        [Command]
        void CmdRequestPlayerRespawn()
        {
            if ((!isDead && !firstSpawn) || NetworkTime.time < timeToRespawn || classData == null) return;
            GameModeManager.INS.RespawnPlayer(this);
            firstSpawn = false;
        }

        [Command]
        void CmdRequestWoundedGiveUp()
        {
            if (isDead || !isWounded) return;
            woundedTime = 0;
            //isWounded = false;
            //RpcCharacterDied();
            //RpcWoundedCanGiveUp(connectionToClient);
        }

        #endregion

        #region TargetRpc

        /*[TargetRpc]
        void RpcSendConnectionOkToPlayer(NetworkConnection target)
        {
            if (target.connectionId != connectionToServer.connectionId) return;
            connectionCheck_Tries = 0;
            connectionCheck_WaitingForServer = false;
            connectionCheck_Time = NetworkTime.localTime + 2;
            //Debug.LogWarning("Player ping Success");
        }*/

        [TargetRpc]
        public void RpcShowScoreboard(NetworkConnection target, PlayerScoreboardInfo[] scoreboardInfo)
        {
            if (target.connectionId != connectionToServer.connectionId || !PlayerCanvasScript.IsScoreboardMenuOpen) return;
            PlayerCanvasScript.InitScoreboard(scoreboardInfo, playerTeam);
        }

        [TargetRpc]
        void RpcWoundedCanGiveUp(NetworkConnection target)
        {
            if (target.connectionId != connectionToServer.connectionId) return;
            PlayerCanvasScript.PlayerNotWounded();
            PlayerCanvasScript.ToggleClassSelection(true);
        }

        [TargetRpc]
        public void RpcShowGreetings(NetworkConnection target, int team1Tickets, int team2Tickets)
        {
            if (target.connectionId != connectionToServer.connectionId) return;

            PlayerCanvasScript.ToggleTeamSelection(true);
            PlayerCanvasScript.SetTeamTickets(0, team1Tickets);
            PlayerCanvasScript.SetTeamTickets(1, team2Tickets);
        }

        [TargetRpc]
        public void RpcTeamSelectionError(NetworkConnection target, int error)
        {
            if (target.connectionId != connectionToServer.connectionId) return;
            Debug.LogError($"Client: TeamSelection error code: {error}");
        }

        [TargetRpc]
        public void RpcTeamSelectionSuccess(NetworkConnection target, int team)
        {
            if (target.connectionId != connectionToServer.connectionId) return;
            PlayerCanvasScript.ToggleTeamSelection(false);

            if (team > 0)
            {
                PlayerCanvasScript.OnTeamSelection(team);
                PlayerCanvasScript.ToggleClassSelection(true);
            }
        }

        [TargetRpc]
        public void RpcClassSelectionError(NetworkConnection target, int error)
        {
            if (target.connectionId != connectionToServer.connectionId) return;
            Debug.LogError($"Client: ClassSelection error code: {error}");
            PlayerCanvasScript.ToggleSpawnButton(true);
        }

        [TargetRpc]
        public void RpcClassSelectionSuccess(NetworkConnection target, int classIndex)
        {
            if (target.connectionId != connectionToServer.connectionId) return;
            if (isDead || firstSpawn) PlayerCanvasScript.ToggleSpawnButton(true);
            PlayerCanvasScript.OnClassSelection(classIndex);
            print($"Client: CurrentClass {classIndex}");
        }

        [TargetRpc]
        public void RpcOnControlPoint(NetworkConnection target, int currentCPController, float cpCaptureProgress, int defyingTeam)
        {
            if (target.connectionId != connectionToServer.connectionId) return;
            PlayerCanvasScript.OnControlPoint(currentCPController, defyingTeam, cpCaptureProgress);
            onControlPoint = true;
        }

        [TargetRpc]
        public void RpcExitControlPoint(NetworkConnection target)
        {
            if (target.connectionId != connectionToServer.connectionId) return;
            PlayerCanvasScript.OnExitControlPoint();
            onControlPoint = false;
        }

        [TargetRpc]
        public void RpcUpdateCPProgress(NetworkConnection target, float progress)
        {
            if (target.connectionId != connectionToServer.connectionId) return;
            PlayerCanvasScript.UpdateCPProgress(progress);
        }

        [TargetRpc]
        public void RpcPlayerOutOfBounds(NetworkConnection target, float timeToReturn)
        {
            if (target.connectionId != connectionToServer.connectionId) return;
            PlayerCanvasScript.PlayerOutOfBounds(timeToReturn);
        }

        [TargetRpc]
        public void RpcPlayerReturned(NetworkConnection target)
        {
            if (target.connectionId != connectionToServer.connectionId) return;
            PlayerCanvasScript.PlayerInBounds();
        }

        [TargetRpc]
        void RpcShowWoundedHUD(NetworkConnection target, double _woundedTime, double _respawnTime)
        {
            if (target.connectionId != connectionToServer.connectionId) return;
            movementScript.FreezeInputs = true;
            inventory.DisablePlayerInputs = true;
            PlayerCanvasScript.PlayerIsWounded(_woundedTime);
            PlayerCanvasScript.ShowRespawnTimer(_respawnTime);
        }

        [TargetRpc]
        public void RpcPlayerConnected(NetworkConnection target, PlayerScoreboardInfo playerInfo)
        {
            if (target.connectionId != connectionToServer.connectionId) return;
            Debug.Log($"Client: Player {playerInfo.playerName} connected");
            //if (!PlayerCanvasScript.IsScoreboardMenuOpen) return;
            //PlayerCanvasScript.AddPlayerToScoreboard(playerInfo);
        }

        [TargetRpc]
        public void RpcPlayerSelectedTeam(NetworkConnection target, PlayerScoreboardInfo playerInfo)
        {
            if (target.connectionId != connectionToServer.connectionId) return;
            Debug.Log($"Client: Player {playerInfo.playerName} changes his team to {playerInfo.playerTeam}");
            if (!PlayerCanvasScript.IsScoreboardMenuOpen) return;
            PlayerCanvasScript.AddPlayerToScoreboard(playerInfo);
        }

        [TargetRpc]
        public void RpcPlayerDisconnected(NetworkConnection target, string playerName)
        {
            if (target.connectionId != connectionToServer.connectionId) return;
            Debug.Log($"Client: Player {playerName} disconnected");
            PlayerCanvasScript.RemovePlayerFromScoreboard(playerName);
        }

        [TargetRpc]
        protected override void RpcOnBulletFlyBy(NetworkConnection target, Vector3 origin)
        {
            if (target.connectionId != connectionToServer.connectionId) return;
            effectsController.PlayBulletFlyBy(origin);
        }

        #endregion

        #region ClientRpc

        [ClientRpc]
        protected override void RpcCharacterDied()
        {
            if (isLocalPlayer)
            {
                movementScript.FreezeInputs = true;
                inventory.DisablePlayerInputs = true;
                movementScript.ResetCameraParent();
            }

            //playerMesh.enabled = false;
        }

        [ClientRpc]
        public void RpcControlPointCaptured(int cpTeam, int oldTeam, string cpName)
        {
            if (PlayerCanvasScript == null) return;
            if (onControlPoint) PlayerCanvasScript.OnPointCaptured(cpTeam, oldTeam != 0 ? oldTeam : 1);
            PlayerCanvasScript.NewCapturedControlPoint(cpTeam, cpName);
        }

        [ClientRpc]
        public void RpcPlayerSpawns(string playerModelName)
        {
            StartCoroutine(WaitForPlayerModelSync(playerModelName));

            if (isLocalPlayer)
            {
                PlayerCanvasScript.PlayerRespawn();
                PlayerCanvasScript.ToggleWeaponInfo(true);
                movementScript.FreezeInputs = false;
                inventory.DisablePlayerInputs = false;
                //playerMesh.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
        }

        [ClientRpc]
        public void RpcMatchEndPlayerSetup(int loosingTeam, float timeToChangeLevel)
        {
            if (isLocalPlayer)
            {
                PlayerCanvasScript.ShowGameOverScreen(loosingTeam, timeToChangeLevel);
            }

            //playerMesh.enabled = false;
        }

        [ClientRpc]
        protected override void RpcCharacterTookDamage(float ammount, DamageType type)
        {
            base.RpcCharacterTookDamage(ammount, type);
            if (isLocalPlayer) effectsController.OnPlayerTakesDamage(type);
        }

        [ClientRpc]
        protected virtual void RpcCharacterIsWounded(LastDamageInfo damageInfo)
        {
            if (PlayerModel == null) return;
            switch (damageInfo.damageType)
            {
                case DamageType.Explosion:
                    PlayerModel.ExplodeRagdoll(damageInfo.hitOriginalPosition, damageInfo.explosionRadius, damageInfo.damageForce);
                    break;

                default:
                    PlayerModel.ReleaseRagdoll(damageInfo.hitDirection, damageInfo.damageForce);
                    break;
            }

            if (isLocalPlayer) movementScript.LinkCameraToPivot(PlayerModel.GetRagdollHeadPivot());
            PlayerModel.ToggleMainModel(false);
        }

        #endregion
    }
}
