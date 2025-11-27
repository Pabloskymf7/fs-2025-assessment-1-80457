using fs_2025_assessment_1_80457.Models;
using Microsoft.Extensions.Caching.Memory;

namespace fs_2025_assessment_1_80457.Services
{
    public class StationService: IStationService
    {
        private readonly IStationRepository _repository;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "StationsListCache";

        public StationService(IStationRepository repository, IMemoryCache cache)
        {
            _repository = repository;
            _cache = cache;
        }

        // ===========================================
        // MÉTODOS DE MUTACIÓN (Pasa directamente al Repositorio)
        // ===========================================
        public void AddStation(Bike station)
        {
            _repository.Add(station);
            _cache.Remove(CacheKey);
        }
        public bool UpdateStation(int number, Bike station)
        {
            var result = _repository.Update(number, station);
            if (result) _cache.Remove(CacheKey);
            return result;
        }
        public bool DeleteStation(int number)
        {
            var result = _repository.Delete(number);
            if (result) _cache.Remove(CacheKey);
            return result;
        }

        // ===========================================
        // MÉTODOS DE CONSULTA
        // ===========================================

        public Bike? GetStationByNumber(int number)
        {
            // Podríamos obtener la lista completa del caché si la necesitamos para otros filtros,
            // pero para una búsqueda simple por clave, es más directo ir al repositorio.
            return _repository.GetByNumber(number);
        }

        public object GetSummary()
        {
            // La lógica de resumen no se cachea si no es necesario, se calcula sobre los datos actuales.
            var stations = _repository.GetAll();
            return new
            {
                TotalStations = stations.Count(),
                TotalAvailableBikes = stations.Sum(s => s.available_bikes),
                TotalBikeStands = stations.Sum(s => s.bike_stands), // stands ocupados
                                                                    // Total Docks (TotalCapacity)
                TotalDocks = stations.Sum(s => s.available_bike_stands) + stations.Sum(s => s.available_bikes),
                // Cuentas por estado (OPEN/CLOSED). Usar valor por defecto si status es nulo
                StatusCounts = stations.GroupBy(s => (s.status ?? "UNKNOWN").ToUpperInvariant())
                    .Select(g => new { Status = g.Key, Count = g.Count() })
            };
        }

        public IEnumerable<Bike> GetStationsAdvanced(
            string? q, string? status, int? minBikes,
            string? sort, string? dir, int page, int pageSize)
        {
            // 1. Lógica de Caching: Obtener la lista completa
            if (!_cache.TryGetValue(CacheKey, out IEnumerable<Bike>? stations))
            {
                // Si no está en caché, obtenemos los datos frescos del repositorio
                stations = _repository.GetAll().ToList();

                // Guardar la lista completa de estaciones en caché por 5 minutos
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                _cache.Set(CacheKey, stations, cacheEntryOptions);
            }

            // Si el background service actualiza el repositorio, la próxima vez que se llame
            // a GetAll, obtendrá los datos nuevos y los recacheará.

            var query = stations.AsQueryable();

            // 2. FILTRADO Y BÚSQUEDA
            if (!string.IsNullOrEmpty(q))
            {
                string lowerQ = q.ToLowerInvariant();
                query = query.Where(s =>
                    (!string.IsNullOrEmpty(s.name) && s.name.ToLowerInvariant().Contains(lowerQ)) ||
                    (!string.IsNullOrEmpty(s.address) && s.address.ToLowerInvariant().Contains(lowerQ)));
            }

            if (!string.IsNullOrEmpty(status))
            {
                // Usar comparación segura y sin distinguir mayúsculas/minúsculas para evitar NRE
                query = query.Where(s => !string.IsNullOrEmpty(s.status) &&
                                          string.Equals(s.status, status, StringComparison.OrdinalIgnoreCase));
            }

            if (minBikes.HasValue && minBikes.Value > 0)
            {
                query = query.Where(s => s.available_bikes >= minBikes.Value);
            }

            // 3. ORDENACIÓN
            bool isDescending = dir?.ToLowerInvariant() == "desc";

            if (!string.IsNullOrEmpty(sort))
            {
                switch (sort.ToLowerInvariant())
                {
                    case "name":
                        query = isDescending ? query.OrderByDescending(s => s.name) : query.OrderBy(s => s.name);
                        break;
                    case "availablebikes":
                        query = isDescending ? query.OrderByDescending(s => s.available_bikes) : query.OrderBy(s => s.available_bikes);
                        break;
                    case "occupancy":
                        query = isDescending ? query.OrderByDescending(s => s.Occupancy) : query.OrderBy(s => s.Occupancy);
                        break;
                    default:
                        query = query.OrderBy(s => s.number);
                        break;
                }
            }
            else
            {
                query = query.OrderBy(s => s.number);
            }

            // 4. PAGINACIÓN
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            int skip = (page - 1) * pageSize;

            return query.Skip(skip).Take(pageSize).ToList();
        }
    }
}
