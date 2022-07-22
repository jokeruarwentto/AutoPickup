using System;
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
    private readonly ConfigEntry<bool> _enabled;
    private readonly ConfigEntry<float> _refreshTime;
    private readonly ConfigEntry<KeyCode> _settingsKey;

    private bool _enabledMenu;
    private float _timer;
    private float _fps = 0;

    public Plugin()
    {
        _enabled = Config.Bind("General", "Enabled", true,
            "Whether to enable by default the auto pickup feature.");

        _settingsKey = Config.Bind("General", "SettingsKey", KeyCode.F1,
            "The Unity's key that will show the AutoPickup settings.");

        _distance = Config.Bind("General", "Distance", 8.0f,
            "The distance that will auto pickup dropped items.");

        _refreshTime = Config.Bind("General", "RefreshTime", 1.0f,
            "The time that will refresh the dropped items and try to pickup them.");
    }

    public void Start()
    {
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        StartCoroutine(FramePerSecondsRoutine());
    }

    private IEnumerator FramePerSecondsRoutine()
    {
        while (true)
        {
            _fps = 1 / Time.deltaTime;
            yield return new WaitForSeconds(0.6f);
        }
    }
    
    private void Update()
    {
        if (NetworkMapSharer.share.localChar == null) return;

        if (Input.GetKeyDown(_settingsKey.Value))
        {
            _enabledMenu = !_enabledMenu;
            CameraController.control.lockCamera(_enabledMenu);
            CameraController.control.cameraShowingSomething = _enabledMenu;
            Cursor.visible = _enabledMenu;
        }

        if (_enabled.Value)
        {
            _timer += Time.deltaTime;

            if (_timer >= _refreshTime.Value)
            {
                foreach (var pickup in FindObjectsOfType<DroppedItem>().Where(item =>
                             Vector3.Distance(NetworkMapSharer.share.localChar.transform.position,
                                 item.transform.position) <=
                             _distance.Value))
                {
                    if (!Inventory.inv.addItemToInventory(pickup.myItemId, pickup.stackAmount)) continue;
                    SoundManager.manage.play2DSound(SoundManager.manage.pickUpItem);

                    if (WorldManager.manageWorld.itemsOnGround.Contains(pickup))
                        WorldManager.manageWorld.itemsOnGround.Remove(pickup);

                    NetworkServer.UnSpawn(pickup.transform.root.gameObject);
                    NetworkServer.Destroy(pickup.transform.root.gameObject);
                }

                _timer = 0;
            }
        }
    }

    private void OnDestroy()
    {
        if (_enabledMenu)
        {
            CameraController.control.lockCamera(false);
            CameraController.control.cameraShowingSomething = false;
            Cursor.visible = false;
        }

        StopAllCoroutines();
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is destroyed!");
    }

    private void OnGUI()
    {
        if (!_enabledMenu) return;

        GUI.Box(new Rect(10.0f, Screen.height / 2.5f, 256.0f, 118.0f), "AutoPickup Settings");

        _enabled.Value = GUI.Toggle(new Rect(18.0f, Screen.height / 2.5f + 22.0f, 256.0f, 22.0f), _enabled.Value,
            " Active");

        GUI.Label(new Rect(18.0f, Screen.height / 2.5f + 22.0f * 2.0f, 100.0f, 22.0f),
            $"Distance: {_distance.Value}");
        _distance.Value = (float) Math.Floor(GUI.HorizontalSlider(
            new Rect(108.0f, Screen.height / 2.5f + 22.0f * 2.3f, 256.0f - 108.0f, 22.0f), _distance.Value, 1.0f,
            128.0f));

        GUI.Label(new Rect(18.0f, Screen.height / 2.5f + 22.0f * 3.0f, 100.0f, 22.0f),
            $"Refresh: {_refreshTime.Value:0.#}s");
        _refreshTime.Value = GUI.HorizontalSlider(
            new Rect(108.0f, Screen.height / 2.5f + 22.0f * 3.3f, 256.0f - 108.0f, 22.0f), _refreshTime.Value, 0.0f,
            2.0f);

        GUI.Label(new Rect(18.0f, Screen.height / 2.5f + 22.0f * 4.0f, 100.0f, 22.0f),
            $"FPS: {_fps:0.#}");
    }
}