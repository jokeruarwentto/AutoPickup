using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using Mirror;
using UnityEngine;

namespace AutoPickup;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private static readonly float[] _distances = {2.0f, 4.0f, 8.0f, 16.0f, 32.0f, 64.0f, 128.0f, 256.0f, 512.0f};
    private readonly ConfigEntry<float> _distance;
    private readonly ConfigEntry<KeyCode> _distanceChangeKey;
    private readonly ConfigEntry<KeyCode> _toggle;
    private int _currentDistance;
    private bool _isEnabled;
    private float _timer;

    public Plugin()
    {
        _toggle = Config.Bind("General", "Toggle", KeyCode.F1,
            "The Unity's KeyCode that will be used to toggle auto pickup in game.");

        _distanceChangeKey = Config.Bind("General", "DistanceKey", KeyCode.F2,
            "The Unity's KeyCode that will be used to change the distance directly in game.");

        _distance = Config.Bind("General", "Distance", _distances[_currentDistance],
            "The distance the player will be able to auto pickup drops.");
    }

    private void Awake()
    {
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void Update()
    {
        if (Input.GetKeyDown(_toggle.Value))
        {
            _isEnabled = !_isEnabled;
            NotificationManager.manage.createChatNotification(
                $"AutoPickup is now {(_isEnabled ? "activated" : "deactivated")}.");
        }

        if (Input.GetKeyDown(_distanceChangeKey.Value))
        {
            if (_currentDistance >= _distances.Length)
                _currentDistance = 0;
            else
                _currentDistance += 1;

            _distance.Value = _currentDistance;
            _distance.ConfigFile.Save();

            NotificationManager.manage.createChatNotification(
                $"You changed the AutoPickup distance to {_distance.Value}.");
        }

        _timer += Time.deltaTime;
        if (!_isEnabled || !(_timer >= 1.0f)) return;

        foreach (var pickup in FindObjectsOfType<DroppedItem>()
                     .Where(item =>
                         WorldManager.manageWorld.itemsOnGround.Contains(item) &&
                         Vector3.Distance(Inventory.inv.localChar.GetComponent<CharMovement>().transform.position,
                             item.transform.position) <= 64.0f))
        {
            if (!Inventory.inv.addItemToInventory(pickup.myItemId, pickup.stackAmount)) continue;
            SoundManager.manage.play2DSound(SoundManager.manage.pickUpItem);
            WorldManager.manageWorld.itemsOnGround.Remove(pickup);
            NetworkServer.Destroy(pickup.gameObject);
        }

        _timer = 0;
    }
}