using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Audio;
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

    public override bool CanPlaceBlockAt(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, bool _bOmitCollideCheck)
    {
        if (!base.CanPlaceBlockAt(_world, _clrIdx, _blockPos, _blockValue, _bOmitCollideCheck))
            return false;

        return true;
    }

    public override void OnBlockEntityTransformAfterActivated(WorldBase _world, Vector3i _blockPos, int _cIdx, BlockValue _blockValue, BlockEntityData _ebcd)
    {
        //Debug.Log("DEBUG " + _ebcd.transform.gameObject.name);
        //DebugGO(_ebcd.transform, 0);
        //Debug.Log("END DEBUG " + _ebcd.transform.gameObject.name);
        //Debug.Log("");

        base.OnBlockEntityTransformAfterActivated(_world, _blockPos, _cIdx, _blockValue, _ebcd);
        SetTag(_ebcd.transform, _ebcd.transform, "T_Block");

        //TileEntityPowerSource tileEntity = _world.GetTileEntity(_cIdx, _blockPos) as TileEntityPowerSource;
        //if (tileEntity == null)
        //{
        //    Debug.Log("Tile entity source was null");
        //    ChunkCluster chunkCluster = _world.ChunkClusters[_cIdx];
        //    if (chunkCluster == null) return;
        //    Chunk chunkFromWorldPos = (Chunk) chunkCluster.GetChunkFromWorldPos(_blockPos);
        //    if (chunkFromWorldPos == null) return;
        //    tileEntity = this.CreateTileEntity(chunkFromWorldPos);
        //    tileEntity.localChunkPos = World.toBlock(_blockPos);
        //    chunkFromWorldPos.AddTileEntity((TileEntity) tileEntity);
        //    Log.Out("DEBUG !!!!!!!   TileEntityPowerSource not found (" + (object) _blockPos + ")");
        //}
        //else
        //{
        //    Debug.Log("Tile entity source: " + tileEntity.BlockTransform.name);
        //}
        //tileEntity.PowerItem = new WindmillPowerItem();

        //PowerSource powerSource = tileEntity.GetPowerItem() as PowerSource;
        //if (powerSource != null)
        //{

        //}

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
    public static void DebugGO(Transform tran, int Depth)
    {

        Component[] comp = tran.GetComponents<Component>();

        //  Debug.Log(tran.name + "=" + comp.Length);
        foreach (Component c in comp)
        {
            string offset = "";
            for (int x = 0; x < Depth; x++)
            {
                offset += "-";
            }
            Debug.Log(offset + " Comp: " + c.name + " (" + c.GetType().Name + ")");

        }

        foreach (Component c in comp)
        {
            if (c.GetType() == typeof(Transform))
            {
                foreach (Transform t in ((Transform)c))
                    DebugGO(t, Depth + 1);
            }
        }

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

}

