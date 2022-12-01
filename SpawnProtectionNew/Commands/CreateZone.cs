using OpenMod.Core.Commands;
using OpenMod.Unturned.Users;
using SpawnProtectionNew.Zones;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace SpawnProtectionNew.Commands
{
    [Command("createzone")]
    [CommandDescription("Create a protection zone.")]
    [CommandSyntax("/createzone [name] [radius]")]
    [CommandActor(typeof(UnturnedUser))]
    public class CreateZone : OpenMod.Core.Commands.Command
    {
        private readonly IZoneDataStore m_ZoneDataStore;
        public CreateZone(IServiceProvider serviceProvider, IZoneDataStore zoneDataStore) : base(serviceProvider)
        {
            m_ZoneDataStore = zoneDataStore;
        }

        protected override async Task OnExecuteAsync()
        {
            UnturnedUser user = Context.Actor as UnturnedUser;

            if (Context.Parameters.Length == 2)
            {
                var name = await Context.Parameters.GetAsync<string>(0);
                var radius = await Context.Parameters.GetAsync<float>(1);
                await m_ZoneDataStore.SetZoneDataAsync(new ZoneData(new SerializableVector3(user.Player.Player.transform.position), name, radius));
                await user.PrintMessageAsync("Зона установлена.", Color.Green);
            }
            else
            {
                await user.PrintMessageAsync("/createzone [name] [radius]", Color.Red);
            }
        }
    }
}
