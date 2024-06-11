using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace FSMExtension
{
    /// <summary>
    /// Helper methods to make async HttpContent calls synchronous. These are needed due to the way
    /// we execute the plugin within D365's Synchronous Execution Context.
    /// </summary>
    public static class HttpContentExtensions
    {
        /// <summary>
        /// Deserializes the given HttpContent into an object of type <typeparamref name="T"/>.
        /// This method blocks until the result is complete.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="content"></param>
        /// <returns></returns>
        public static async Task<T> ReadAsAsync<T>(this HttpContent content)
        {
            return JsonConvert.DeserializeObject<T>(await content.ReadAsStringAsync());
        }
    }
}
