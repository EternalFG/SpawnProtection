using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using OpenMod.API.Eventing;
using OpenMod.API.Plugins;
using OpenMod.Core.Commands.Events;
using OpenMod.Core.Helpers;
using OpenMod.Unturned.Players;
using OpenMod.Unturned.Players.Connections.Events;
using OpenMod.Unturned.Players.Equipment.Events;
using OpenMod.Unturned.Players.Life.Events;
using OpenMod.Unturned.Plugins;
using OpenMod.Unturned.RocketMod;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using SpawnProtectionNew.Extension;
using SpawnProtectionNew.Zones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

[assembly: PluginMetadata("SpawnProtectionNew")]
namespace SpawnProtectionNew
{
    public class Plugin : OpenModUnturnedPlugin
    {
        private readonly IZoneDataStore m_ZoneDataStore;
        private readonly IStringLocalizer m_StringLocalizer;
        public Dictionary<UnturnedPlayer, (Vector3, float)> ProtectedPlayers = new Dictionary<UnturnedPlayer, (Vector3, float)>();

        public Plugin(IServiceProvider serviceProvider, IZoneDataStore zoneDataStore, IStringLocalizer stringLocalizer) : base(serviceProvider)
        {
            m_ZoneDataStore = zoneDataStore;
            m_StringLocalizer = stringLocalizer;
        }

        protected override UniTask OnLoadAsync()
        {
            VehicleManager.onEnterVehicleRequested += onEnterVehicleRequested;
            CheckPlayerPositionAndProtectionTime checkPlayerPositionAndProtectionTime = new CheckPlayerPositionAndProtectionTime(ProtectedPlayers, Configuration, m_StringLocalizer);

            AsyncHelper.RunSync(() =>
            {
                checkPlayerPositionAndProtectionTime.Enabled = true;
                checkPlayerPositionAndProtectionTime.Start().Forget();
                return Task.CompletedTask;
            });


            return UniTask.CompletedTask;
        }



        protected override UniTask OnUnloadAsync()
        {
            VehicleManager.onEnterVehicleRequested -= onEnterVehicleRequested;
            return UniTask.CompletedTask;
        }

        private void onEnterVehicleRequested(Player player, InteractableVehicle vehicle, ref bool shouldAllow)
        {
            foreach (var protectedPlayer in ProtectedPlayers)
            {
                if (player.channel.owner.playerID.steamID.m_SteamID == protectedPlayer.Key.SteamId.m_SteamID)
                {
                    ProtectedPlayers.Remove(protectedPlayer.Key);
                    if (RocketModIntegration.IsRocketModInstalled())
                    {
                        Rocket.Unturned.Player.UnturnedPlayer.FromPlayer(player).VanishMode = false;
                    }
                    break;
                }
            }
        }

        public async Task<(bool, ZoneData)> GetZoneInPositionStructureAsync(Vector3 position)
        {
            var zones = await m_ZoneDataStore.GetZonesDataAsync();
            ZoneData zoneData = zones.FirstOrDefault(z => position.IsInRange(z.Point.Position, z.SqrRange));

            return (zoneData != null, zoneData);
        }
    }

    public class Events : IEventListener<UnturnedPlayerDamagingEvent>, IEventListener<CommandExecutingEvent>, IEventListener<UnturnedPlayerItemEquippingEvent>,
        IEventListener<UnturnedPlayerSpawnedEvent>, IEventListener<UnturnedPlayerDisconnectedEvent>
    {
        private readonly IPluginAccessor<Plugin> m_pluginAccessor;
        private readonly IConfiguration m_Configuration;
        private readonly IStringLocalizer m_StringLocalizer;
        public Events(IPluginAccessor<Plugin> pluginAccessor, IConfiguration configuration, IStringLocalizer stringLocalizer)
        {
            m_pluginAccessor = pluginAccessor;
            m_Configuration = configuration;
            m_StringLocalizer = stringLocalizer;
        }

        public Task HandleEventAsync(object sender, UnturnedPlayerDamagingEvent @event)
        {
            foreach (var protectedPlayer in m_pluginAccessor.Instance.ProtectedPlayers)
            {
                if (protectedPlayer.Key.SteamId.m_SteamID == @event.Player.SteamId.m_SteamID)
                {
                    @event.IsCancelled = true;

                    break;
                }
            }
            return Task.CompletedTask;
        }

        public async Task HandleEventAsync(object sender, CommandExecutingEvent @event)
        {
            if (@event.Actor is UnturnedUser user)
            {
                var zone = await m_pluginAccessor.Instance.GetZoneInPositionStructureAsync(user.Player.Player.transform.position);

                if (zone.Item1)
                {

                    if (m_Configuration.GetSection("BanCommandsInProtectionZone").Get<List<string>>().Any(c => c.Equals(@event.CommandContext.CommandAlias)))
                    {
                        @event.IsCancelled = true;
                        await user.PrintMessageAsync(m_StringLocalizer["commandexecuting"], System.Drawing.Color.Red);
                    }


                }
            }


        }

        public async Task HandleEventAsync(object sender, UnturnedPlayerItemEquippingEvent @event)
        {
            foreach (var protectedPlayer in m_pluginAccessor.Instance.ProtectedPlayers)
            {
                if (@event.Player.SteamId.m_SteamID == protectedPlayer.Key.SteamId.m_SteamID)
                {
                    m_pluginAccessor.Instance.ProtectedPlayers.Remove(protectedPlayer.Key);
                    if (RocketModIntegration.IsRocketModInstalled())
                    {
                        Rocket.Unturned.Player.UnturnedPlayer.FromPlayer(@event.Player.Player).VanishMode = false;
                    }
                    await @event.Player.PrintMessageAsync(m_StringLocalizer["playeritemequipping"], System.Drawing.Color.RosyBrown);
                    break;
                }
            }

        }

        public async Task HandleEventAsync(object sender, UnturnedPlayerSpawnedEvent @event)
        {
            try
            {
                if (BarricadeManager.tryGetBed(@event.Player.SteamId, out Vector3 position, out byte angle))
                {
                    if ((@event.Player.Player.transform.position - position).sqrMagnitude < 2.25)
                    {
                        return;
                    }
                }
                if (m_pluginAccessor.Instance.ProtectedPlayers.TryGetValue(@event.Player, out (Vector3, float) _))
                {
                    return;
                }
                m_pluginAccessor.Instance.ProtectedPlayers.Add(@event.Player, (@event.Player.Player.transform.position, Time.realtimeSinceStartup));
                if (RocketModIntegration.IsRocketModInstalled())
                {
                    Rocket.Unturned.Player.UnturnedPlayer.FromPlayer(@event.Player.Player).VanishMode = true;
                }
                await @event.Player.PrintMessageAsync(m_StringLocalizer["playerspawned"], System.Drawing.Color.Azure);
            }
            catch (Exception)
            {

            }
            
        }

        public Task HandleEventAsync(object sender, UnturnedPlayerDisconnectedEvent @event)
        {
            m_pluginAccessor.Instance.ProtectedPlayers.Remove(@event.Player);
            return Task.CompletedTask;
        }
    }
}
