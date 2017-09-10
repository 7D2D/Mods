using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Audio;
using Random = System.Random;

public class BlockWindmill : BlockSolarPanel
{
    private string LC;

    public BlockWindmill() : base()
    {
        this.LC = "solarpanel_idle";
    }

    public override TileEntityPowerSource CreateTileEntity(Chunk chunk)
    {
        if (this.slotItem == null)
            this.slotItem = ItemClass.GetItemClass(this.SlotItemName, false);
        TileEntityPowerSource entityPowerSource = new TileEntityPowerSource(chunk);
        entityPowerSource.PowerItemType = (PowerItem.PowerItemTypes)12;
        entityPowerSource.SlotItem = this.slotItem;
        //entityPowerSource.PowerItem = new WindmillPowerItem();
        return entityPowerSource;
    }
    
    IEnumerator ChangeSpeed(Animator animator, float targetSpeed)
    {

        if (animator == null)
        {
            Debug.Log("Animator null");
            yield break;
        }
        var speed = animator.GetFloat("Speed");
        float time = 0f;
        var totalTime = 10;
        if (speed == targetSpeed)
        {
            Debug.Log("Speed already 0 or matching at " + speed);
            yield break;
        }
        
        while (true)
        {

            if (animator == null)
            {
                yield break;
            }

            var newSpeed = Mathf.Lerp(speed, targetSpeed, time / totalTime);
            animator.SetFloat("Speed", newSpeed);

            time += Time.deltaTime;

            if (time > totalTime) break;
            yield return null;
        }
    }
    
    IEnumerator CheckAnimation(Animator animator, int _cIdx, Vector3i _blockPos, float MinSpeed, float MaxSpeed, float WattsPerMph)
    {

        float lastChange = float.MinValue;
        var ret = new WaitForSeconds(10);

        var missingCount = 0;
        //Debug.Log("Starting coroutine for Windmill " + _blockPos);
        while (true)
        {

            if (GameManager.Instance == null || GameManager.Instance.World == null)
            {
                //Debug.Log("Windmill world not active");
                yield break;
            }
            else
            {
                TileEntityPowerSource tileEntity = GameManager.Instance.World.GetTileEntity(_cIdx, _blockPos) as TileEntityPowerSource;
                if (tileEntity == null)
                {
                    Log.Out("Tile Entity missing for windmill count" + missingCount);
                    if (++missingCount > 5) break;
                }
                else if (animator != null)
                {
                    var CurrentWindSpeed = (float)tileEntity.MaxOutput / WattsPerMph;
                    var speed = CurrentWindSpeed < MinSpeed ? 0 : CurrentWindSpeed / MaxSpeed;
                    var change = Mathf.Abs(speed - lastChange);
                    if (change > 0.1f || CurrentWindSpeed == 0)
                    {
                        lastChange = speed;
                        GameManager.Instance.StartCoroutine(ChangeSpeed(animator, speed));
                    }
                }
            }

            yield return ret;
        }

        //Debug.Log("Ending coroutine for Windmill " + _blockPos);

    }


    public override void OnBlockRemoved(WorldBase world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
    {
        base.OnBlockRemoved(world, _chunk, _blockPos, _blockValue);
        Manager.BroadcastStop(_blockPos.ToVector3(), this.LC);
    }

    public override void OnBlockUnloaded(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        base.OnBlockUnloaded(_world, _clrIdx, _blockPos, _blockValue);
        Manager.Stop(_blockPos.ToVector3(), this.LC);
    }

    public override void OnBlockEntityTransformAfterActivated(WorldBase _world, Vector3i _blockPos, int _cIdx, BlockValue _blockValue, BlockEntityData _ebcd)
    {
        
        base.OnBlockEntityTransformAfterActivated(_world, _blockPos, _cIdx, _blockValue, _ebcd);
        SetTag(_ebcd.transform, _ebcd.transform, "T_Block");


        //this is a really ugly way of doing it but there's no update tick on clients for dedi support
        if (Network.isClient)
        {
            Block block = Block.list.FirstOrDefault(d=> d != null && d.GetBlockName() == "Windmill"); //.list[BlockID];

            if (block == null)
            {
                Debug.Log("Can't find windmill block");
                return;
            }
            var MinSpeed = 0f;
            var MaxSpeed = 0f;
            var WattPerMph = 0f;
            if (block.Properties.Values.ContainsKey("MinWindSpeed"))
                MinSpeed = float.Parse(block.Properties.Values["MinWindSpeed"]);
            if (block.Properties.Values.ContainsKey("MaxWindSpeed"))
                MaxSpeed = float.Parse(block.Properties.Values["MaxWindSpeed"]);
            if (block.Properties.Values.ContainsKey("WattPerMph"))
                WattPerMph = float.Parse(block.Properties.Values["WattPerMph"]);
            
            var animator = _ebcd.transform.gameObject.GetComponent<Animator>();
            GameManager.Instance.StartCoroutine(CheckAnimation(animator, _cIdx, _blockPos, MinSpeed, MaxSpeed, WattPerMph));
        }
    }
    
    public override BlockActivationCommand[] GetBlockActivationCommands(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        var ret = base.GetBlockActivationCommands(_world, _blockValue, _clrIdx, _blockPos, _entityFocusing);
        return ret;
    }

    public override string GetActivationText(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        var ret = base.GetActivationText(_world, _blockValue, _clrIdx, _blockPos, _entityFocusing);
        return ret;
    }
  
    private void SetTag(Transform root, Transform t, string tag)
    {
        t.tag = tag;
        foreach (Transform tran in t)
            SetTag(root, tran, tag);

        if (root != t)
        {
            var go = t.gameObject.gameObject;
            var c = go.GetComponent<RootTransformRefParent>();
            if (c == null)
            {
                c = go.AddComponent<RootTransformRefParent>();
                c.RootTransform = root;
            }
        }
    }

    protected override string GetPowerSourceIcon()
    {
        return "electric_solar";
    }

}

