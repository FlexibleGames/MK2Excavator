using System.Collections.Generic;
using System.Text;
using System.IO;
using UnityEngine;

public class Mk2Excavator : MachineEntity, PowerConsumerInterface
{
    public enum ExcavateState
    {
        ClearGarbage,
        ClearOre,
        ClearAll
    }

    public enum DropState
    {
        DropSome,
        DropNone,
        DropOre,
        DropAll
    }

    public float mrMaxPower = 1280f;
    public float mrCurrentPower;
    public float mrPowerRate = 20f;

    public int mModVersion = 10;
    private string PopUpText;
    public static bool AllowMovement = true;
    public bool mbDoDigOre;
    public float mrPowerRateOre = 80f;
    public float mrPowerRateDefault = 20f;
    public Mk2Excavator.ExcavateState eExcavateState;
    public Mk2Excavator.DropState eDropState;
    public float mfCommandDebounce;
    public Color mCubeColor;
    private GameObject mExcavatorBase;
    private GameObject mExcavatorTurret;
    public bool mbDoDropBlocks;

    public int mnDigSizeX;

    public int mnDigSizeY;

    public int mnDigSizeZ;

    private int mnCurrentDigSizeX;

    private int mnCurrentDigSizeY;

    private int mnCurrentDigSizeZ;

    public float PercentScanned;

    private bool mbLinkedToGO;

    public bool mbWorkComplete;

    public bool mbLocatedBlock;

    private float mrDigDelay;

    private GameObject mPreviewCube;

    private GameObject mCurrentCube;

    private GameObject mTurretObject; // no MR
    private GameObject mGunObject;
    private GameObject mBarrelObject;
    private GameObject mLaserObject; // No MR

    public float mrTimeSinceShoot;

    public int mnBlocksDestroyed;

    private float mrOutOfPowerTime;

    private System.Random mRand;

    private int mnTotalBlocksScanned;

    private long mnLastDigX;

    private long mnLastDigY;

    private long mnLastDigZ;

    private ushort mReplaceType = 1;

    private DigArea digArea;
    private IEnumerator<CubeCoord> digAreaEnumerator;
    private CubeCoord? currentCube;
    private CubeCoord origin;
    private byte machineFlags;
    public bool superOPflag;
    public int mutePews;
    private int powerDefaultBackup;
    private int powerOreBackup;

    public Mk2Excavator(Segment segment, long x, long y, long z, ushort cube, byte flags, ushort lValue,
        bool lbFromDisk, int powerDefault, int powerOre, int digRadius, int digHeight, int maxPower)
        : base(eSegmentEntity.Mod, SpawnableObjectEnum.AutoExcavator, x, y, z, cube, flags, lValue, Vector3.zero,
            segment)
    {
        this.mrPowerRate = powerDefault;
        this.mrPowerRateDefault = this.mrPowerRate;
        this.powerDefaultBackup = powerDefault;
        this.mrPowerRateOre = powerOre;
        this.powerOreBackup = powerOre;
        this.mbNeedsLowFrequencyUpdate = true;                
        this.mbNeedsUnityUpdate = true;
        this.mbWorkComplete = false;

        this.mnDigSizeX = digRadius; // 9;
        this.mnDigSizeY = digHeight; // 128;
        this.mnDigSizeZ = digRadius; // 9;
        this.mnCurrentDigSizeX = digRadius; // 9;
        this.mnCurrentDigSizeY = 1;
        this.mnCurrentDigSizeZ = digRadius; // 9;
        this.mRand = new System.Random();
        this.mbLocatedBlock = false;
        this.mbDoDigOre = false;
        this.eExcavateState = ExcavateState.ClearGarbage;
        this.eDropState = DropState.DropSome;
        this.mfCommandDebounce = 0.02f;
        this.mCubeColor = Color.blue;
        this.mbDoDropBlocks = true;
        this.mrMaxPower = maxPower;
        this.mutePews = 0;
        // New DigArea object should go in here
        machineFlags = flags;
        origin = new CubeCoord(x, y, z);
        digArea = new DigArea(origin, 0, digRadius, digHeight, machineFlags);

    }

    public override bool ShouldSave()
    {
        return true;
    }

    public override int GetVersion()
    {
        return this.mModVersion;
    }

