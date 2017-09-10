using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Audio;
using System.Runtime.CompilerServices;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class WindmillPowerItem : PowerSource 
{
    private float Frequency = 45f;
    private float Amplitude = 0.9f;
    private float HeightAdvantage = 0.25f;

    private float CurrentWindSpeed;
    private string IdleSound;
    private ulong NextUpdateTime = 0;
    private ulong NextWindUpdateTime = 0;

    private int UpdateTimeInSeconds = 60;

    private float MinSpeed = 10;
    private float MaxSpeed = 20;
    private float WattPerMph = 2;

    private Animator animator;

    private float lastChange = 99999f;
    private ushort last;


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

        if (GameManager.IsDedicatedServer) yield break;

        var myCounter = ++updateCounter;
        float time = 0f;
        var totalTime = 10;
        var speed = animator.GetFloat("Speed");

        if (animator == null) yield break;
        while (true)
        {
            var newSpeed = Mathf.Lerp(speed, targetSpeed, time / totalTime);
            animator.SetFloat("Speed", newSpeed);

            time += Time.deltaTime;

            if (time > totalTime || myCounter != updateCounter) break;
            yield return null;
        }
    }
    
    
    private void CanPower()
    {
        if (GameManager.Instance == null || GameManager.Instance.World == null)
        {
            Debug.Log("World missing");
            return;
        }
        last = CurrentPower;
        if (TileEntity == null )
        {
            return;
        }

        if (GameManager.Instance.World.worldTime > NextWindUpdateTime || (NextWindUpdateTime - GameManager.Instance.World.worldTime) > 1000)
        {
            //random noise based on x/z location
            var pos = TileEntity.ToWorldPos();
            var noise = Mathf.PerlinNoise((GameManager.Instance.World.worldTime + (ulong) pos.x) * (Amplitude / Frequency), pos.z * (Amplitude / Frequency));
            //height advantage based on y position
            var height = (pos.y / 255f) * HeightAdvantage;
            CurrentWindSpeed = MaxSpeed * (noise + height); //(EntityStats.WeatherSurvivalEnabled && WeatherManager.theInstance != null) ? WeatherManager.theInstance.GetCurrentWindValue() : 15;
            NextWindUpdateTime = GameManager.Instance.World.worldTime + (ulong)((16.666) * UpdateTimeInSeconds);
            //Debug.Log("Wind: " + CurrentWindSpeed + " y-pos: " + pos.y);
        }

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
        UpdateAnimation();


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

    
    private void UpdateAnimation()
    {
      
        if (!GameManager.IsDedicatedServer && animator == null && TileEntity != null)
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

    }
    
    protected override void TickPowerGeneration()
    {

    }

    public override void HandleSendPower()
    {
        if (GameManager.Instance == null || GameManager.Instance.World == null) return;

        if (!IsOn)
        {
            if (animator != null && CurrentPower > 0)
            {
                GameManager.Instance.StartCoroutine(ChangeSpeed(0));
                CurrentPower = 0;
            }
            return;
        }
        if (GameManager.Instance.World.worldTime > NextUpdateTime || (NextUpdateTime - GameManager.Instance.World.worldTime) > 1000)
        {
            NextUpdateTime = GameManager.Instance.World.worldTime + (ulong)((16.666) * 2);
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
