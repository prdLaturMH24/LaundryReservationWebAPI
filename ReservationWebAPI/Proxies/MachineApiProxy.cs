using Newtonsoft.Json;

namespace ReservationWebAPI.Proxies
{
    public class MachineApiProxy:ProxyBase, IMachineApiProxy
    {
        public MachineApiProxy(HttpClient httpClient):base(httpClient) {
        }

        public virtual async Task<string> LockMachineAsync(string machineNumber)
        {
            if (string.IsNullOrWhiteSpace(machineNumber)) { throw new ArgumentNullException($"{nameof(machineNumber)}"); }
            var query = $"api/Machine/lock?machineNumber={machineNumber}";
            var payload = await PostResourceAsync(new Uri(query, UriKind.Relative));
            return await payload.Content.ReadAsStringAsync();
        }

        public virtual async Task<string> UnlockMachineAsync(string machineNumber)
        {
            if (string.IsNullOrWhiteSpace(machineNumber)) { throw new ArgumentNullException($"{nameof(machineNumber)}"); }
            var query = $"api/Machine/unlock?machineNumber={machineNumber}";
            var payload = await PostResourceAsync(new Uri(query, UriKind.Relative));
            return await payload.Content.ReadAsStringAsync();
        }

        public virtual async Task<IEnumerable<Models.Machine>> GetMachinesAsync()
        {
            return await GetResourceAsync(new Uri("api/Machine/list", UriKind.Relative), JsonConvert.DeserializeObject<IEnumerable<Models.Machine>>);
        }
    }
}
