using System.Collections;
using Audio;
using System.Runtime.CompilerServices;
using UnityEngine;

public class WindmillPowerItem : PowerSource 
{
    private float CurrentWindSpeed;
    private string IdleSound;
    private float NextUpdateTime;

    private int UpdateTime = 2;

    private float MinSpeed = 10;
    private float MaxSpeed = 20;
    private float WattPerMph = 2;

    private Animator animator;

    public override PowerItemTypes PowerItemType
    {
        get
        {
            return (PowerItemTypes) 12;
        }
    }

    public override string OnSound
    {
        get
        {
            return "solarpanel_on";
        }
    }

    public override string OffSound
    {
        get
        {
            return "solarpanel_off";
        }
    }

    public WindmillPowerItem() : base()
    {
        IdleSound = "solarpanel_idle";
    }

    private int updateCounter = 0;
    IEnumerator ChangeSpeed(float targetSpeed)
    {

        var myCounter = ++updateCounter;
        float time = 0f;
        var totalTime = 10;
        var speed = animator.GetFloat("Speed");

        while (true)
        {
            var newSpeed = Mathf.Lerp(speed, targetSpeed, time / totalTime);
            animator.SetFloat("Speed", newSpeed);

            time += Time.deltaTime;

            if (time > totalTime || myCounter != updateCounter) break;
            yield return null;
        }
    }

    private float lastChange = 99999f;

    private void CanPower()
    {
        
        var last = CurrentPower;
        //if (TileEntity != null)
        //{
        //    Chunk chunk = TileEntity.GetChunk();
        //    Vector3i localChunkPos = TileEntity.localChunkPos;
        //}

        CurrentWindSpeed = EntityStats.WeatherSurvivalEnabled ? WeatherManager.theInstance.GetCurrentWindValue() : 15;

        if (CurrentWindSpeed < MinSpeed)
        {
            CurrentPower = 0;
        }
        else
        {
            CurrentPower = (ushort)(CurrentWindSpeed * WattPerMph);

        }
        if (CurrentPower > 0)
            RequiredPower = MaxPower = MaxOutput = CurrentPower;

        //Debug.Log("Current Windspeed: " + CurrentWindSpeed + " / Current Power: " + CurrentPower);
        
        if (animator == null && TileEntity != null)
            animator = TileEntity.BlockTransform.gameObject.GetComponent<Animator>();

        if (animator != null)
        {
            var speed = CurrentWindSpeed < MinSpeed ? 0 : CurrentWindSpeed / MaxSpeed;
            var change = Mathf.Abs(speed - lastChange);
            if (change > 0.1f || (CurrentPower == 0 && last != 0) || (last == 0 && CurrentPower != 0))
            {
                lastChange = speed;
                GameManager.Instance.StartCoroutine(ChangeSpeed(speed));
            }
        }

        if (last == CurrentPower) return;

        HandleOnOffSound();
        if (CurrentPower == 0)
        {
            GameManager.Instance.StartCoroutine(ChangeSpeed(0));
            HandleDisconnect();
        }
        else
            SendHasLocalChangesToRoot();
    }

    protected override void TickPowerGeneration()
    {
        //Debug.Log("Tick: " + MaxOutput);
        //if (CurrentWindSpeed == 0) return;
        //CurrentPower = MaxOutput;
        //Debug.Log("Current power: " + CurrentPower);
    }

    public override void HandleSendPower()
    {
        if (!IsOn)
        {
            if (animator != null && CurrentPower > 0)
            {
                GameManager.Instance.StartCoroutine(ChangeSpeed(0));
                CurrentPower = 0;
            }
            return;
        }
        if (Time.time > NextUpdateTime)
        {
            NextUpdateTime = Time.time + UpdateTime;
            CanPower();
        }
        
        if (CurrentPower < MaxPower)
            TickPowerGeneration();
        else if (CurrentPower > MaxPower)
            CurrentPower = MaxPower; 
        if (ShouldAutoTurnOff())
        {
            IsOn = false;
        }
        if (hasChangesLocal)
        {
            LastPowerUsed = 0;
            ushort power = (ushort)Mathf.Min(MaxOutput, CurrentPower);

            for (int index = 0; index < Children.Count; ++index)
            {
                ushort num = power;
                Children[index].HandlePowerReceived(ref power);
                LastPowerUsed += (ushort)(num - (uint)power);
            }
        }
        if (LastPowerUsed >= CurrentPower)
        {
            SendHasLocalChangesToRoot();
            CurrentPower = 0;
        }
        else
            CurrentPower -= LastPowerUsed;
    }

    public override void SetValuesFromBlock()
    {
        base.SetValuesFromBlock();

        Block block = Block.list[BlockID];
        if (block.Properties.Values.ContainsKey("MinWindSpeed"))
            MinSpeed = float.Parse(block.Properties.Values["MinWindSpeed"]);
        if (block.Properties.Values.ContainsKey("MaxWindSpeed"))
            MaxSpeed = float.Parse(block.Properties.Values["MaxWindSpeed"]);
        if (block.Properties.Values.ContainsKey("WattPerMph"))
            WattPerMph = float.Parse(block.Properties.Values["WattPerMph"]);
        
        this.RequiredPower = this.MaxPower = this.MaxOutput = 1;
        
    }

    protected bool ShouldClearPower()
    {
        return CurrentWindSpeed == 0;
    }

    protected override void HandleOnOffSound()
    {
        Vector3 vector3 = Position.ToVector3();
        Manager.BroadcastPlay(vector3, !isOn ? OffSound : OnSound);
        if (isOn )
            Manager.BroadcastPlay(vector3, IdleSound);
        else
            Manager.BroadcastStop(vector3, IdleSound);
    }

    protected override void RefreshPowerStats()
    {
        SlotCount = 0;
        MaxOutput = 1;
        if (MaxPower == 0)
            MaxPower = MaxOutput;
        if (RequiredPower != 0) return;
        this.RequiredPower = this.MaxOutput;
    }
}