    public override void Write(BinaryWriter writer)
    {
        float value = 0f;

        writer.Write(this.mModVersion); // mod version
        writer.Write((int) this.eExcavateState); // Excavate state
        writer.Write((int) this.eDropState); // drop state
        writer.Write(this.mnDigSizeX); // current radius
        writer.Write(this.mnDigSizeY); // max dig height
        writer.Write(this.mnCurrentDigSizeY); // current dig height

        int OPFlag = superOPflag ? 1 : 0;

        // version 10
        writer.Write(mutePews);
        writer.Write(OPFlag);

        writer.Write(value);
        writer.Write(value);
        // 10 32bit writes
    }

    public override void Read(BinaryReader reader, int entityVersion)
    {
        int modversion = 0;
        modversion = reader.ReadInt32();

        if (modversion >= 6)
        {
            if (modversion <= 9)
            {
                GameManager.DoLocalChat("Mk2Excavator version updated. Please replace all placed Mk2Excavators.");
            }

            // versions match, load normally
            this.eExcavateState = (ExcavateState) reader.ReadInt32();
            this.eDropState = (DropState) reader.ReadInt32();
            this.mnDigSizeX = reader.ReadInt32();
            this.mnDigSizeZ = mnDigSizeX;
            this.mnDigSizeY = reader.ReadInt32();
            this.mnCurrentDigSizeY = reader.ReadInt32();

            int OPFlag;
            this.mutePews = reader.ReadInt32();
            OPFlag = reader.ReadInt32();
            superOPflag = OPFlag == 0 ? false : true;

            reader.ReadSingle();
            reader.ReadSingle();
            // 10 32bit reads
        }
        else
        {
            // version mismatch, must use defaults or things can get bad
            Debug.Log("Mk2Excavator: Reading modversion: " + modversion + " With entityVersion : " + entityVersion +
                      " And current modversion = " + this.mModVersion);
            GameManager.DoLocalChat("Mk2Excavator version updated. Please replace all placed Mk2Excavators.");

            this.eExcavateState = ExcavateState.ClearGarbage;
            this.eDropState = DropState.DropSome;

            this.mnDigSizeX = mnDigSizeZ = 4;
            this.mnDigSizeY = 64;
            this.mnCurrentDigSizeY = 1;

            // lets get the reads out of the way.
            if (entityVersion < 5)
            {
                reader.ReadSingle();
                reader.ReadBoolean();
                reader.ReadSingle();
                reader.ReadSingle();
                reader.ReadSingle();
                reader.ReadSingle();
                reader.ReadSingle();
                reader.ReadSingle();
                reader.ReadSingle();
                // 10 total reads, one bool
            }
            else
            {
                reader.ReadSingle();
                reader.ReadSingle();
                reader.ReadSingle();
                reader.ReadSingle();
                reader.ReadSingle();
                reader.ReadSingle();
                reader.ReadSingle();
                reader.ReadSingle();
                reader.ReadSingle();
                // 10 total reads
            }
        }

        UpdateDigSettings();
    }

    public override void UnitySuspended()
    {
        this.mPreviewCube = null;
        this.mCurrentCube = null;
        this.mTurretObject = null;
        this.mGunObject = null;
        this.mBarrelObject = null;
        this.mLaserObject = null;
    }

    public override void DropGameObject()
    {
        base.DropGameObject();
        this.mbLinkedToGO = false;
    }

