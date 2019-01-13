using StorageManagementKit.Core.Transforms;
using StorageManagementKit.Diagnostics.Logging;
using StorageManagementKit.Types;
using System;

namespace StorageManagementKit.Core.Copying.Destinations
{
    public class RepositoryDestinationFactory
    {
        #region Members
        private ILogging _logger;
        private ITransforming _transform;
        #endregion

        #region Constructors
        public RepositoryDestinationFactory(ILogging logger, ITransforming transform)
        {
            _logger = logger ?? throw new ArgumentNullException("logger");
            _transform = transform;
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Create a <see cref="IRepositoryDestination"/> instance in according to the command arguments
        /// </summary>
        public IRepositoryDestination Create(DestinationSettings settings)
        {
            settings = settings ?? throw new ArgumentNullException("settings");

            switch (settings.Repository)
            {
                case DestinationRepository.Local:
                    {
                        Helpers.MandatoryValue("destination", settings.Path);

                        LocalDirectoryDestination dest = new LocalDirectoryDestination(settings.Path, true, _transform);
                        dest.Logger = _logger;
                        return dest;
                    }

                case DestinationRepository.GCS:
                    {
                        Helpers.MandatoryValue("destination", settings.Path);
                        Helpers.MandatoryValue("destination api key filename", settings.ApiKey);

                        GcsBucketDestination dest = new GcsBucketDestination(settings.Path, settings.ApiKey);
                        dest.Logger = _logger;
                        return dest;
                    }

                case DestinationRepository.S3:
                    {
                        Helpers.MandatoryValue("destination", settings.Path);
                        Helpers.MandatoryValue("destination api key filename", settings.ApiKey);

                        S3BucketDestination dest = new S3BucketDestination(settings.Path, settings.ApiKey);
                        dest.Logger = _logger;
                        return dest;
                    }

                case DestinationRepository.ABS:
                    {
                        Helpers.MandatoryValue("destination", settings.Path);
                        Helpers.MandatoryValue("destination api key filename", settings.ApiKey);

                        AbsDestination dest = new AbsDestination(settings.Path, settings.ApiKey);
                        dest.Logger = _logger;
                        return dest;
                    }

                default:
                    throw new SmkException($"Unsupported repository destination '{settings.Repository}");
            }
        }
        #endregion
    }
}
