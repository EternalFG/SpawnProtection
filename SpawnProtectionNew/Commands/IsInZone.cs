using OpenMod.API.Plugins;
using OpenMod.Core.Commands;
using OpenMod.Unturned.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpawnProtectionNew.Commands
{
    [Command("isinzone")]
    [CommandDescription("Are you in the zone.")]
    [CommandSyntax("/isinzone")]
    [CommandActor(typeof(UnturnedUser))]
    public class IsInZone : OpenMod.Core.Commands.Command
    {
        private readonly IPluginAccessor<Plugin> m_pluginAccessor;
        public IsInZone(IServiceProvider serviceProvider, IPluginAccessor<Plugin> pluginAccessor) : base(serviceProvider)
        {
            m_pluginAccessor = pluginAccessor;
        }

        protected override async Task OnExecuteAsync()
        {
            UnturnedUser user = Context.Actor as UnturnedUser;

            var zone = await m_pluginAccessor.Instance.GetZoneInPositionStructureAsync(user.Player.Player.transform.position);
            if (zone.Item1)
            {
                await user.PrintMessageAsync("Вы находитесь в зоне", System.Drawing.Color.Coral);
            }
            else
            {
                await user.PrintMessageAsync("Вы не находитесь в зоне", color: System.Drawing.Color.Red);
            }
        }
    }
}