    public override void UnityUpdate()
    {
        if (!this.mbLinkedToGO)
        {
            if ((base.mWrapper != null) && (base.mWrapper.mGameObjectList != null))
            {
                if (base.mWrapper.mGameObjectList[0].gameObject == null)
                {
                    Debug.LogError("Mk2Excavator missing game object #0 (GO)?");
                }

                this.mPreviewCube = base.mWrapper.mGameObjectList[0].gameObject.transform.Search("Preview Cube").gameObject;
                this.mCurrentCube = base.mWrapper.mGameObjectList[0].gameObject.transform.Search("Current Cube").gameObject;
                this.mTurretObject = base.mWrapper.mGameObjectList[0].gameObject.transform.Search("Excavator Turret Holder").gameObject;
                this.mGunObject = base.mWrapper.mGameObjectList[0].gameObject.transform.Search("Excavator Gun Holder").gameObject;
                this.mBarrelObject = base.mWrapper.mGameObjectList[0].gameObject.transform.Search("Excavator Gun").gameObject;
                this.mLaserObject = base.mWrapper.mGameObjectList[0].gameObject.transform.Search("Laser").gameObject;
                this.mExcavatorBase = base.mWrapper.mGameObjectList[0].gameObject.transform.Search("Excavator Base").gameObject;
                this.mExcavatorTurret = base.mWrapper.mGameObjectList[0].gameObject.transform.Search("Excavator Turret").gameObject;

                MeshRenderer baseRenderer = mExcavatorBase.GetComponent<MeshRenderer>();
                MeshRenderer turretRenderer = mExcavatorTurret.GetComponent<MeshRenderer>();
                MeshRenderer barrelRenderer = mBarrelObject.GetComponent<MeshRenderer>();
                if (baseRenderer != null) baseRenderer.material.SetColor("_Color", mCubeColor);
                else Debug.Log("BaseRenderer = null!");
                if (turretRenderer != null) turretRenderer.material.SetColor("_Color", mCubeColor);
                else Debug.Log("TurretRenderer = null!");
                if (barrelRenderer != null) barrelRenderer.material.SetColor("_Color", mCubeColor);
                else Debug.Log("BarrelRenderer = null!");

                this.mbLinkedToGO = true;
            }
        }
        else
        {
            if (this.mbWorkComplete)
            {
                Transform transform = this.mPreviewCube.transform;
                transform.localScale = (Vector3) (transform.localScale * 0.975f);
                Transform transform2 = this.mPreviewCube.transform;
                transform2.localPosition = (Vector3) (transform2.localPosition * 0.975f);
                Transform transform3 = this.mCurrentCube.transform;
                transform3.localScale = (Vector3) (transform3.localScale * 0.975f);
                Transform transform4 = this.mCurrentCube.transform;
                transform4.localPosition = (Vector3) (transform4.localPosition * 0.975f);
            }
            else
            {
                this.mPreviewCube.transform.localScale = new Vector3((this.mnCurrentDigSizeX * 2) + 0.75f,
                (float)this.mnCurrentDigSizeY, (this.mnCurrentDigSizeZ * 2) + 0.75f);
                this.mPreviewCube.transform.localPosition = new Vector3(0f, ((float) this.mnCurrentDigSizeY) / 2f, 0f);
                this.mCurrentCube.transform.localScale = new Vector3((this.mnDigSizeX * 2) + 0.75f,
                (float)this.mnDigSizeY, (this.mnDigSizeZ * 2) + 0.75f);
                this.mCurrentCube.transform.localPosition = new Vector3(0f, ((float) this.mnDigSizeY) / 2f, 0f);
            }

            Vector3 lPos = WorldScript.instance.mPlayerFrustrum.GetCoordsToUnity(this.mnLastDigX, this.mnLastDigY, this.mnLastDigZ);

            if (((this.mrOutOfPowerTime > 2f) || this.mbWorkComplete) || (this.mrTimeSinceShoot > 2f))
            {
                this.mLaserObject.SetActive(false);
                Transform transform5 = this.mTurretObject.transform;
                transform5.forward +=
                    (Vector3) (((Vector3.forward - this.mTurretObject.transform.forward) * Time.deltaTime) * 0.21f);
                Transform transform6 = this.mGunObject.transform;
                transform6.forward +=
                    (Vector3) (((Vector3.up - this.mGunObject.transform.forward) * Time.deltaTime) * 0.21f);
            }
            else
            {
                Vector3 vector2 = lPos - this.mTurretObject.transform.position;
                vector2.y = 0f;
                Transform transform7 = this.mTurretObject.transform;
                transform7.forward += (Vector3)(((vector2 - this.mTurretObject.transform.forward) * Time.deltaTime) * 2f);
                vector2 = lPos - this.mGunObject.transform.position;
                Vector3 forward = this.mTurretObject.transform.forward;
                forward.y = vector2.y;
                Transform transform8 = this.mGunObject.transform;
                transform8.forward += (Vector3)(((forward - this.mGunObject.transform.forward) * Time.deltaTime) * 2.75f);
                vector2 = lPos - this.mBarrelObject.transform.position;
                float magnitude = vector2.magnitude;
                if (this.mrTimeSinceShoot == 0f)
                {
                    this.mLaserObject.SetActive(true);
                    if (mnBlocksDestroyed % 5 == 0)
                    {
                        if (mutePews == 0) AudioHUDManager.instance.ExcavatorFire(this.mTurretObject.transform.position);
                    }

                    if ((SurvivalParticleManager.instance == null) ||
                        (SurvivalParticleManager.instance.GreenShootParticles == null))
                    {
                        if (WorldScript.meGameMode == eGameMode.eCreative)
                        {
                            return;
                        }

                        Debug.LogError("Error, the Mk2AE's particles have gone null?!");
                    }
                    else
                    {
                        SurvivalParticleManager.instance.GreenShootParticles.transform.position = this.mBarrelObject.transform.position + ((Vector3) (this.mBarrelObject.transform.forward * 0.5f));
                        SurvivalParticleManager.instance.GreenShootParticles.transform.forward = this.mBarrelObject.transform.forward;
                        SurvivalParticleManager.instance.GreenShootParticles.Emit(50);
                    }

                    SurvivalDigScript.instance.RockDig(lPos);
                }

                if (this.mrTimeSinceShoot < 2f)
                {
                    this.mLaserObject.transform.right = -this.mGunObject.transform.forward;
                    this.mLaserObject.transform.position =
                        (Vector3) ((this.mGunObject.transform.position +
                                    (this.mGunObject.transform.forward * (magnitude * 0.5f))) +
                                   (this.mGunObject.transform.forward * 0.5f));
                    this.mLaserObject.transform.localScale = new Vector3(magnitude, 2f - this.mrTimeSinceShoot,
                        2f - this.mrTimeSinceShoot);
                    this.mrTimeSinceShoot += Time.deltaTime * 25f;
                    if (this.mrTimeSinceShoot >= 2f)
                    {
                        this.mLaserObject.SetActive(false);
                    }
                }
            }
        }
        //T2 Battery -> Sphere_Low->Renderer()->Material
        //Laser Quad V and Laser Quad H 

        //Excavator Base
        //Excavator Turret
        //Excavator Gun
    }

