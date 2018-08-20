using System;
using UnityEngine;

public class Mk2ExcavatorWindow : BaseMachineWindow
{
    
    public const string InterfaceName = "FlexibleGames.Mk2ExcavatorWindow";

    public const string InterfaceAlterHeight = "AlterHeight";
    public const string InterfaceAlterRadius = "AlterRadius";
    public const string InterfaceAlterDigState = "AlterDigState";
    public const string InterfaceAlterDropState = "AlterDropState";

    public static bool dirty;
    public static bool networkredraw;

    public override void UpdateMachine(SegmentEntity targetEntity)
    {
        Mk2Excavator machine = targetEntity as Mk2Excavator;

        if (machine == null)
        {
            return;
        }
        if (networkredraw)
        {
            this.manager.RedrawWindow();
        }
        if (!dirty)
            return;

        dirty = false;
    }

    public static bool AlterHeight(Mk2Excavator machine, int data)
    {
        machine.mnDigSizeY = data;
        machine.UpdateDigSettings();
        machine.MarkDirtyDelayed();

        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand("FlexibleGames.Mk2ExcavatorWindow", "AlterHeight", data.ToString(), null, machine, 0.0f);

        return true;
    }

    public static bool AlterRadius(Mk2Excavator machine, int data)
    {
        machine.mnDigSizeX = data;
        machine.mnDigSizeZ = data;
        machine.UpdateDigSettings();
        machine.MarkDirtyDelayed();

        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand("FlexibleGames.Mk2ExcavatorWindow", "AlterRadius", data.ToString(), null, machine, 0.0f);

        return true;
    }

    public static bool AlterDigState(Mk2Excavator machine, string data)
    {
        Mk2Excavator.ExcavateState newdigstate = Mk2Excavator.ExcavateState.ClearGarbage;
        switch (data)
        {
            case "ClearAll": newdigstate = Mk2Excavator.ExcavateState.ClearAll; break;
            case "ClearGarbage": newdigstate = Mk2Excavator.ExcavateState.ClearGarbage; break;
            case "ClearOre": newdigstate = Mk2Excavator.ExcavateState.ClearOre; break;
            case "Error": Debug.Log("Mk2Excavator: Error while processing NewDigState"); break;
            default: break;
        }
        machine.eExcavateState = newdigstate;
        machine.MarkDirtyDelayed();

        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand("FlexibleGames.Mk2ExcavatorWindow", "AlterDigState", data, null, machine, 0.0f);

        return true;
    }

    public static bool AlterDropState(Mk2Excavator machine, string data)
    {
        Mk2Excavator.DropState newdropstate = Mk2Excavator.DropState.DropSome;
        switch (data)
        {
            case "DropSome": newdropstate = Mk2Excavator.DropState.DropSome; break;
            case "DropNone": newdropstate = Mk2Excavator.DropState.DropNone; break;
            case "DropAll": newdropstate = Mk2Excavator.DropState.DropAll; break;
            case "DropOre": newdropstate = Mk2Excavator.DropState.DropOre; break;
            case "Error": Debug.Log("Mk2Excavator: Error while processing NewDropState"); break;
            default: break;
        }
        machine.eDropState = newdropstate;
        machine.MarkDirtyDelayed();

        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand("FlexibleGames.Mk2ExcavatorWindow", "AlterDropState", data, null, machine, 0.0f);

        return true;
    }

    public static NetworkInterfaceResponse HandleNetworkCommand(Player player, NetworkInterfaceCommand nic)
    {        
        Mk2Excavator machine = nic.target as Mk2Excavator;
        string key = nic.command;

        if (key != null)
        {
            int data;
            if (key == "AlterHeight")
            {
                int.TryParse(nic.payload ?? "1", out data);
                Mk2ExcavatorWindow.AlterHeight(machine, data);
            }
            else if (key == "AlterRadius")
            {
                int.TryParse(nic.payload ?? "1", out data);
                Mk2ExcavatorWindow.AlterRadius(machine, data);
            }
            else if (key == "AlterDigState")
            {
                Mk2ExcavatorWindow.AlterDigState(machine, nic.payload);
            }
            else if (key == "AlterDropState")
            {
                Mk2ExcavatorWindow.AlterDropState(machine, nic.payload);
            }
        }
        return new NetworkInterfaceResponse()
        {
            entity = (SegmentEntity)machine,
            inventory = player.mInventory
        };
    }

}

