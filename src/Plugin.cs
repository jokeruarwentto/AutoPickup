using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using Mirror;
using UnityEngine;

namespace AutoPickup;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private readonly ConfigEntry<float> _distance;

    public Plugin()
    {
        _distance = Config.Bind("General", "Distance", 2.0f,
            "The distance the player will be able to auto pickup drops.");
    }

    private void Awake()
    {
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void Update()
    {
        if (FindObjectsOfType<DroppedItem>().Length <= 0 && Inventory.inv.localChar == null)
            return;

        foreach (var pickup in FindObjectsOfType<DroppedItem>().ToList().Where(pickup =>
                     WorldManager.manageWorld.itemsOnGround.Contains(pickup) &&
                     Vector3.Distance(Inventory.inv.localChar.GetComponent<CharMovement>().transform.position,
                         pickup.transform.position) <= _distance.Value))
        {
            if (!Inventory.inv.addItemToInventory(pickup.myItemId, pickup.stackAmount)) continue;

            WorldManager.manageWorld.itemsOnGround.Remove(pickup);
            NetworkServer.Destroy(pickup.gameObject);
        }
    }
}