    public override void LowFrequencyUpdate()
    {
        if (mbWorkComplete)
            return;

        if (this.mrDigDelay > 0f)
        {
            this.mrDigDelay -= LowFrequencyThread.mrPreviousUpdateTimeStep;
            return;
        }

        if (digArea == null)
            digArea = new DigArea(origin, mnCurrentDigSizeY, mnDigSizeX, mnDigSizeY, machineFlags);

        PercentScanned = (float) mnTotalBlocksScanned / digArea.Volume;
        if (digAreaEnumerator == null)
            digAreaEnumerator = digArea.GetRemainingDigArea();

        if (mrCurrentPower < mrPowerRate)
        {
            mrOutOfPowerTime += LowFrequencyThread.mrPreviousUpdateTimeStep;
            return;
        }

        mrOutOfPowerTime = 0f;
        int skipCounter = 0;
        int OPCounter = 0;
        while (true)
        {
            // Get the next cube in our dig area
            if (!currentCube.HasValue)
            {
                if (digAreaEnumerator.MoveNext())
                    currentCube = digAreaEnumerator.Current;
                else // enumerator has no more cubes for us
                {
                    mbWorkComplete = true;
                    return;
                }
            }

            if (!(currentCube is CubeCoord)) return;
            CubeCoord cubeCoord = (CubeCoord)currentCube;
            DigResult result = DigResult.Fail;
            if (WorldScript.mbIsServer)
            {
                result = AttemptToDig(cubeCoord.x, cubeCoord.y, cubeCoord.z);                    
            }

            if (result == DigResult.Fail)
                return;

            mnTotalBlocksScanned++;

            switch (result)
            {
                case DigResult.Dig:
                    // Success, we reset and come back again next tick
                    if (!superOPflag)
                    {
                        currentCube = null;
                        return;
                    }
                    else
                    {
                        OPCounter++;
                        currentCube = null;
                        if (OPCounter > 5)
                            break;
                        continue;
                    }
                case DigResult.Skip:
                    // A block we want to skip ... let's do up to 32 per tick
                    skipCounter++;
                    currentCube = null;
                    if (skipCounter > 32)
                        break;
                    continue;
                case DigResult.Fail:
                    // Fail, we try this cube again next tick
                    return;
            }
        }
    }


    private enum DigResult
    {
        Skip,
        Dig,
        Fail
    }

