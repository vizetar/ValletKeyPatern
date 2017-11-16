using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace ValletKey.Client
{
    public class Program
    {
        public static void Main(string[] args)
        {

            UploadFileAsync().Wait();
            Console.ReadLine();
        }

        private static async Task UploadFileAsync()
        {
            // sas token api endpoint
            var tokenServiceEndpoint = "http://127.0.0.1:81/api/SASToken";

            try
            {
                var blobSas = GetBlobSas(new Uri(tokenServiceEndpoint)).Result;

                // Create storage credentials object based on SAS
                var credentials = new StorageCredentials(blobSas.Credentials);

                // Using the returned SAS credentials and BLOB Uri create a block blob instance to upload
                var blob = new CloudBlockBlob(blobSas.BlobUri, credentials);

                using (var stream = GetFileToUpload(10))
                {
                    await blob.UploadFromStreamAsync(stream);
                }

                Console.WriteLine("Blob uplodad successful: {0}", blobSas.Name);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine();
            Console.WriteLine("Done. Press any key to exit...");
            Console.ReadKey();
        }

        private static async Task<StorageEntitySas> GetBlobSas(Uri blobUri)
        {
            StorageEntitySas blobSas = new StorageEntitySas();
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(blobUri);
            if (response.IsSuccessStatusCode)
            {
                string stringData = response.Content.ReadAsStringAsync().Result;
                blobSas = JsonConvert.DeserializeObject<StorageEntitySas>(stringData);
            }

            return blobSas;
        }

        /// <summary>
        /// Create a sample file containing random bytes of data
        /// </summary>
        /// <param name="sizeMb"></param>
        /// <returns></returns>
        private static MemoryStream GetFileToUpload(int sizeMb)
        {
            var stream = new MemoryStream();

            var rnd = new Random();
            var buffer = new byte[1024 * 1024];

            for (int i = 0; i < sizeMb; i++)
            {
                rnd.NextBytes(buffer);
                stream.Write(buffer, 0, buffer.Length);
            }

            stream.Position = 0;

            return stream;
        }

        public struct StorageEntitySas
        {
            public string Credentials;
            public Uri BlobUri;
            public string Name;
        }
    }
}
