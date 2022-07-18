using BepInEx;
using BepInEx.Configuration;
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
        var pickups = FindObjectsOfType<DroppedItem>();

        if (pickups is not { Length: > 0 })
            return;

        foreach (var pickup in pickups)
        {
            var pickupPosition = pickup.transform.position;
            var playerPosition = Inventory.inv.localChar.GetComponent<CharMovement>().transform.position;

            if (Vector3.Distance(playerPosition, pickupPosition) > _distance.Value)
                continue;

            if (Inventory.inv.addItemToInventory(pickup.myItemId, pickup.stackAmount))
            {
                SoundManager.manage.play2DSound(SoundManager.manage.pickUpItem);
                pickup.pickUp();
                Destroy(pickup.gameObject);
            }
        }
    }
}