    private DigResult AttemptToDig(long checkX, long checkY, long checkZ)
    {
        mnDigSizeY = digArea.CurrentHeight;
        var segment = mFrustrum != null
            ? AttemptGetSegment(checkX, checkY, checkZ)
            : WorldScript.instance.GetSegment(checkX, checkY, checkZ);

        if (segment == null || !segment.mbInitialGenerationComplete || segment.mbDestroyed)
        {
            mrDigDelay = 1f;
            return DigResult.Fail;
        }

        ushort cube = segment.GetCube(checkX, checkY, checkZ);

        if (cube == eCubeTypes.CentralPowerHub)
            return DigResult.Dig;

        if (cube == mReplaceType)
            return DigResult.Skip;

        if (CubeHelper.IsReinforced(cube) && eExcavateState != ExcavateState.ClearAll)
        {
            return DigResult.Dig;
        }

        if (CubeHelper.HasEntity((int) cube) && eExcavateState != ExcavateState.ClearAll)
        {
            if (cube != eCubeTypes.AlienPlant && cube != eCubeTypes.ArachnidRock)
            {
                return DigResult.Dig;
            }
        }

        if (TerrainData.GetHardness(cube, 0) > 500f)
        {
            return DigResult.Dig;
        }

        if (eExcavateState == ExcavateState.ClearGarbage)
        {
            if (CubeHelper.IsOre(cube))
            {
                return DigResult.Dig;
            }
        }

        // Tranqs Creative Survival mod really breaks this call as nearly everything in the game is craftable.
        if (CraftingManager.IsCraftable(cube) && cube != eCubeTypes.Giger &&
            eExcavateState != ExcavateState.ClearAll)
        {
            // ore should never ever be craftable
            if (!CubeHelper.IsOre(cube))
                return DigResult.Dig;
        }

        int num = (int) (checkX - segment.baseX);
        int num2 = (int) (checkY - segment.baseY);
        int num3 = (int) (checkZ - segment.baseZ);
        if (num == 0 && base.AttemptGetSegment(checkX - 1L, checkY, checkZ) == null)
        {
            return DigResult.Fail;
        }

        if (num == 15 && base.AttemptGetSegment(checkX + 1L, checkY, checkZ) == null)
        {
            return DigResult.Fail;
        }

        if (num2 == 0 && base.AttemptGetSegment(checkX, checkY - 1L, checkZ) == null)
        {
            return DigResult.Fail;
        }

        if (num2 == 15 && base.AttemptGetSegment(checkX, checkY + 1L, checkZ) == null)
        {
            return DigResult.Fail;
        }

        if (num3 == 0 && base.AttemptGetSegment(checkX, checkY, checkZ - 1L) == null)
        {
            return DigResult.Fail;
        }

        if (num3 == 15 && base.AttemptGetSegment(checkX, checkY, checkZ + 1L) == null)
        {
            return DigResult.Fail;
        }

        ushort mValue = segment.GetCubeData(checkX, checkY, checkZ).mValue;
        WorldScript.instance.BuildFromEntity(segment, checkX, checkY, checkZ, this.mReplaceType,
            TerrainData.GetDefaultValue(this.mReplaceType));
        this.mrCurrentPower -= this.mrPowerRate;
        this.mbLocatedBlock = true;
        this.mnBlocksDestroyed++;
        if (segment.mbInLocalFrustrum)
        {
            if (eDropState == DropState.DropSome)
            {
                bool flag = true;
                if (CubeHelper.IsGarbage(cube) && this.mRand.Next(100) > 5)
                {
                    flag = false;
                }

                if (flag)
                {
                    Vector3 velocity = new Vector3((float) this.mRand.NextDouble() - 0.5f, 0f,
                        (float) this.mRand.NextDouble() - 0.5f);
                    velocity.x *= 5f;
                    velocity.z *= 5f;
                    ItemManager.DropNewCubeStack(cube, mValue, 1, checkX, checkY, checkZ, velocity);
                }
            }

            if (eDropState == DropState.DropAll)
            {
                Vector3 velocity = new Vector3((float) this.mRand.NextDouble() - 0.5f, 0f,
                    (float) this.mRand.NextDouble() - 0.5f);
                velocity.x *= 5f;
                velocity.z *= 5f;
                ItemManager.DropNewCubeStack(cube, mValue, 1, checkX, checkY, checkZ, velocity);
            }

            if (eDropState == DropState.DropOre && CubeHelper.IsOre(cube))
            {
                Vector3 velocity = new Vector3((float) this.mRand.NextDouble() - 0.5f, 0f,
                    (float) this.mRand.NextDouble() - 0.5f);
                velocity.x *= 5f;
                velocity.z *= 5f;
                ItemManager.DropNewCubeStack(cube, mValue, 1, checkX, checkY, checkZ, velocity);
            }

            this.mrTimeSinceShoot = 0f;
        }

        return DigResult.Dig;
    }

    public float GetRemainingPowerCapacity()
    {
        return this.mrMaxPower - this.mrCurrentPower;
    }

    public float GetMaximumDeliveryRate()
    {
        return float.MaxValue;
    }

    public float GetMaxPower()
    {
        return mrMaxPower;
    }

    public bool WantsPowerFromEntity(SegmentEntity entity)
    {
        return true;
    }

    public bool DeliverPower(float amount)
    {
        if (amount > GetRemainingPowerCapacity())
        {
            return false;
        }

        this.mrCurrentPower += amount;
        this.MarkDirtyDelayed();
        return true;
    }

