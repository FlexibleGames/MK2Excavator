using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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

	public float mrNormalisedPower;

	public float mrPowerSpareCapacity;

	public float mrPowerRate = 20f;

    public int mModVersion = 9;
    public static float ForceNGUIUpdate;
    private string PopUpText;
    public static bool AllowMovement = true;
    public bool mbDoDigOre;
    public float mrPowerRateOre = 80f;
    public float mrPowerRateDefault = 20f;
    public Mk2Excavator.ExcavateState eExcavateState;
    public Mk2Excavator.DropState eDropState;
    public static bool AllowBuilding = true;
    public static bool AllowInteracting = true;
    public static bool AllowLooking = true;
    public float mfCommandDebounce;
    public int miTotalVolume;
    public Color mCubeColor;
    private GameObject mExcavatorBase;
    private GameObject mExcavatorTurret;
    public bool mbDoDropBlocks;    

	private int mnXDig;

	private int mnYDig;

	private int mnZDig;

    public int mnDigSizeX;

    public int mnDigSizeY;

    public int mnDigSizeZ;

	private int mnCurrentDigSizeX;

	private int mnCurrentDigSizeY;

	private int mnCurrentDigSizeZ;

	public float PercentScanned;

	private int mnLFUpdates; // ?

	private bool mbLinkedToGO;

	public bool mbWorkComplete;

	public bool mbLocatedBlock;

	private float mrDigDelay;

	private GameObject mPreviewCube;

	private GameObject mCurrentCube;

	private GameObject mTurretObject; // no MR
	private GameObject mGunObject;
	private GameObject mBarrelObject;
	private GameObject mLaserObject;  // No MR

	public float mrTimeSinceShoot;

	public int mnBlocksDestroyed;

	private float mrOutOfPowerTime;

	private System.Random mRand;

	private int mnTotalBlocksScanned;

	private long mnLastDigX;

	private long mnLastDigY;

	private long mnLastDigZ;

	private ushort mReplaceType = 1;

    public Mk2Excavator(Segment segment, long x, long y, long z, ushort cube, byte flags, ushort lValue, bool lbFromDisk, int powerDefault, int powerOre, int digRadius, int digHeight, int maxPower)
        : base(eSegmentEntity.Mod, SpawnableObjectEnum.AutoExcavator, x, y, z, cube, flags, lValue, Vector3.zero, segment)
	{
        this.mrPowerRate = powerDefault;
        this.mrPowerRateDefault = this.mrPowerRate;
        this.mrPowerRateOre = powerOre;
		this.mbNeedsLowFrequencyUpdate = true;
		this.mbNeedsUnityUpdate = true;
		this.mbWorkComplete = false;
        this.mnDigSizeX = digRadius; // 9;
        this.mnDigSizeY = digHeight; // 128;
        this.mnDigSizeZ = digRadius; // 9;
        this.mnCurrentDigSizeX = digRadius; // 9;
        if (digHeight < 20)
        {
            this.mnCurrentDigSizeY = digHeight;
        }
        else
        {
            this.mnCurrentDigSizeY = (digHeight / 4) - 1; // 31;
        }
        this.mnCurrentDigSizeZ = digRadius; // 9;
		this.mnXDig = -this.mnCurrentDigSizeX;
		this.mnYDig = 1;
		this.mnZDig = -this.mnCurrentDigSizeZ;
		this.mRand = new System.Random();
		this.mbLocatedBlock = false;
        this.mbDoDigOre = false;
        this.eExcavateState = ExcavateState.ClearGarbage;
        this.eDropState = DropState.DropSome;
        this.mfCommandDebounce = 0.02f;
        this.miTotalVolume = (digRadius * 2 + 1) * (digRadius * 2 + 1) * digHeight; // mnDigSizeX * mnDigSizeY * mnDigSizeZ;
        this.mCubeColor = Color.blue;
        this.mbDoDropBlocks = true;
        this.mrMaxPower = maxPower;        
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

        writer.Write(this.mModVersion);  // mod version
        writer.Write((int)this.eExcavateState); // Excavate state
        writer.Write((int)this.eDropState);  // drop state
        writer.Write(this.mnDigSizeX);  // current radius
        writer.Write(this.mnDigSizeY); // max dig height
        writer.Write(this.mnCurrentDigSizeY); // current dig height

        writer.Write(value);
        writer.Write(value);
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
            if (modversion == 8)
            {
                GameManager.DoLocalChat("Mk2Excavator version updated. Please replace all placed Mk2Excavators.");
            }

            // versions match, load normally
            this.eExcavateState = (ExcavateState)reader.ReadInt32();
            this.eDropState = (DropState)reader.ReadInt32();
            this.mnDigSizeX = reader.ReadInt32();
            this.mnDigSizeZ = mnDigSizeX;
            this.mnDigSizeY = reader.ReadInt32();
            this.mnCurrentDigSizeY = reader.ReadInt32();

            reader.ReadSingle();
            reader.ReadSingle();
            reader.ReadSingle();
            reader.ReadSingle();
            // 10 32bit reads
        }
        else
        {
            // version mismatch, must use defaults or things can get bad
            Debug.Log("Mk2Excavator: Reading modversion: " + modversion + " With entityVersion : " + entityVersion + " And current modversion = " + this.mModVersion);
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
                transform.localScale = (Vector3)(transform.localScale * 0.975f);
                Transform transform2 = this.mPreviewCube.transform;
                transform2.localPosition = (Vector3)(transform2.localPosition * 0.975f);
                Transform transform3 = this.mCurrentCube.transform;
                transform3.localScale = (Vector3)(transform3.localScale * 0.975f);
                Transform transform4 = this.mCurrentCube.transform;
                transform4.localPosition = (Vector3)(transform4.localPosition * 0.975f);
            }
            else
            {
                this.mPreviewCube.transform.localScale = new Vector3((this.mnCurrentDigSizeX * 2) + 0.75f, (float)this.mnCurrentDigSizeY, (this.mnCurrentDigSizeZ * 2) + 0.75f);
                this.mPreviewCube.transform.localPosition = new Vector3(0f, ((float)this.mnCurrentDigSizeY) / 2f, 0f);
                this.mCurrentCube.transform.localScale = new Vector3((this.mnDigSizeX * 2) + 0.75f, (float)this.mnDigSizeY, (this.mnDigSizeZ * 2) + 0.75f);
                this.mCurrentCube.transform.localPosition = new Vector3(0f, ((float)this.mnDigSizeY) / 2f, 0f);
            }
            Vector3 lPos = WorldScript.instance.mPlayerFrustrum.GetCoordsToUnity(this.mnLastDigX, this.mnLastDigY, this.mnLastDigZ);
            if (((this.mrOutOfPowerTime > 2f) || this.mbWorkComplete) || (this.mrTimeSinceShoot > 2f))
            {
                this.mLaserObject.SetActive(false);
                Transform transform5 = this.mTurretObject.transform;
                transform5.forward += (Vector3)(((Vector3.forward - this.mTurretObject.transform.forward) * Time.deltaTime) * 0.21f);
                Transform transform6 = this.mGunObject.transform;
                transform6.forward += (Vector3)(((Vector3.up - this.mGunObject.transform.forward) * Time.deltaTime) * 0.21f);
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
                        AudioHUDManager.instance.ExcavatorFire(this.mTurretObject.transform.position);
                    }
                    if ((SurvivalParticleManager.instance == null) || (SurvivalParticleManager.instance.GreenShootParticles == null))
                    {
                        if (WorldScript.meGameMode == eGameMode.eCreative)
                        {
                            return;
                        }
                        Debug.LogError("Error, the Mk2AE's particles have gone null?!");
                    }
                    else
                    {
                        SurvivalParticleManager.instance.GreenShootParticles.transform.position = this.mBarrelObject.transform.position + ((Vector3)(this.mBarrelObject.transform.forward * 0.5f));
                        SurvivalParticleManager.instance.GreenShootParticles.transform.forward = this.mBarrelObject.transform.forward;
                        SurvivalParticleManager.instance.GreenShootParticles.Emit(50);
                    }
                    SurvivalDigScript.instance.RockDig(lPos);
                }
                if (this.mrTimeSinceShoot < 2f)
                {
                    this.mLaserObject.transform.right = -this.mGunObject.transform.forward;
                    this.mLaserObject.transform.position = (Vector3)((this.mGunObject.transform.position + (this.mGunObject.transform.forward * (magnitude * 0.5f))) + (this.mGunObject.transform.forward * 0.5f));
                    this.mLaserObject.transform.localScale = new Vector3(magnitude, 2f - this.mrTimeSinceShoot, 2f - this.mrTimeSinceShoot);
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

	private bool MoveToNextEdgeCube()
	{
		this.mnXDig++;
		if (this.mnXDig > this.mnCurrentDigSizeX)
		{
			this.mnXDig = -this.mnCurrentDigSizeX;
			this.mnZDig++;
			if (this.mnZDig > this.mnCurrentDigSizeZ)
			{
				this.mnZDig = -this.mnCurrentDigSizeZ;
				this.mnYDig++;
				if (this.mnYDig > this.mnCurrentDigSizeY)
				{
					this.mbWorkComplete = true;
					if (this.mnCurrentDigSizeX < this.mnDigSizeX)
					{
						this.mnCurrentDigSizeX++;
						this.mbWorkComplete = false;
					}
					if (this.mnCurrentDigSizeY < this.mnDigSizeY)
					{
						this.mnCurrentDigSizeY++;
						this.mbWorkComplete = false;
					}
					if (this.mnCurrentDigSizeZ < this.mnDigSizeZ)
					{
						this.mnCurrentDigSizeZ++;
						this.mbWorkComplete = false;
					}
					this.mnXDig = -this.mnCurrentDigSizeX;
					this.mnZDig = -this.mnCurrentDigSizeZ;
					if (this.mbWorkComplete)
					{
						return true;
					}
				}
			}
		}
		return this.mnXDig == -this.mnCurrentDigSizeX || this.mnXDig == this.mnCurrentDigSizeX || (this.mnYDig == 1 || this.mnYDig == this.mnCurrentDigSizeY) || (this.mnZDig != -this.mnCurrentDigSizeZ && this.mnZDig != this.mnCurrentDigSizeZ) || true;
	}

	private void MoveToNextCube()
	{
		while (!this.MoveToNextEdgeCube())
		{
		}
	}

	public override void LowFrequencyUpdate()
	{
		if (this.mrDigDelay > 0f)
		{
			this.mrDigDelay -= LowFrequencyThread.mrPreviousUpdateTimeStep;
			return;
		}



        // total number of blocks
        //int num = this.mnDigSizeX * this.mnDigSizeY * this.mnDigSizeZ;
        // number we have processed thus far
        int num2 = this.mnTotalBlocksScanned;

        //what percent of the total have we scanned?
        float num3 = (float)num2 / (float)this.miTotalVolume;
            
        //float num3 = (float)this.mnYDig / (float)this.mnDigSizeY;
        this.PercentScanned = num3;// - this.PercentScanned) * 0.025f;
        
		for (int i = 0; i < 128; i++)
		{
			if (this.mrCurrentPower < this.mrPowerRate)
			{
				this.mrOutOfPowerTime += LowFrequencyThread.mrPreviousUpdateTimeStep;
				return;
			}
			this.mrOutOfPowerTime = 0f;
			if (this.mbWorkComplete)
			{
				return;
			}
			long checkX = this.mnX + (long)this.mnXDig;
			long checkY = this.mnY + (long)this.mnYDig;
			long checkZ = this.mnZ + (long)this.mnZDig;
			if (this.AttemptToDig(checkX, checkY, checkZ))
			{
				i = 999;
				this.mnLastDigX = checkX;
				this.mnLastDigY = checkY;
				this.mnLastDigZ = checkZ;
				this.MoveToNextCube();
				this.mnTotalBlocksScanned++;
			}
		}
	}

	private bool AttemptToDig(long checkX, long checkY, long checkZ)
	{
		Segment segment;
		if (this.mFrustrum != null)
		{
			segment = base.AttemptGetSegment(checkX, checkY, checkZ);
			if (segment == null)
			{
				return false;
			}
		}
		else
		{
			segment = WorldScript.instance.GetSegment(checkX, checkY, checkZ);
			if (segment == null || !segment.mbInitialGenerationComplete || segment.mbDestroyed)
			{
				this.mrDigDelay = 1f;
				return false;
			}
		}
		ushort cube = segment.GetCube(checkX, checkY, checkZ);

        if (cube == 502) // CPH block ID
        {
            return true;
        }

        if (cube != this.mReplaceType)
        {
            if (CubeHelper.IsReinforced(cube) && eExcavateState != ExcavateState.ClearAll)
            {
                return true;
            }
            if (CubeHelper.HasEntity((int)cube) && eExcavateState != ExcavateState.ClearAll)
            {
                if (cube != 166 && cube != 165)
                {
                    return true;
                }
            }
            if (global::TerrainData.GetHardness(cube, 0) > 500f)
            {
                return true;
            }
            if (eExcavateState == ExcavateState.ClearGarbage)
            {
                if (CubeHelper.IsOre(cube))
                {
                    return true;
                }
            }

            // Tranqs Creative Survival mod really breaks this call as nearly everything in the game is craftable.
            if (CraftingManager.IsCraftable(cube) && cube != eCubeTypes.Giger && eExcavateState != ExcavateState.ClearAll)
            {
                // ore should never ever be craftable
                if (!CubeHelper.IsOre(cube))
                    return true;
            }
            int num = (int)(checkX - segment.baseX);
            int num2 = (int)(checkY - segment.baseY);
            int num3 = (int)(checkZ - segment.baseZ);
            if (num == 0 && base.AttemptGetSegment(checkX - 1L, checkY, checkZ) == null)
            {
                return false;
            }
            if (num == 15 && base.AttemptGetSegment(checkX + 1L, checkY, checkZ) == null)
            {
                return false;
            }
            if (num2 == 0 && base.AttemptGetSegment(checkX, checkY - 1L, checkZ) == null)
            {
                return false;
            }
            if (num2 == 15 && base.AttemptGetSegment(checkX, checkY + 1L, checkZ) == null)
            {
                return false;
            }
            if (num3 == 0 && base.AttemptGetSegment(checkX, checkY, checkZ - 1L) == null)
            {
                return false;
            }
            if (num3 == 15 && base.AttemptGetSegment(checkX, checkY, checkZ + 1L) == null)
            {
                return false;
            }
            ushort mValue = segment.GetCubeData(checkX, checkY, checkZ).mValue;
            WorldScript.instance.BuildFromEntity(segment, checkX, checkY, checkZ, this.mReplaceType, global::TerrainData.GetDefaultValue(this.mReplaceType));
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
                        Vector3 velocity = new Vector3((float)this.mRand.NextDouble() - 0.5f, 0f, (float)this.mRand.NextDouble() - 0.5f);
                        velocity.x *= 5f;
                        velocity.z *= 5f;
                        ItemManager.DropNewCubeStack(cube, mValue, 1, checkX, checkY, checkZ, velocity);
                    }

                }
                if (eDropState == DropState.DropAll)
                {
                    Vector3 velocity = new Vector3((float)this.mRand.NextDouble() - 0.5f, 0f, (float)this.mRand.NextDouble() - 0.5f);
                    velocity.x *= 5f;
                    velocity.z *= 5f;
                    ItemManager.DropNewCubeStack(cube, mValue, 1, checkX, checkY, checkZ, velocity);
                }
                if (eDropState == DropState.DropOre && CubeHelper.IsOre(cube))
                {
                    Vector3 velocity = new Vector3((float)this.mRand.NextDouble() - 0.5f, 0f, (float)this.mRand.NextDouble() - 0.5f);
                    velocity.x *= 5f;
                    velocity.z *= 5f;
                    ItemManager.DropNewCubeStack(cube, mValue, 1, checkX, checkY, checkZ, velocity);
                }
                this.mrTimeSinceShoot = 0f;
            }
        }
        else
        {
            MoveToNextCube();
            mnTotalBlocksScanned++;
            return false;
        }
		return true;
	}

	public float GetRemainingPowerCapacity()
	{
		return this.mrMaxPower - this.mrCurrentPower;
	}

	public float GetMaximumDeliveryRate()
	{
		return 3.40282347E+38f;
	}

	public float GetMaxPower()
	{
		return this.mrMaxPower;
	}

	public bool WantsPowerFromEntity(SegmentEntity entity)
	{
		return true;
	}

	public bool DeliverPower(float amount)
	{
		if (amount > this.GetRemainingPowerCapacity())
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
            result = (int)modCubeMap.CubeType;
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

        // resets dig target
        this.mnXDig = -this.mnCurrentDigSizeX;
        this.mnYDig = 1;
        this.mnZDig = -this.mnCurrentDigSizeZ;

        mbWorkComplete = false;
        mnBlocksDestroyed = 0;
        mnTotalBlocksScanned = 0;

        this.miTotalVolume = (mnDigSizeX * 2 + 1) * (mnDigSizeX * 2 + 1) * mnDigSizeY;
    }

    public override string GetPopupText()
    {        
        ushort selectBlockType = WorldScript.instance.localPlayerInstance.mPlayerBlockPicker.selectBlockType;
        bool flag = (int)selectBlockType == this.GetCubeType("FlexibleGames.Mk2Excavator");
        if (flag)
        {
            Mk2Excavator lmk2Excavator = (Mk2Excavator)WorldScript.instance.localPlayerInstance.mPlayerBlockPicker.selectedEntity;
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
                                    case ExcavateState.ClearAll: digstate = "ClearAll"; break;
                                    case ExcavateState.ClearGarbage: digstate = "ClearGarbage"; break;
                                    case ExcavateState.ClearOre: digstate = "ClearOre"; break;
                                    default: digstate = "Error"; break;
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
                                    case DropState.DropAll: dropstate = "DropAll"; break;
                                    case DropState.DropNone: dropstate = "DropNone"; break;
                                    case DropState.DropOre: dropstate = "DropOre"; break;
                                    case DropState.DropSome: dropstate = "DropSome"; break;
                                    default: dropstate = "Error"; break;
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
                    if (flag18 || interactkey)
                    {
                        MarkDirtyDelayed();
                    }
                    if (radiusupkey || radiusdownkey || heightdownkey || heightupkey)
                    {
                        UpdateDigSettings();
                        MarkDirtyDelayed();
                    }
                }
                string text = string.Empty;

                text = "Mk2 Auto-Excavator v" + this.mModVersion;
                text += "\n" + this.mnTotalBlocksScanned + " Blocks Scanned";
                text += "\n" + this.PercentScanned * 100 + "%";
                text += "\n" + this.mrCurrentPower + "/" + this.mrMaxPower + " power";
                text += "\n(Q)Clear mode: Currently: " + this.eExcavateState;
                text += "\n(T)Drop mode: Currently: " + this.eDropState;
                text += "\n(Shift) (Home/End) Radius: " + this.mnDigSizeX;
                text += "\n(Shift) NumPad (+/-) Height: " + this.mnDigSizeY;                

                this.PopUpText = text;
            }
        }
        return this.PopUpText;
    }
}
