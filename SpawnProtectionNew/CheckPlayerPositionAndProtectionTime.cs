using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using OpenMod.Unturned.Players;
using OpenMod.Unturned.RocketMod;
using System.Collections.Generic;
using UnityEngine;

namespace SpawnProtectionNew
{
    public class CheckPlayerPositionAndProtectionTime
    {
        private Dictionary<UnturnedPlayer, (Vector3, float)> _protectedPlayers;

        private float protectionTimeOff;
        private float DisableProtectionOnMoveRange;
        private readonly IStringLocalizer m_StringLocalizer;
        public bool Enabled { get; set; }
        public CheckPlayerPositionAndProtectionTime(Dictionary<UnturnedPlayer, (Vector3, float)> rotectedPlayers, IConfiguration configuration, IStringLocalizer stringLocalizer)
        {
            _protectedPlayers = rotectedPlayers;
            protectionTimeOff = configuration.GetSection("ProtectionTime").Get<float>();
            DisableProtectionOnMoveRange = configuration.GetSection("DisableProtectionOnMoveRange").Get<float>();
            m_StringLocalizer = stringLocalizer;
        }

        public async UniTask Start()
        {
            while (Enabled)
            {
                await UniTask.WaitForFixedUpdate();
                await UniTask.SwitchToMainThread();

                List<(UnturnedPlayer, string)> deleteProtections = new List<(UnturnedPlayer, string)>();

                foreach (var protectedPlayer in _protectedPlayers)
                {
                    if (Time.realtimeSinceStartup - protectedPlayer.Value.Item2 >= protectionTimeOff)
                    {
                        deleteProtections.Add((protectedPlayer.Key, "time_left"));
                    }
                    else if ((protectedPlayer.Value.Item1 - protectedPlayer.Key.Player.transform.position).sqrMagnitude >= DisableProtectionOnMoveRange * DisableProtectionOnMoveRange)
                    {
                        deleteProtections.Add((protectedPlayer.Key, "zone_left"));
                    }
                }

                if (deleteProtections.Count > 0)
                {
                    foreach (var deleteProtection in deleteProtections)
                    {
                        if (RocketModIntegration.IsRocketModInstalled())
                        {
                            var rocketPlayer = Rocket.Unturned.Player.UnturnedPlayer.FromPlayer(deleteProtection.Item1.Player);
                            if (rocketPlayer != null)
                            {
                                rocketPlayer.VanishMode = false;
                            }
                        }
                        switch (deleteProtection.Item2)
                        {
                            case "time_left":
                                await deleteProtection.Item1.PrintMessageAsync(m_StringLocalizer["timeleft"], System.Drawing.Color.RosyBrown);
                                break;
                            case "zone_left":
                                await deleteProtection.Item1.PrintMessageAsync(m_StringLocalizer["zoneleft"], System.Drawing.Color.RosyBrown);
                                break;
                        }
                        _protectedPlayers.Remove(deleteProtection.Item1);
                    }
                }

                await UniTask.SwitchToThreadPool();
            }
        }
    }
}
