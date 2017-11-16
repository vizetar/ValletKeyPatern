using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.AspNetCore.Http;

namespace ValletKey.API.Controllers
{
    [Route("api/[controller]")]
    public class SASTokenController : Controller
    {
        private readonly IConfigurationRoot configuration;
        private readonly CloudStorageAccount account;
        private readonly string blobContainer;

        public SASTokenController()
        {
            configuration = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .Build();
            this.account = CloudStorageAccount.Parse(configuration.GetSection("StorageAccount").GetSection("ConnectionString").Value);
            this.blobContainer = "files";
        }

        [HttpGet()]
        public IActionResult Get(int id)
        {
            try
            {
                var blobName = Guid.NewGuid();

                // Retrieve a shared access signature of the location we should upload this file to
                var blobSas = this.GetSharedAccessReferenceForUpload(blobName.ToString());

                return Ok(blobSas);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message); 
            }
        }

        private StorageEntitySas GetSharedAccessReferenceForUpload(string blobName)
        {
            var blobClient = this.account.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(this.blobContainer);

            var blob = container.GetBlockBlobReference(blobName);

            var policy = new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Write,

                // Create a signature for 5 min earlier to leave room for clock skew
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-5),

                // Create the signature for as long as necessary -  we can 
                SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(5)
            };

            var sas = blob.GetSharedAccessSignature(policy);

            return new StorageEntitySas
            {
                BlobUri = blob.Uri,
                Credentials = sas,
                Name = blobName
            };
        }

        public struct StorageEntitySas
        {
            public string Credentials;
            public Uri BlobUri;
            public string Name;
        }

    }
}