    public int GetCubeType(string key)
    {
        ModCubeMap modCubeMap = null;
        ModManager.mModMappings.CubesByKey.TryGetValue(key, out modCubeMap);
        bool flag = modCubeMap != null;
        int result;
        if (flag)
        {
            result = (int) modCubeMap.CubeType;
        }
        else
        {
            result = 0;
        }

        if (result == 0)
        {
            Debug.Log("Mk2Extractor : GetCubeType returned 0");
        }

        return result;
    }

    public void ChangeFireMode()
    {
        if (this.eExcavateState == ExcavateState.ClearGarbage)
        {
            this.eExcavateState = ExcavateState.ClearOre;
            this.mbDoDigOre = true;
            this.mrPowerRate = this.mrPowerRateOre;
        }
        else if (this.eExcavateState == ExcavateState.ClearOre)
        {
            this.eExcavateState = ExcavateState.ClearAll;
            this.mbDoDigOre = true;
            this.mrPowerRate = this.mrPowerRateOre;
        }
        else
        {
            this.eExcavateState = ExcavateState.ClearGarbage;
            this.mbDoDigOre = false;
            this.mrPowerRate = this.mrPowerRateDefault;
        }
    }

    public void ChangeDropMode()
    {
        switch (eDropState)
        {
            case DropState.DropSome:
                eDropState = DropState.DropNone;
                break;
            case DropState.DropNone:
                eDropState = DropState.DropOre;
                break;
            case DropState.DropOre:
                eDropState = DropState.DropAll;
                break;
            case DropState.DropAll:
                eDropState = DropState.DropSome;
                break;
            default:
                break;
        }
    }

    public void UpdateDigSettings()
    {
        this.mnCurrentDigSizeX = mnDigSizeX; // 9;
        if (mnDigSizeY < 20)
        {
            this.mnCurrentDigSizeY = mnDigSizeY;
        }
        else
        {
            this.mnCurrentDigSizeY = (mnDigSizeY / 4) - 1; // 31;
        }

        this.mnCurrentDigSizeZ = mnDigSizeX; // 9;

        digArea = null;
        digArea = new DigArea(origin, 0, mnDigSizeX, mnDigSizeY, machineFlags);
        digAreaEnumerator = null;
        currentCube = null;

        if (superOPflag)
        {
            this.mrPowerRate = 1f;
            this.mrPowerRateOre = 1f;
        }
        else
        {
            this.mrPowerRate = powerDefaultBackup;
            this.mrPowerRateOre = powerOreBackup;
        }

        mbWorkComplete = false;
        mnBlocksDestroyed = 0;
        mnTotalBlocksScanned = 0;
        this.RequestImmediateNetworkUpdate();
    }

