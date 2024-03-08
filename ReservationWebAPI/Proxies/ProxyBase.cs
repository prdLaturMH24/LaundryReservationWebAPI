using System.Net;

namespace ReservationWebAPI.Proxies
{
    public class ProxyBase
    {
        private HttpClient _httpClient;

        protected ProxyBase(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        protected async Task<T> GetResourceAsync<T>(Uri uri, Func<string, T> deseializer)
        {
            HttpResponseMessage response = null;
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, uri);
                response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                response.EnsureSuccessStatusCode();
                return deseializer(content);

            }
            catch (TaskCanceledException)
            {
                throw new TimeoutException($"Timeout occured while getting resource (uri: {uri}).");
            }
            catch (Exception ex)
            {
                if (response != null && response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new Exception($"401 (Unauthorized) returned while getting resource (uri: {uri}).");
                }
                else
                {
                    throw new Exception($"Error occured while getting resource (uri: {uri}).", ex);
                }
            }
        }

        protected async Task<HttpResponseMessage> GetResourceAsync(Uri uri)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            return await _httpClient.SendAsync(request);
        }

        protected async Task<HttpResponseMessage> PostResourceAsync(Uri uri)
        {
            var request = new HttpRequestMessage(HttpMethod.Post,uri);
            return await _httpClient.SendAsync(request);
        }

        protected async Task<ResponseType> PostResourceAsync<ResponseType>(Uri uri, Func<string, ResponseType> deserializer)
        {
            HttpResponseMessage response = null;
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, uri);
                response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsStringAsync();
                return deserializer(result);
            }
            catch (TaskCanceledException)
            {
                throw new TimeoutException($"Timeout occured while getting resource (uri: {uri}).");
            }
            catch (Exception ex)
            {
                if (response != null && response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new Exception($"401 (Unauthorized) returned while getting resource (uri: {uri}).");
                }
                else
                {
                    throw new Exception($"Error occured while getting resource (uri: {uri}).", ex);
                }
            }
        }
    }
}
