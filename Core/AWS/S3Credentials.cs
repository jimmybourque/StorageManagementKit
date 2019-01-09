using Amazon;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;

namespace StorageManagementKit.Core.AWS
{
    public class S3Credentials
    {
        #region Properties
        public string AccessKeyId { get; set; }
        public string SecretAccessKey { get; set; }
        public string Region { get; set; }

        public RegionEndpoint AwsRegion
        {
            get
            {
                foreach (var awsRegion in RegionEndpoint.EnumerableAllRegions)
                    if (awsRegion.SystemName.Equals(Region))
                        return awsRegion;

                StringBuilder sb = new StringBuilder();
                foreach (var awsRegion in RegionEndpoint.EnumerableAllRegions)
                {
                    if (sb.Length == 0)
                        sb.Append(awsRegion.SystemName);
                    else
                        sb.Append($", {awsRegion.SystemName}");
                }

                throw new Exception($"The region {Region} is not recognized by AWS. Supported regions are {sb.ToString()}.");
            }
        }
        #endregion

        #region Public methods
        public static S3Credentials LoadKey(string fileKey)
        {
            try
            {
                if (!File.Exists(fileKey))
                    throw new FileNotFoundException(fileKey);

                return JsonConvert.DeserializeObject<S3Credentials>(File.ReadAllText(fileKey));
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load the filekey {fileKey}", ex);
            }
        }
        #endregion
    }
}
