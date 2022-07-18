using System.Collections;
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
    private readonly ConfigEntry<KeyCode> _toggle;
    private bool _isEnabled;

    public Plugin()
    {
        _toggle = Config.Bind("General", "Toggle", KeyCode.F1,
            "The Unity's KeyCode that will be used to toggle auto pickup in game.");

        _distance = Config.Bind("General", "Distance", 2.0f,
            "The distance the player will be able to auto pickup drops.");
    }

    private void Awake()
    {
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void Start()
    {
        StartCoroutine(DoPickup());
    }

    private void Update()
    {
        if (!Input.GetKeyDown(_toggle.Value)) return;

        _isEnabled = !_isEnabled;
        NotificationManager.manage.createChatNotification(
            $"AutoPick is now {(_isEnabled ? "activated" : "deactivated")}.");
    }

    private IEnumerator DoPickup()
    {
        while (true)
            if (!_isEnabled || (FindObjectsOfType<DroppedItem>().Length <= 0 && Inventory.inv.localChar == null))
            {
                yield return 0;
            }
            else
            {
                foreach (var pickup in FindObjectsOfType<DroppedItem>().ToList().Where(pickup =>
                             WorldManager.manageWorld.itemsOnGround.Contains(pickup) &&
                             Vector3.Distance(Inventory.inv.localChar.GetComponent<CharMovement>().transform.position,
                                 pickup.transform.position) <= _distance.Value))
                {
                    if (!Inventory.inv.addItemToInventory(pickup.myItemId, pickup.stackAmount)) continue;
                    SoundManager.manage.play2DSound(SoundManager.manage.pickUpItem);
                    WorldManager.manageWorld.itemsOnGround.Remove(pickup);
                    NetworkServer.Destroy(pickup.gameObject);
                }

                yield return new WaitForSeconds(0.4f);
            }
    }
}