    public override string GetPopupText()
    {
        ushort selectBlockType = WorldScript.instance.localPlayerInstance.mPlayerBlockPicker.selectBlockType;
        bool flag = (int) selectBlockType == this.GetCubeType("FlexibleGames.Mk2Excavator");
        if (flag)
        {
            Mk2Excavator lmk2Excavator =
                (Mk2Excavator) WorldScript.instance.localPlayerInstance.mPlayerBlockPicker.selectedEntity;
            bool flag2 = lmk2Excavator != null;
            if (flag2)
            {
                bool allowMovement = Mk2Excavator.AllowMovement;
                if (allowMovement)
                {
                    bool flag18 = Input.GetKeyDown(KeyCode.Q); //.GetButton("Extract"); 
                    if (flag18)
                    {
                        this.mfCommandDebounce += LowFrequencyThread.mrPreviousUpdateTimeStep;
                        //Debug.LogWarning("Q pressed, Debounce = " + mfCommandDebounce);
                        if (mfCommandDebounce >= 0.2f)
                        {
                            AudioHUDManager.instance.HUDOut();
                            //Mk2Excavator.ForceNGUIUpdate = 0.1f;
                            ChangeFireMode();
                            if (!WorldScript.mbIsServer)
                            {
                                string digstate;
                                switch (this.eExcavateState)
                                {
                                    case ExcavateState.ClearAll:
                                        digstate = "ClearAll";
                                        break;
                                    case ExcavateState.ClearGarbage:
                                        digstate = "ClearGarbage";
                                        break;
                                    case ExcavateState.ClearOre:
                                        digstate = "ClearOre";
                                        break;
                                    default:
                                        digstate = "Error";
                                        break;
                                }

                                Mk2ExcavatorWindow.AlterDigState(this, digstate);
                                //NetworkManager.instance.SendInterfaceCommand("FlexibleGames.Mk2ExcavatorWindow", "AlterDigState", digstate, null, this, 0.0f);
                            }

                            UIManager.ForceNGUIUpdate = 0.1f;
                            mfCommandDebounce = 0f;
                        }
                    }

                    bool interactkey = Input.GetKeyDown(KeyCode.T); //.GetButton("Extract"); 
                    if (interactkey)
                    {
                        this.mfCommandDebounce += LowFrequencyThread.mrPreviousUpdateTimeStep;
                        // Debug.LogWarning("T pressed, Debounce = " + mfCommandDebounce);
                        if (this.mfCommandDebounce >= 0.2f)
                        {
                            AudioHUDManager.instance.HUDOut();
                            //this.mbDoDropBlocks = !this.mbDoDropBlocks;
                            ChangeDropMode();
                            if (!WorldScript.mbIsServer)
                            {
                                string dropstate;
                                switch (this.eDropState)
                                {
                                    case DropState.DropAll:
                                        dropstate = "DropAll";
                                        break;
                                    case DropState.DropNone:
                                        dropstate = "DropNone";
                                        break;
                                    case DropState.DropOre:
                                        dropstate = "DropOre";
                                        break;
                                    case DropState.DropSome:
                                        dropstate = "DropSome";
                                        break;
                                    default:
                                        dropstate = "Error";
                                        break;
                                }

                                Mk2ExcavatorWindow.AlterDropState(this, dropstate);
                                //NetworkManager.instance.SendInterfaceCommand("FlexibleGames.Mk2ExcavatorWindow", "AlterDropState", dropstate, null, this, 0.0f);
                            }

                            UIManager.ForceNGUIUpdate = 0.1f;
                            mfCommandDebounce = 0f;
                        }
                    }

                    bool lshiftkey = Input.GetKey(KeyCode.LeftShift);
                    bool rshiftkey = Input.GetKey(KeyCode.RightShift);

                    bool radiusupkey = Input.GetKeyDown(KeyCode.Home);
                    bool superOPkey = Input.GetKeyDown(KeyCode.KeypadMultiply);
                    if ((lshiftkey || rshiftkey) && superOPkey)
                    {
                        this.mfCommandDebounce += LowFrequencyThread.mrPreviousUpdateTimeStep;
                        if (this.mfCommandDebounce >= 0.4f)
                        {
                            superOPflag = !superOPflag;

                            Mk2ExcavatorWindow.SuperOPMode(this, superOPflag ? 1 : 0);                            
                            mfCommandDebounce = 0f;
                        }
                    }

                    if (radiusupkey)
                    {
                        this.mfCommandDebounce += LowFrequencyThread.mrPreviousUpdateTimeStep;
                        //GameManager.DoLocalChat("HomeKey Pressed: mnDigSizeX was " + this.mnDigSizeX);
                        if (this.mfCommandDebounce >= 0.2f)
                        {
                            AudioHUDManager.instance.HUDOut();
                            if (this.mnDigSizeX < 1024)
                            {
                                if (lshiftkey || rshiftkey)
                                {
                                    this.mnDigSizeX += 10;
                                    this.mnDigSizeZ += 10;
                                }
                                else
                                {
                                    this.mnDigSizeX++;
                                    this.mnDigSizeZ++;
                                }

                                Mk2ExcavatorWindow.AlterRadius(this, mnDigSizeX);
                                //if (!WorldScript.mbIsServer)
                                //    NetworkManager.instance.SendInterfaceCommand("FlexibleGames.Mk2ExcavatorWindow", "AlterRadius", mnDigSizeX.ToString(), null, this, 0.0f);
                                //GameManager.DoLocalChat("HomeKey Pressed: mnDigSizeX now " + this.mnDigSizeX);
                            }

                            UIManager.ForceNGUIUpdate = 0.1f;
                            mfCommandDebounce = 0f;
                        }
                    }

                    bool radiusdownkey = Input.GetKeyDown(KeyCode.End);
                    if (radiusdownkey)
                    {
                        this.mfCommandDebounce += LowFrequencyThread.mrPreviousUpdateTimeStep;
                        if (this.mfCommandDebounce >= 0.2f)
                        {
                            AudioHUDManager.instance.HUDOut();
                            if (this.mnDigSizeX > 1)
                            {
                                if ((lshiftkey || rshiftkey) && mnDigSizeX > 10)
                                {
                                    this.mnDigSizeX -= 10;
                                    this.mnDigSizeZ -= 10;
                                }
                                else
                                {
                                    this.mnDigSizeX--;
                                    this.mnDigSizeZ--;
                                }

                                Mk2ExcavatorWindow.AlterRadius(this, mnDigSizeX);
                                //if (!WorldScript.mbIsServer)
                                //    NetworkManager.instance.SendInterfaceCommand("FlexibleGames.Mk2ExcavatorWindow", "AlterRadius", mnDigSizeX.ToString(), null, this, 0.0f);
                            }

                            UIManager.ForceNGUIUpdate = 0.1f;
                            mfCommandDebounce = 0f;
                        }
                    }

                    bool heightupkey = Input.GetKey(KeyCode.KeypadPlus);
                    if (heightupkey)
                    {
                        this.mfCommandDebounce += LowFrequencyThread.mrPreviousUpdateTimeStep;
                        if (this.mfCommandDebounce >= 2.0f)
                        {
                            AudioHUDManager.instance.HUDOut();
                            if (this.mnDigSizeY < 2048)
                            {
                                if ((lshiftkey || rshiftkey) && mnDigSizeY < 2038)
                                {
                                    this.mnDigSizeY += 10;
                                }
                                else
                                {
                                    this.mnDigSizeY++;
                                }

                                Mk2ExcavatorWindow.AlterHeight(this, mnDigSizeY);
                                //if (!WorldScript.mbIsServer)
                                //    NetworkManager.instance.SendInterfaceCommand("FlexibleGames.Mk2ExcavatorWindow", "AlterHeight", mnDigSizeY.ToString(), null, this, 0.0f);
                            }

                            UIManager.ForceNGUIUpdate = 0.1f;
                            mfCommandDebounce = 0f;
                        }
                    }

                    bool heightdownkey = Input.GetKey(KeyCode.KeypadMinus);
                    if (heightdownkey)
                    {
                        this.mfCommandDebounce += LowFrequencyThread.mrPreviousUpdateTimeStep;
                        if (this.mfCommandDebounce >= 2.0f)
                        {
                            AudioHUDManager.instance.HUDOut();
                            if (this.mnDigSizeY > 4)
                            {
                                if ((lshiftkey || rshiftkey) && mnDigSizeY > 14)
                                {
                                    this.mnDigSizeY -= 10;
                                }
                                else
                                {
                                    this.mnDigSizeY--;
                                }

                                Mk2ExcavatorWindow.AlterHeight(this, mnDigSizeY);
                                //if (!WorldScript.mbIsServer)
                                //    NetworkManager.instance.SendInterfaceCommand("FlexibleGames.Mk2ExcavatorWindow", "AlterHeight", mnDigSizeY.ToString(), null, this, 0.0f);
                            }

                            UIManager.ForceNGUIUpdate = 0.1f;
                            mfCommandDebounce = 0f;
                        }
                    }

                    bool rctrlKey = Input.GetKeyDown(KeyCode.RightControl);
                    bool lctrlKey = Input.GetKeyDown(KeyCode.LeftControl);
                    bool essKey = Input.GetKeyDown(KeyCode.S);
                    if ((rctrlKey || lctrlKey) && essKey)
                    {
                        this.mfCommandDebounce += LowFrequencyThread.mrPreviousUpdateTimeStep;
                        if (this.mfCommandDebounce >= 0.4f)
                        {
                            mutePews = mutePews == 0 ? 1 : 0;
                            Mk2ExcavatorWindow.MutePews(this, mutePews);
                            mfCommandDebounce = 0f;
                        }
                    }

                    if (flag18 || interactkey)
                    {
                        MarkDirtyDelayed();
                    }

                    if (radiusupkey || radiusdownkey || heightdownkey || heightupkey || superOPkey || essKey)
                    {
                        this.RequestImmediateNetworkUpdate();
                        UpdateDigSettings();
                        MarkDirtyDelayed();
                    }
                }
                UIManager.ForceNGUIUpdate = 0.1f;

                var response = new StringBuilder();
                response.AppendLine("Mk2 Excavator v" + mModVersion + (mutePews!=0 ? " muted" : ""));
                response.AppendLine(mnTotalBlocksScanned + " Blocks Scanned");
                response.AppendLine((PercentScanned * 100) + "% complete " + (superOPflag ? "@.@" : ""));
                response.AppendLine(mrCurrentPower + "/" + mrMaxPower + " power");
                response.AppendLine("(Q)Clear mode: Currently: " + eExcavateState);
                response.AppendLine("(T)Drop mode: Currently: " + eDropState);
                response.AppendLine("(Shift) (Home/End) Radius: " + mnDigSizeX);
                response.AppendLine("(Shift) Numpad (+/-) Height: " + mnDigSizeY);
                PopUpText = response.ToString();
            }
        }

        return PopUpText;
    }
}