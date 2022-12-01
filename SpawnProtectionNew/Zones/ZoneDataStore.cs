using OpenMod.API;
using OpenMod.API.Ioc;
using OpenMod.API.Persistence;
using OpenMod.Core.Helpers;
using OpenMod.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpawnProtectionNew.Zones
{
    [Service]
    public interface IZoneDataStore
    {
        Task<IReadOnlyCollection<ZoneData>> GetZonesDataAsync();

        Task<ZoneData> GetZoneDataAsync(string zoneId);

        Task SetZoneDataAsync(ZoneData zone);
    }

    [ServiceImplementation(Lifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton)]
    public class PointDataStore : IZoneDataStore, IAsyncDisposable
    {
        const string c_ZonesKey = "zones";


        private readonly IOpenModComponent m_Component;
        private readonly IDataStore m_DataStore;


        private ZonesData m_CachedPointsData;
        private IDisposable m_FileChangeWatcher;
        private bool m_IsUpdating;


        public PointDataStore(IRuntime runtime, IDataStoreFactory dataStoreFactory)
        {
            m_Component = runtime;
            m_DataStore = dataStoreFactory.CreateDataStore(new DataStoreCreationParameters()
            {
#pragma warning disable 618
                ComponentId = "SpawnProtectionNew",
#pragma warning restore 618
                Suffix = "data",
                LogOnChange = true,
                WorkingDirectory = PluginHelper.GetWorkingDirectory(runtime, "SpawnProtectionNew")
            });

            m_CachedPointsData = null;
            m_FileChangeWatcher = null;

            AsyncHelper.RunSync(async () =>
            {
                m_CachedPointsData = await EnsurePointsDataCreatedAsync();
            });
        }


        private async Task<ZonesData> EnsurePointsDataCreatedAsync()
        {
            var created = false;
            if (await m_DataStore.ExistsAsync(c_ZonesKey) == false)
            {
                m_CachedPointsData = new ZonesData()
                {
                    Zones = GetDefaultZonesData()
                };

                await m_DataStore.SaveAsync(c_ZonesKey, m_CachedPointsData);
                created = true;
            }

            m_FileChangeWatcher = m_DataStore.AddChangeWatcher(c_ZonesKey, m_Component, () =>
            {
                if (m_IsUpdating == false)
                {
                    m_CachedPointsData = AsyncHelper.RunSync(LoadZonesDataFromDiskAsync);
                }

                m_IsUpdating = false;
            });

            if (created == false)
            {
                m_CachedPointsData = await LoadZonesDataFromDiskAsync();
            }

            return m_CachedPointsData;
        }

        private List<ZoneData> GetDefaultZonesData()
        {
            return new List<ZoneData>()
            {
                new ZoneData()
                {
                    Name = "bit",
                    Point = new SerializableVector3(0,0,0),
                    Range = 0.1f
                }
            };
        }

        private async Task<ZonesData> LoadZonesDataFromDiskAsync()
        {
            if (await m_DataStore.ExistsAsync(c_ZonesKey) == false)
            {
                m_CachedPointsData = new ZonesData()
                {
                    Zones = GetDefaultZonesData()
                };

                await m_DataStore.SaveAsync(c_ZonesKey, m_CachedPointsData);
                return m_CachedPointsData;
            }

            return await m_DataStore.LoadAsync<ZonesData>(c_ZonesKey) ?? new ZonesData()
            {
                Zones = GetDefaultZonesData()
            };
        }

        private Task<List<ZoneData>> GetZonesDataAsync()
        {
            return Task.FromResult(m_CachedPointsData.Zones?.ToList());
        }


        async Task<IReadOnlyCollection<ZoneData>> IZoneDataStore.GetZonesDataAsync()
        {
            var pointsData = await GetZonesDataAsync();
            return pointsData?.AsReadOnly();
        }
        public async Task<ZoneData> GetZoneDataAsync(string zoneName)
        {
            if (string.IsNullOrEmpty(zoneName))
            {
                throw new ArgumentException(nameof(zoneName));
            }

            var pointsData = await GetZonesDataAsync();
            return pointsData?.FirstOrDefault(p => p?.Name.Equals(zoneName, StringComparison.OrdinalIgnoreCase) ?? false);
        }
        public async Task SetZoneDataAsync(ZoneData pointData)
        {
            if (pointData == null)
            {
                throw new ArgumentNullException(nameof(pointData));
            }

            if (string.IsNullOrWhiteSpace(pointData.Name))
            {
                throw new ArgumentException($"Point data missing required property: {nameof(pointData.Name)}", nameof(pointData));
            }

            var pointsData = await GetZonesDataAsync() ?? GetDefaultZonesData();

            var match = new Predicate<ZoneData>(p => p?.Name.Equals(pointData.Name, StringComparison.OrdinalIgnoreCase) ?? false);

            var index = pointsData.FindIndex(match);
            pointsData.RemoveAll(match);

            if (index >= 0)
            {
                pointsData.Insert(index, pointData);
            }
            else
            {
                pointsData.Add(pointData);
            }

            m_CachedPointsData.Zones = pointsData;
            m_IsUpdating = true;

            await m_DataStore.SaveAsync(c_ZonesKey, m_CachedPointsData);
        }


        public async ValueTask DisposeAsync()
        {
            m_FileChangeWatcher?.Dispose();

            if (m_CachedPointsData == null)
            {
                throw new Exception("Tried to save null points data");
            }

            await m_DataStore.SaveAsync(c_ZonesKey, m_CachedPointsData);
        }
    }
}
